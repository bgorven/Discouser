using Discouser.Data;
using Discouser.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Discouser.Data.Messages
{
    internal abstract class Message
    {
        public Message(int id, string channel)
        {
            MessageId = id;
            Channel = channel;
        }

        /// <summary>
        /// Calls <code>Poller.Process(this)</code>.
        /// </summary>
        public abstract Task BeProcessedBy(Poller poller);

        public int MessageId { get; private set; }
        public string Channel { get; private set; }

        public static Message Decode(JToken messageToDecode)
        {
            try
            {
                var channel = (string)messageToDecode["channel"];
                var id = (int)messageToDecode["message_id"];

                if (channel == "/__status")
                {
                    return StatusMessage.Decode(messageToDecode, id, channel);
                }
                else if (channel.StartsWith("/topic/"))
                {
                    return TopicMessage.Decode(messageToDecode, id, channel);
                }
                else
                {
                    return new ErrorMessage(messageToDecode.ToString());
                }
            }
            catch (Exception)
            {
                return new ErrorMessage(messageToDecode.ToString());
            }
        }
    }
}