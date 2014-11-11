using Discouser.Model;
using System.Threading.Tasks;

namespace Discouser.Data.Messages
{
    internal class ErrorMessage : Message
    {
        public ErrorMessage(string rawMessage) : base(0, "__error")
        {
            RawMessage = rawMessage;
        }

        public string RawMessage { get; set; }

        public override Task BeProcessedBy(Poller poller)
        {
            return poller.Process(this);
        }
    }
}
