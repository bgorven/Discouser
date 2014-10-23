namespace Discouser.Model
{

    internal class Message
    {
        public Message(int id)
        {
            MessageId = id;
        }

        internal enum Types
        {
            Status,
            Topic,
            Latest,
            Notifications,
            Error
        }

        public int MessageId { get; set; }
        public Types Type { get; set; }
    }

    internal class StatusMessage : Message
    {
        public StatusMessage(int id) : base(id)
        {
            Type = Types.Status;
        }

        public string channel { get; set; }
        public int channelStatus { get; set; }
    }

    internal class TopicMessage : Message
    {
        public TopicMessage(int id) : base(id)
        {
            Type = Types.Topic;
        }

        /// <summary>
        /// https://github.com/discourse/discourse/blob/master/app/assets/javascripts/discourse/controllers/topic.js.es6
        /// </summary>
        internal enum TopicMessageTypes
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
        public TopicMessageTypes TopicMessageType { get; set; }
    }
}