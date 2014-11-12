using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discouser.Data;
using Discouser.Model;
using System.IO;
using Windows.Storage;
using System.Threading;
using Discouser.Data.Messages;
using System.Collections.Concurrent;

namespace Discouser.Data
{
    class DataContext : IDisposable
    {
        private string _dbString;

        internal ApiConnection Api { get; private set; }
        internal Guid LocalGuid { get; private set; }
        internal TimeSpan PollDelay { get; private set; }
        internal string FolderName { get; private set; }

        public string Username { get; private set; }
        internal string SiteUrl { get; private set; }

        public string SiteName { get; private set; }
        public StorageFolder StorageDir { get; private set; }
        public Logger Logger { get; private set; }
        public Poller Poller { get; private set; }

        private SQLiteAsyncConnection _db;


        public DataContext(string url, string username, Guid guid)
        {
            SiteUrl = url;
            Username = username;
            LocalGuid = guid;
            Api = new ApiConnection(url, LocalGuid);
            FolderName = SiteUrl.Replace("http:", "").Replace("https:", "").Replace("/", "");
            Logger = new Logger();
            Poller = new Poller(this);
        }

        public DataContext() : this("meta.discourse.org", "", Guid.Empty) { }

        /// <summary>
        /// Attempts to login with the login username and password if supplied, or the authorization
        /// token stored in the HttpClient's cookies.
        /// </summary>
        /// <param name="username">The user name to log in with. If null, the contents of the Username property will be used. If the property
        /// is also null, the site will be queried and the name of the currently logged in session will be returned.
        /// </param>
        /// <param name="password">If username and password are not null, will log in using the supplied credentials.</param>
        /// <returns>The name of the logged in session, or null if no session is active.</returns>
        internal async Task<string> Authorize(string username = null, string password = null)
        {
            var login = username ?? Username;

            if (login != null && password != null)
            {
                await Api.PostSession(login: login, password: password);
            }

            var session = await Api.GetSessionCurrent();

            return (session ?? new Session()).Username;
        }

        internal async Task Initialize()
        {
            StorageDir = await ApplicationData.Current.RoamingFolder.CreateFolderAsync(FolderName, CreationCollisionOption.OpenIfExists);
            _dbString = Path.Combine(StorageDir.Path, Username + ".db");

            _db = new SQLiteAsyncConnection(_dbString);
            await Transaction(db =>
            {
                //db.DropTable<TopicMessage>();
                //db.DropTable<Category>();
                //db.DropTable<UserInfo>();
                //db.DropTable<Topic>();
                //db.DropTable<Reply>();
                //db.DropTable<Like>();
                //db.DropTable<Post>();
                //db.DropTable<User>();
                //db.DropTable<Site>();
                db.CreateTable<TopicMessage>();
                db.CreateTable<Category>();
                db.CreateTable<UserInfo>();
                db.CreateTable<Topic>();
                db.CreateTable<Reply>();
                db.CreateTable<Like>();
                db.CreateTable<Post>();
                db.CreateTable<User>();
                db.CreateTable<Site>();
            });

            await InitializeSite();
        }

        public async Task Transaction(Action<SQLiteConnection> action)
        {
            await _db.RunInTransactionAsync(action);
        }

        public async Task<T> Transaction<T>(Func<SQLiteConnection,T> action)
        {
            T result = default(T);
            await _db.RunInTransactionAsync(db => result = action(db));
            return result;
        }

        public async Task InitializeSite()
        {
            var categories = await Api.GetAllCategories();

            await Transaction(db => db.InsertAll(categories, "OR IGNORE"));

        }

        public async Task<IEnumerable<Category>> AllCategories()
        {
            return await Transaction(db => db.Table<Category>().ToList());
        }

        internal async Task InitializeCategory(Category category)
        {
            var result = await DownloadCategory(category);
            var backgroundTask = Task.Run(async () =>
            {
                try
                {
                    while (!string.IsNullOrEmpty(result))
                    {
                        result = await DownloadCategory(category, result);
                    }
                    category.Initialized = true;
                    await Transaction(db => db.Update(category));
                }
                catch (Exception é)
                {
                    Logger.Log(é);
                }
            });
        }

        internal async Task<string> DownloadCategory(Category category, string pageToGet = null)
        {
            var result = await Api.GetCategoryPage(category.Path, pageToGet);
            if (result.Item1 == null || !result.Item1.Any()) throw new FileNotFoundException("Category ‘" + category.Path + "’ failed to download");
            await Transaction(db => db.InsertAll(result.Item1, "OR IGNORE"));
            return result.Item2;
        }

        internal async Task LatestTopicMessage(int topicId, int messageId)
        {
            await Transaction(db =>
            {
                var topic = db.Get<Topic>(topicId);
                if (topic != null)
                {
                    topic.LatestMessage = messageId;
                    db.Update(topic);
                }
            });
        }

        internal async Task DeletePost(int postId)
        {
            await Transaction(db =>
            {
                var post = db.Get<Post>(postId);
                if (post != null)
                {
                    post.Deleted = true;
                    db.Update(post);
                }
            });
        }

        internal async Task DownloadLikes(int id)
        {
            var likes = await Api.GetLikes(id);

            await Transaction(db => db.InsertAll(likes, "OR REPLACE"));
        }

        internal async Task DownloadPost(int id)
        {
            var result = await Api.GetPost(id);
            await Transaction(db =>
            {
                db.InsertOrReplace(result.Item1);
                db.InsertOrReplace(result.Item2);
            });
        }

        public void Dispose()
        {
            if (Api != null)
            {
                Api.Dispose();
                Api = null;
            }
        }

        private SingletonTask _topicTask;
        private ConcurrentDictionary<int, Func<Topic, Task>> _topicCallbacks = new ConcurrentDictionary<int,Func<Topic,Task>>();
        private ConcurrentDictionary<int, Func<Post, Task>> _postCallbacks = new ConcurrentDictionary<int,Func<Post,Task>>();

        internal async Task InitializeTopic(Topic topic, Func<Topic, Task> callback)
        {
            try
            {
                _topicCallbacks[topic.Id] = callback;
                if (topic.LatestMessage == -1)
                {
                    var initial = await Api.DownloadTopicInitial(topic);
                    var resultList = initial.Item2;
                    await Transaction(db => InsertOrUpdateAll(resultList, db));

                    if (_topicTask == null)
                    {
                        _topicTask = new SingletonTask(Logger);
                    }
                    await _topicTask.SetTask(topic.Name, async cancellationToken =>
                    {
                        var alreadyDownloaded = new HashSet<int>(await Transaction(db => db.Table<Post>() 
                                                                          .Where(post => post.TopicId == topic.Id)
                                                                          .AsEnumerable()
                                                                          .Select(post => post.Id)));
                        var yetToDownload = initial.Item1.Where(post => !alreadyDownloaded.Contains(post));
                        var results = Api.DownloadTopicStream(topic, yetToDownload);
                        foreach (var resultTask in results)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            var result = await resultTask;
                            await Transaction(db => InsertOrUpdateAll(result, db));
                        }
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            //Download completed successfully, save updated topic with latestmessage
                            await Transaction(db => db.Update(initial.Item3));
                            await callback(topic);
                        }
                    });
                }
                else await UpdateTopic(topic);
            }
            catch (Exception é)
            {
                Logger.Log(é);
            }
        }

        private async Task UpdateTopic(Topic topic)
        {
            var unprocessedMessages = await Transaction(db => db
                .Table<TopicMessage>()
                .Where(message => message.TopicId == topic.Id && message.Id > topic.LatestMessage));

            if (!unprocessedMessages.Any()) return;

            topic.LatestMessage = unprocessedMessages.Select(message => message.Id).Max();

            var updatedPostsList = Api.DownloadTopicStream(topic, unprocessedMessages.Select(message => message.PostId));
            var deletedPosts = unprocessedMessages.Where(message => message.TopicMessageType == TopicMessage.Type.Deleted).ToArray();

            foreach (var updatedPostsTask in updatedPostsList)
            {
                var updatedPosts = await updatedPostsTask;
                await Transaction(db =>
                {
                    db.Update(topic);
                    InsertOrUpdateAll(updatedPosts, db);
                    foreach (var deletedPost in deletedPosts)
                    {
                        try
                        {
                            var post = db.Get<Post>(deletedPost.PostId);
                            post.Deleted = true;
                            db.Update(post);
                        }
                        catch (Exception é)
                        {
                            Logger.Log(é, "Deleted post not found.");
                        }
                    }
                });
            }
        }

        private void InsertOrUpdateAll(IEnumerable<Tuple<Post,User>> results, SQLiteConnection db)
        {
            db.InsertAll(results.Select(tuple => tuple.Item1).Where(i => i != null), "OR REPLACE");
            db.InsertAll(results.Select(tuple => tuple.Item2).Where(i => i != null), "OR REPLACE");
        }

        internal async Task SaveTopicMessages(List<TopicMessage> _topicMessages)
        {
            await Transaction(db => db.InsertAll(_topicMessages, "OR REPLACE"));
        }

        internal async Task NotifyTopics(IEnumerable<int> topicIds)
        {
            foreach (var topicId in topicIds)
            {
                Func<Topic, Task> callback;
                if (_topicCallbacks.TryGetValue(topicId, out callback)) await callback(null);
            }
        }

        internal async Task NotifyPosts(IEnumerable<int> postIds)
        {
            foreach (var postId in postIds)
            {
                Func<Post, Task> callback;
                if (_postCallbacks.TryGetValue(postId, out callback)) await callback(null);
            }
        }
    }
}
