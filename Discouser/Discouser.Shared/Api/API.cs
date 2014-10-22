using Discouser.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Discouser.Api
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
            path = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : "https://" + path;
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
            return categories.Select(c => new Category() { Id = (int)c["id"], Description = (string)c["description"], Name = (string)c["name"] });
        }

        internal async Task<string> GetPostHtml(string postId)
        {
            var result = await Get("posts/" + postId);
            if (result == null) return "<p>Download Failed :(</p>";
            return (string)result["cooked"];
        }

        internal async Task<IEnumerable<TopicMessage>> TopicMessages(int topicId, int latestMessage)
        {
            IEnumerable<JToken> result = await Post("message-bus/" + _guid + "/poll", Parameter("topic/" + topicId, latestMessage.ToString()));
            if (result == null) return new TopicMessage[0];
            return result.Select<JToken,TopicMessage>(DecodeMessage);
        }

        private static TopicMessage DecodeMessage(JToken m)
        {
            try
            {
                var message = new TopicMessage() { MessageId = (int)m["message_id"] };
                if ((string)m["channel"] == "__status")
                {
                    message.Type = TopicMessage.MessageType.Status;
                    var statusMessage = ((JObject)m["data"]).Properties().First();
                    int topicNumber;
                    if (!int.TryParse( statusMessage.Name.Replace("/topic/", ""), out topicNumber)){
                        message.Type = TopicMessage.MessageType.Unknown;
                    }
                    else
                    {
                        message.TopicId = topicNumber;
                        message.MessageId = (int)statusMessage.Value;
                    }
                }
                else
                {
                    switch ((string)m["data"]["type"])
                    {
                        case "Revised":
                            message.Type = TopicMessage.MessageType.Revised;
                            break;
                        case "Rebaked":
                            message.Type = TopicMessage.MessageType.Rebaked;
                            break;
                        case "Recovered":
                            message.Type = TopicMessage.MessageType.Recovered;
                            break;
                        case "Created":
                            message.Type = TopicMessage.MessageType.Created;
                            break;
                        case "Acted":
                            message.Type = TopicMessage.MessageType.Acted;
                            break;
                        case "Deleted":
                            message.Type = TopicMessage.MessageType.Deleted;
                            break;
                        default:
                            message.Type = TopicMessage.MessageType.Unknown;
                            break;
                    }
                    if (message.Type != TopicMessage.MessageType.Unknown)
                    {
                        message.PostId = (int)m["data"]["id"];
                        message.PostNumber = (int)m["data"]["post_number"];
                    }
                }
                return message;
            }
            catch (Exception)
            {
                return new TopicMessage() { Type = TopicMessage.MessageType.Unknown };
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
