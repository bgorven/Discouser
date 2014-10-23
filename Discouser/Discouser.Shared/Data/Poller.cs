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
        private IDictionary<string, string> channels = new ConcurrentDictionary<string, string>();

        public Poller(DataContext context, Logger logger)
        {
            _context = context;
            _logger = logger;
        }

        public void Initialize()
        {
            _task = Poll();
        }

        internal void RegisterChannel(string channel, int status)
        {
            channels.Add(channel, status.ToString());
        }

        internal async Task Poll()
        {
            var messages = await _context.Api.Poll(channels);
        }

#pragma warning disable 1998

        private Task ProcessMessage(Message message)
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
