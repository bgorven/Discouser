using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discouser.Api;
using Discouser.Model;
using System.IO;
using Windows.Storage;

namespace Discouser
{
    class DataContext : IDisposable
    {
        internal ApiConnection Api { get; private set; }
        internal SQLiteConnection PersistentDbConnection { get; private set; }
        internal SQLiteConnection NewDbConnection() {  return new SQLiteConnection(_dbString); }
        internal SQLiteAsyncConnection NewAsyncDbConnection() {  return new SQLiteAsyncConnection(_dbString); }
        internal Guid LocalGuid { get; private set; }
        internal TimeSpan PollDelay { get; set; }
        internal string FolderName { get; private set; }
        private string _dbString;

        public string Username { get; private set; }
        internal string SiteUrl { get; private set; }
        public string SiteName { get; private set; }
        public StorageFolder StorageDir { get; private set; }

        public DataContext(string url, string username, Guid localGuid)
        {
            SiteUrl = url;
            Username = username;
            LocalGuid = localGuid;
            Api = new ApiConnection(url, LocalGuid);
            FolderName = SiteUrl.Replace("http:", "").Replace("https:", "").Replace("/", "");
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

            PersistentDbConnection = NewDbConnection();

            var db = NewAsyncDbConnection();

            await Task.WhenAll(new Task[] {
                db.CreateTableAsync<Category>(),
                db.CreateTableAsync<LongText>(),
                db.CreateTableAsync<UserInfo>(),
                db.CreateTableAsync<Topic>(),
                db.CreateTableAsync<Reply>(),
                db.CreateTableAsync<Like>(),
                db.CreateTableAsync<Post>(),
                db.CreateTableAsync<User>(),
                db.CreateTableAsync<Site>(),
            });
        }

        public async Task<ICollection<Category>> AllCategories()
        {
            var categories = await Api.GetCategories();

            var db = NewDbConnection();
            db.InsertAll(categories, "OR REPLACE");

            return db.Table<Category>().ToList();
        }

        private volatile ViewModel.Topic _topicToWatch = null;

        /// <summary>
        /// Polls message bus for information about the active topic, updates the db if new information has arrived,
        /// notifies the topic that there is new information available, then launches a new copy of its Watch task 
        /// on a configurable delay. Polling of the current topic will end when a new topic is watched.
        /// </summary>
        public async Task MonitorTopic(ViewModel.Topic toWatch)
        {
            _topicToWatch = toWatch;

            await new Watcher(toWatch, this).PollTopic();
        }

        private class Watcher
        {
            private DataContext _context;
            private ViewModel.Topic _topicToWatch;

            internal Watcher(ViewModel.Topic toWatch, DataContext context)
            {
                _topicToWatch = toWatch;
                _context = context;
            }

            internal async Task PollTopic()
            {
                if (_context._topicToWatch != _topicToWatch) return;

                var result = await _context.Api.TopicMessages(_topicToWatch.Id, _topicToWatch.LatestMessage);
                foreach (var message in result)
                {
                    if (message.Type != TopicMessage.MessageType.Error && message.TopicId == _topicToWatch.Id)
                    {
                        _context.LatestTopicMessage(message.TopicId, message.MessageId);
                        switch (message.Type)
                        {
                            case TopicMessage.MessageType.Created:
                            case TopicMessage.MessageType.Recovered:
                                await _context.DownloadPost(message.PostId);
                                _topicToWatch.Changes = true;
                                break;
                            case TopicMessage.MessageType.Acted:
                                await _context.DownloadLikes(message.PostId);
                                _topicToWatch.UpdatePostInfo(message.PostNumber);
                                break;
                            case TopicMessage.MessageType.Rebaked:
                            case TopicMessage.MessageType.Revised:
                                await _context.DownloadPost(message.PostId);
                                _topicToWatch.UpdatePost(message.PostNumber);
                                break;
                            case TopicMessage.MessageType.Deleted:
                                _context.DeletePost(message.PostId);
                                _topicToWatch.DeletePost(message.PostNumber);
                                break;
                        }
                    }
                }

                var nextPoll = Task.Delay(_context.PollDelay).ContinueWith(PollTopic);
            }

            internal Task PollTopic(Task antecedent)
            {
                return PollTopic();
            }
        }

        private void LatestTopicMessage(int topicId, int messageId)
        {
            var db = NewDbConnection();
            var topic = db.Get<Topic>(topicId);
            if (topic != null)
            {
                topic.LatestMessage = messageId;
                db.Update(topic);
            }
        }

        private void DeletePost(int postId)
        {
            var db = NewDbConnection();
            var post = db.Get<Post>(postId);
            if (post != null)
            {
                post.Deleted = true;
                db.Update(post);
            }
        }

        private async Task DownloadLikes(int id)
        {
            var db = NewDbConnection();
            db.InsertAll(await Api.GetLikes(id: id), "OR REPLACE");
        }

        private async Task DownloadPost(int id)
        {
            var db = NewDbConnection();
            db.InsertOrReplace(await Api.GetPost(id));
        }

        public void Dispose()
        {
            var db = NewDbConnection();
            db.Dispose();
            Api.Dispose();
        }
    }
}
