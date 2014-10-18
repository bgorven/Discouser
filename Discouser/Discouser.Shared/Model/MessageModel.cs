namespace Discouser.Model
{
    internal class TopicMessage
    {
        /// <summary>
        /// https://github.com/discourse/discourse/blob/master/app/assets/javascripts/discourse/controllers/topic.js.es6
        /// </summary>
        internal enum MessageType
        {
            Status,
            Revised,
            Rebaked,
            Recovered,
            Created,
            Acted,
            Deleted,
            Unknown,
            Error
        }

        public int MessageId { get; set; }
        public int TopicId { get; set; }
        public int PostNumber { get; set; }
        public int PostId { get; set; }
        public MessageType Type { get; set; }
    }
}