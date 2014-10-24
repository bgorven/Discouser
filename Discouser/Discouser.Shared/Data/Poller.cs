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
        private DataContext _context;
        private Logger _logger;
        private Task _task;

        public Poller(DataContext context, Logger logger)
        {
            _context = context;
            _logger = logger;
        }

        public void Initialize()
        {
            _task = Poll();
        }

        private IDictionary<string, string> channels = new ConcurrentDictionary<string, string>();
        private IDictionary<int, Action<TopicMessage>> topicCallBacks = new ConcurrentDictionary<int, Action<TopicMessage>>();

        internal void Register(Topic topic, Action<TopicMessage> callback)
        {
            topicCallBacks[topic.Id] = callback;
        }

        internal async Task Poll()
        {
            var messages = await _context.Api.Poll(channels);
            foreach (var message in messages)
            {
                await Process(message);
            }
        }

#pragma warning disable 1998

        private Task Process(Message message)
        {
            return message.BeProcessedBy(this);
        }

        internal async Task Process(TopicMessage message)
        {
            _context.LatestTopicMessage(message.TopicId, message.MessageId);

            switch (message.TopicMessageType)
            {
                case TopicMessage.Type.Created:
                case TopicMessage.Type.Recovered:
                    await _context.DownloadPost(message.PostId);
                    //viewModel.UpdateTopic(message.TopicId);
                    break;
                case TopicMessage.Type.Acted:
                    await _context.DownloadLikes(message.PostId);
                    //viewModel.UpdatePostInfo(message.PostNumber);
                    break;
                case TopicMessage.Type.Rebaked:
                case TopicMessage.Type.Revised:
                    await _context.DownloadPost(message.PostId);
                    //viewModel.UpdatePost(message.PostNumber);
                    break;
                case TopicMessage.Type.Deleted:
                    _context.DeletePost(message.PostId);
                    //viewModel.DeletePost(message.PostNumber);
                    break;
            }
        }

        internal async Task Process(ErrorMessage errorMessage) { }

        internal async Task Process(StatusMessage statusMessage)
        {
            channels[statusMessage.Channel] = statusMessage.ChannelStatus.ToString();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
