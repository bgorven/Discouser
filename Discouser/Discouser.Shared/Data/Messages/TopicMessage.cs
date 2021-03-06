﻿using Newtonsoft.Json.Linq;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discouser.Data.Messages
{
    [Table("TopicMessages")]
    internal class TopicMessage : Message
    {
        public TopicMessage(int id, string channel) : base(id, channel) { }
        public TopicMessage() : base(0, "") { }

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
        [Indexed]
        public int TopicId { get; set; }
        public int PostNumber { get; set; }
        public int PostId { get; set; }
        public Type TopicMessageType { get; set; }

        public static TopicMessage Decode(JToken messageToDecode, int id, string channel)
        {
            var messageResult = new TopicMessage(id, channel);

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
