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
        private string _dbString;
        private string _username;
        private string _sitePath;

        public DataContext(string url, string username)
        {
            _username = username;
            _sitePath = url;
            _dbString = url.Replace("http:", "").Replace("https:", "").Replace("/", "") + ":" + _username + ".db";
            Api = new ApiConnection(url, LocalGuid);
        }

        void Initialize()
        {
            Db.CreateTable<Reply>();
            Db.CreateTable<Category>();
            Db.CreateTable<LongText>();
            Db.CreateTable<Topic>();
            Db.CreateTable<Like>();
            Db.CreateTable<Post>();
            Db.CreateTable<User>();

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            var guid = localSettings["Guid"];
            if (guid == null)
            {
                LocalGuid = Guid.NewGuid();
                localSettings["Guid"] = LocalGuid;
            }
            else
            {
                LocalGuid = (Guid)guid;
            }
        }

        async Task<bool> Authorize(string password)
        {
            if (!string.IsNullOrEmpty(password))
            {
                await Api.PostSession(login: _username, password: password);
            }

            return ((await Api.GetSessionCurrent()) ?? new Session()).Username == _username;
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
