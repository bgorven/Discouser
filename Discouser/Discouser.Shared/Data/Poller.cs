using Discouser.Data.Messages;
using Discouser.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discouser.Data
{
    class Poller : IDisposable
    {
        private const string TopicPrefix = "/topic/";
        private const string PostPrefix = "/post/";

        private DataContext _context;
        private Logger _logger;
        private Task _task;
        private bool _cancelled;

        public Poller(DataContext context)
        {
            _context = context;
            _logger = context.Logger;
            _cancelled = false;
        }

        public void Initialize()
        {
            _task = Poll();
        }

        public void Cancel()
        {
            _cancelled = true;
        }

        private IDictionary<string, string> channels = new Dictionary<string, string>();
        private IDictionary<string, Action> callbacks = new Dictionary<string, Action>();

        private void Register(string channel, Action callback, int latestMessage)
        {
            channels[channel] = latestMessage.ToString();
            callbacks[channel] = callback;
        }

        private void Deregister(string channel)
        {
            channels.Remove(channel);
            callbacks.Remove(channel);
        }

        internal void Register(Topic topic, Action callback, bool deregister = false)
        {
            if (deregister)
            {
                Deregister(TopicPrefix + topic.Id);
            }
            else
            {
                Register(TopicPrefix + topic.Id, callback, topic.LatestMessage);
            }
        }

        internal void Register(Post post, Action callback, bool deregister = false)
        {
            if (deregister)
            {
                callbacks.Remove(PostPrefix + post.Id);
            }
            else
            {
                callbacks[PostPrefix + post.Id] = callback;
            }
        }

        internal async Task Poll()
        {
            while (!_cancelled)
            {
                var messages = await _context.Api.Poll(channels);
                _topicMessages = new List<TopicMessage>();
                foreach (var message in messages)
                {
                    await Process(message);
                    await _context.SaveTopicMessages(_topicMessages);
                    await _context.NotifyTopics(_topicIds);
                }
            }
        }

        List<TopicMessage> _topicMessages;
        HashSet<int> _topicIds;
        List<int> _postIds;

#pragma warning disable 1998

        private Task Process(Message message)
        {
            return message.BeProcessedBy(this);
        }

        internal async Task Process(TopicMessage message)
        {
            _topicMessages.Add(message);
            _topicIds.Add(message.TopicId);
            _postIds.Add(message.PostId);
        }

        internal async Task Process(ErrorMessage errorMessage)
        {
            _logger.Log(errorMessage.RawMessage);
        }

        internal async Task Process(StatusMessage statusMessage)
        {
            foreach (var update in statusMessage.Statuses)
            {
                channels[update.Key] = update.Value.ToString();
            }
        }

        public void Dispose()
        {
            _cancelled = true;
            _task = null;
        }
    }
}
