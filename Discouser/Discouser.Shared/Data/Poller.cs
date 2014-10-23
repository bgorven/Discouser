using Discouser.Model;
using System;
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

        internal async Task Poll()
        {

        }

        internal async Task ProcessMessage(Message message)
        {
            if (message.TopicMessageType != Message.TopicMessageType.Error)
            {
                _context.LatestTopicMessage(message.TopicId, message.MessageId);
                switch (message.TopicMessageType)
                {
                    case Message.TopicMessageType.Created:
                    case Message.TopicMessageType.Recovered:
                        await _context.DownloadPost(message.PostId);
                        //viewModel.UpdateTopic(message.TopicId);
                        break;
                    case Message.TopicMessageType.Acted:
                        await _context.DownloadLikes(message.PostId);
                        //viewModel.UpdatePostInfo(message.PostNumber);
                        break;
                    case Message.TopicMessageType.Rebaked:
                    case Message.TopicMessageType.Revised:
                        await _context.DownloadPost(message.PostId);
                        //viewModel.UpdatePost(message.PostNumber);
                        break;
                    case Message.TopicMessageType.Deleted:
                        _context.DeletePost(message.PostId);
                        //viewModel.DeletePost(message.PostNumber);
                        break;
                }
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
