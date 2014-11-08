using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discouser.Data;
using Discouser.Model;
using System.IO;
using Windows.Storage;

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

            await DbTransaction(db =>
            {
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

        public async Task DbTransaction(Action<SQLiteConnection> action)
        {
            await _db.RunInTransactionAsync(action);
        }

        public async Task<T> DbTransaction<T>(Func<SQLiteConnection,T> action)
        {
            T result = default(T);
            await _db.RunInTransactionAsync(db => result = action(db));
            return result;
        }

        public async Task InitializeSite()
        {
            var categories = await Api.GetAllCategories();

            await DbTransaction(db => db.InsertAll(categories, "OR REPLACE"));

        }

        public async Task<IEnumerable<Category>> AllCategories()
        {
            return await DbTransaction(db => db.Table<Category>().ToList());
        }

        internal async Task InitializeCategory(Category category)
        {
            var result = await DownloadCategory(category, 0);
            if (result)
            {
                var backgroundTask = Task.Run(async () =>
                {
                    var nextPage = 1;
                    try
                    {
                        while (await DownloadCategory(category, nextPage)) nextPage++;

                        category.Initialized = true;
                        await DbTransaction(db => db.Update(category));
                    }
                    catch (Exception é)
                    {
                        var task = Logger.Log(é);
                    }
                });
            }
        }

        internal async Task<bool> DownloadCategory(Category category, int pageToGet)
        {
            var result = await Api.GetCategoryPage(category.Path, pageToGet);
            await DbTransaction(db => db.InsertAll(result.Item1, "OR REPLACE"));
            return result.Item2;
        }

        internal async Task LatestTopicMessage(int topicId, int messageId)
        {
            await DbTransaction(db =>
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
            await DbTransaction(db =>
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

            await DbTransaction(db => db.InsertAll(likes, "OR REPLACE"));
        }

        internal async Task DownloadPost(int id)
        {
            var post = await Api.GetPost(id);
            await DbTransaction(db => db.InsertOrReplace(post));
        }

        public void Dispose()
        {
            if (Api != null)
            {
                Api.Dispose();
                Api = null;
            }
        }
    }
}
