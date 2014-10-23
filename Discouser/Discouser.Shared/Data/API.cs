using Discouser.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Discouser.Data
{
    public partial class ApiConnection : IDisposable
    {
        private readonly HttpClient _client = new HttpClient();
        private Logger _logger = new Logger();
        private Guid _guid;

        private string _host;

        public ApiConnection(string path, Guid guid)
        {
            path = path.EndsWith("/") ? path.Substring(0, path.Length - 1) : path;
            path = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : "http://" + path;
            _host = path;
            _guid = guid;

            _client.DefaultRequestHeaders.UserAgent.ParseAdd("Buddy");
            _client.DefaultRequestHeaders.Append("X-Requested-With", "XMLHttpRequest");
    }

        public void Dispose()
        {
            _client.Dispose();
        }

        internal async Task PostSession(string login, string password)
        {
            await Post("session", Parameter("login", login), Parameter("password", password));
        }

        internal async Task<Session> GetSessionCurrent()
        {
            var result = await Get("session/current");
            if (result == null) return null;
            return new Session() { Username = (string)result["current_user"]["username"] };
        }

        internal async Task<IEnumerable<Category>> GetCategories()
        {
            var result = await Get("categories");
            if (result == null) return new Category[0];
            IEnumerable<JToken> categories = result["category_list"]["categories"];
            return categories.Select(c => new Category() { Id = (int)c["id"], Description = (string)c["description"], Name = (string)c["name"], Color = DecodeColor(c["color"]), TextColor = DecodeColor(c["text_color"]) });
        }

        private string DecodeColor(JToken jToken)
        {
            var color = (string)jToken;
            if (color.Length == 3)
            {
                color = "" + color[0] + color[0] + color[1] + color[1] + color[2] + color[2];
            }

            if (color.Length < 8)
            {
                color = color.PadLeft(8, 'F');
            }

            return "#" + color;
        }

        internal async Task<string> GetPostHtml(string postId)
        {
            var result = await Get("posts/" + postId);
            if (result == null) return "<p>Download Failed :(</p>";
            return (string)result["cooked"];
        }

        internal async Task<IEnumerable<Message>> TopicMessages(IEnumerable<KeyValuePair<string,string>> channels)
        {
            IEnumerable<JToken> result = await Post("message-bus/" + _guid + "/poll", channels.ToArray());
            if (result == null) return new Message[0];
            return result.Select(DecodeMessage);
        }

        private static Message DecodeMessage(JToken messageToDecode)
        {
            try
            {

                var channel = (string)messageToDecode["channel"];
                if (channel == "__status")
                {
                    var messageResult = new StatusMessage(MessageId = (int)messageToDecode["message_id"]);
                    messageResult.TopicMessageType = TopicMessage.TopicMessageTypes.Status;
                    var statusMessage = ((JObject)messageToDecode["data"]).Properties().First();
                    int topicNumber;
                    if (int.TryParse(statusMessage.Name.Replace("/topic/", ""), out topicNumber))
                    {
                        messageResult.TopicId = topicNumber;
                        messageResult.MessageId = (int)statusMessage.Value;
                    }
                    else
                    {
                        messageResult.TopicMessageType = Message.TopicMessageType.Unknown;
                    }
                }
                else if (channel.StartsWith("/topic/"))
                {
                    var messageResult = new TopicMessage((int)messageToDecode["message_id"]);

                    switch ((string)messageToDecode["data"]["type"])
                    {
                        case "Revised":
                            messageResult.TopicMessageType = Message.TopicMessageType.Revised;
                            break;
                        case "Rebaked":
                            messageResult.TopicMessageType = Message.TopicMessageType.Rebaked;
                            break;
                        case "Recovered":
                            messageResult.TopicMessageType = Message.TopicMessageType.Recovered;
                            break;
                        case "Created":
                            messageResult.TopicMessageType = Message.TopicMessageType.Created;
                            break;
                        case "Acted":
                            messageResult.TopicMessageType = Message.TopicMessageType.Acted;
                            break;
                        case "Deleted":
                            messageResult.TopicMessageType = Message.TopicMessageType.Deleted;
                            break;
                        default:
                            messageResult.TopicMessageType = Message.TopicMessageType.Unknown;
                            break;
                    }
                    if (messageResult.TopicMessageType != Message.TopicMessageType.Unknown)
                    {
                        messageResult.PostId = (int)messageToDecode["data"]["id"];
                        messageResult.PostNumber = (int)messageToDecode["data"]["post_number"];
                    }
                }
                
                return messageResult;
            }
            catch (Exception)
            {
                return new Message() { TopicMessageType = Message.TopicMessageType.Unknown };
            }
        }


        /// <summary>
        /// https://github.com/discourse/discourse/blob/master/db/fixtures/003_post_action_types.rb
        /// </summary>
        private enum PostActionTypes
        {
            Bookmark = 1,
            Like = 2,
            OffTopic = 3,
            Inappropriate = 4,
            Vote = 5,
            Spam = 6,
            NotifyUser = 7,
            NotifyModerators = 8
        }

        internal async Task<IEnumerable<Like>> GetLikes(int id)
        {
            IEnumerable<JToken> result = await Get("post_actions/users", 
                Parameter("id", id.ToString()), 
                Parameter("post_action_type_id", PostActionTypes.Like.ToString()), 
                Parameter("_", "wtf"));
            if (result == null) return new Like[0];
            return result.Select(user => new Like() { PostId = id, UserId = (int)user["id"] });
        }

        /// <summary>
        /// <para>Note that the value of Reply.OriginalPostId returned here is actually the post number within the current topic :(</para>
        /// <para>The first string is the raw text, the second is the cooked.</para>
        /// </summary>
        internal async Task<Tuple<User,Post,string,string,Reply>> GetPost(int id)
        {
            var result = await Get("posts/" + id);
            if (result == null) return null;
            return Tuple.Create(
                new User()
                {
                    Id = (int)result["user_id"],
                    AvatarId = (int)result["uploaded_avatar_id"],
                    Username = (string)result["username"],
                    DisplayName = (string)result["display_username"] ?? "",
                    Title = (string)result["user_title"] ?? ""
                },
                new Post()
                {
                    Id = id,
                    TopicId = (int)result["topic_id"],
                    UserId = (int)result["user_id"],
                    Created = (DateTime)result["created_at"],
                },
                (string)result["raw"],
                (string)result["cooked"],
                result["reply_to_post_number"] == null ? null : new Reply()
                {
                    ReplyPostId = id,
                    OriginalPostId = (int)result["reply_to_post_number"]
                });
        }
    }
}
