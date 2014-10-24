using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discouser.Data.Messages
{
    internal class StatusMessage : Message
    {
        public StatusMessage(int id, string channel) : base(id, channel) { }

        public IEnumerable<KeyValuePair<string, string>> Statuses { get; private set; }

        public static StatusMessage Decode(JToken messageToDecode, int id, string channel)
        {
            return new StatusMessage(id, channel)
            {
                Statuses = ((JObject)messageToDecode["data"]).Properties()
                                    .Select(statusData => Utility.KeyValuePair(statusData.Name, (string)statusData.Value))
            };
        }

        public override Task BeProcessedBy(Poller poller)
        {
            return poller.Process(this);
        }
    }
}
