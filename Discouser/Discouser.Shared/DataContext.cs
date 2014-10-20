using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discouser.Api;
using Discouser.Model;

namespace Discouser
{
    class DataContext : IDisposable
    {
        internal ApiConnection Api { get; private set; }
        internal SQLiteConnection Db { get { return new SQLiteConnection(_dbString); } }
        internal SQLiteAsyncConnection DbAsync { get { return new SQLiteAsyncConnection(_dbString); } }
        internal Guid LocalGuid { get; private set; }
        internal TimeSpan PollDelay { get; set; }
        private string _dbString { get { return SiteUrl.Replace("http:", "").Replace("https:", "").Replace("/", "") + ":" + Username + ".db"; } }
        public string Username { get; private set; }
        internal string SiteUrl { get; private set; }
        public string SiteName { get; internal set; }

        public DataContext(string url, string username, Guid localGuid)
        {
            SiteUrl = url;
            Username = username;
            LocalGuid = localGuid;
            Api = new ApiConnection(url, LocalGuid);
        }

        /// <summary>
        /// Attempts to login with the login username and password if supplied, or the authorization
        /// token stored in the HttpClient's cookies.
        /// </summary>
        /// <param name="username">The user name to log in with. If not null, sets the username property this DataContext
        /// will use for local db connections. If null, the contents of the username property will be used. If the property
        /// is also null, the site will be queried and the name of the currently logged in session will be returned.
        /// </param>
        /// <param name="password">If username and password are not null, will log in using the supplied credentials.</param>
        /// <returns>The name of the logged in session, or null if no session is active.</returns>
        async Task<string> Authorize(string username = null, string password = null)
        {
            if (username != null)
            {
                Username = username;
            }

            if (Username != null && password != null)
            {
                await Api.PostSession(login: Username, password: password);
            }

            var session = await Api.GetSessionCurrent();

            if (session != null && session.Username == Username)
            {
                return Username;
            }
            else
            {
                return null;
            }
        }

        void Initialize()
        {
            Db.CreateTable<Site>();
            Db.CreateTable<Reply>();
            Db.CreateTable<Category>();
            Db.CreateTable<LongText>();
            Db.CreateTable<Topic>();
            Db.CreateTable<Like>();
            Db.CreateTable<Post>();
            Db.CreateTable<User>();
            Db.CreateTable<UserInfo>();
        }

        public async Task<ICollection<Category>> AllCategories()
        {
            var categories = await Api.GetCategories();

            Db.InsertAll(categories, "OR REPLACE");

            return Db.Table<Category>().ToList();
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
            var topic = Db.Get<Topic>(topicId);
            if (topic != null)
            {
                topic.LatestMessage = messageId;
                Db.Update(topic);
            }
        }

        private void DeletePost(int postId)
        {
            var post = Db.Get<Post>(postId);
            if (post != null)
            {
                post.Deleted = true;
                Db.Update(post);
            }
        }

        private async Task DownloadLikes(int id)
        {
            Db.InsertAll(await Api.GetLikes(id: id), "OR REPLACE");
        }

        private async Task DownloadPost(int id)
        {
            Db.InsertOrReplace(await Api.GetPost(id));
        }

        public void Dispose()
        {
            Db.Dispose();
            Api.Dispose();
        }
    }
}
