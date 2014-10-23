using Discouser.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Discouser.Model
{

    internal abstract class Message
    {
        public Message(int id = 0)
        {
            MessageId = id;
        }

        /// <summary>
        /// Calls <code>Poller.Process(this)</code>.
        /// </summary>
        public abstract Task BeProcessedBy(Poller poller);

        public int MessageId { get; set; }
        public Type MessageType { get; set; }

        public static Message Decode(JToken messageToDecode)
        {
            try
            {
                var channel = (string)messageToDecode["channel"];
                var id = (int)messageToDecode["message_id"];

                if (channel == "__status")
                {
                    return StatusMessage.Decode(messageToDecode, id);
                }
                else if (channel.StartsWith("/topic/"))
                {
                    return TopicMessage.Decode(messageToDecode, id);
                }
                else
                {
                    return new ErrorMessage();
                }
            }
            catch (Exception)
            {
                return new ErrorMessage();
            }
        }
    }


    internal class ErrorMessage : Message
    {
        public ErrorMessage(int id = 0) : base(id) { }

        public override Task BeProcessedBy(Poller poller)
        {
            return poller.Process(this);
        }
    }

    internal class StatusMessage : Message
    {
        public StatusMessage(int id) : base(id) { }

        public string Channel { get; set; }
        public int ChannelStatus { get; set; }

        public static StatusMessage Decode(JToken messageToDecode, int id)
        {
            var messageResult = new StatusMessage(id);

            var statusData = ((JObject)messageToDecode["data"]).Properties().First();

            messageResult.Channel = statusData.Name;
            messageResult.ChannelStatus = (int)statusData.Value;

            return messageResult;
        }

        public override Task BeProcessedBy(Poller poller)
        {
            return poller.Process(this);
        }
    }

    internal class TopicMessage : Message
    {
        public TopicMessage(int id) : base(id: id, type: Message.Type.Topic) { }

        /// <summary>
        /// https://github.com/discourse/discourse/blob/master/app/assets/javascripts/discourse/controllers/topic.js.es6
        /// </summary>
        internal enum Type
        {
            Revised,
            Rebaked,
            Recovered,
            Created,
            Acted,
            Deleted,
            Unknown
        }
        public int TopicId { get; set; }
        public int PostNumber { get; set; }
        public int PostId { get; set; }
        public Type TopicMessageType { get; set; }

        public static TopicMessage Decode(JToken messageToDecode, int id)
        {
            var messageResult = new TopicMessage(id);

            switch ((string)messageToDecode["data"]["type"])
            {
                case "Revised":
                    messageResult.TopicMessageType = Type.Revised;
                    break;
                case "Rebaked":
                    messageResult.TopicMessageType = Type.Rebaked;
                    break;
                case "Recovered":
                    messageResult.TopicMessageType = Type.Recovered;
                    break;
                case "Created":
                    messageResult.TopicMessageType = Type.Created;
                    break;
                case "Acted":
                    messageResult.TopicMessageType = Type.Acted;
                    break;
                case "Deleted":
                    messageResult.TopicMessageType = Type.Deleted;
                    break;
                default:
                    messageResult.TopicMessageType = Type.Unknown;
                    break;
            }
            messageResult.PostId = (int)messageToDecode["data"]["id"];
            messageResult.PostNumber = (int)messageToDecode["data"]["post_number"];

            return messageResult;
        }

        public override Task BeProcessedBy(Poller poller)
        {
            return poller.Process(this);
        }
    }
}