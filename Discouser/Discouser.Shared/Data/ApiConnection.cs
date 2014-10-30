using Discouser.Data.Messages;
using Discouser.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
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
            _client.DefaultRequestHeaders.Append("X-SILENCE-LOGGER", "true");
    }

        public void Dispose()
        {
            _client.Dispose();
        }

        internal async Task PostSession(string login, string password)
        {
            await Post("session", Utility.KeyValuePair("login", login), Utility.KeyValuePair("password", password));
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

        internal async Task<IEnumerable<Message>> Poll(IEnumerable<KeyValuePair<string,string>> channels)
        {
            var result = await Post("message-bus/" + _guid + "/poll", channels.ToArray());
            if (result == null) return new Message[0];
            return result.Select(Message.Decode);
        }

        internal async Task<Tuple<IEnumerable<User>, IEnumerable<Post>, Topic>> DownloadEntireThread(int id)
        {
            var topic = new Topic();
            string topicChannelString = "/topic/" + id;
            string topicString = "/t/" + id;

            topic.LatestMessage = -1;
            var channel = Utility.KeyValuePair(topicChannelString, "-1");
            var pollResult = await Post("message-bus/" + _guid + "/poll?dlp=t", new KeyValuePair<string, string>[] { channel });
            if (pollResult != null)
            {
                var message = Message.Decode(pollResult.First()) as StatusMessage;
                if (message != null) topic.LatestMessage = message.Statuses.Where(status => status.Key == topicChannelString).First().Value;
            }

            var result = await Get(topicString);
            if (result == null) return null;

            topic.Activity = (DateTime)result["last_posted_at"];
            topic.CategoryId = (int)result["category_id"];
            topic.Id = (int)result["id"];
            topic.Name = (string)result["title"];

            var posts = new Dictionary<int, Post>();
            var users = new Dictionary<int, User>();
            var replies = new List<Reply>();
            var poststream = result["post_stream"]["posts"];
            var stream = result["post_stream"]["stream"].Select(value => (int)value);

            foreach (var token in await GetRawPostStream(topicString, stream))
            {
                DecodePostToken(token, posts, users, replies);
            }

            return new Tuple<IEnumerable<User>, IEnumerable<Post>, Topic>(users.Values, posts.Values, topic);
        }

        private static void DecodePostToken(JToken token, Dictionary<int, Post> posts, Dictionary<int, User> users, List<Reply> replies)
        {
            var user = ExtractUserFromPost(token);
            users[user.Id] = user;
            if (token["reply_to_post_number"] != null)
            {
                var reply = new Reply()
                {
                    OriginalPostId = posts[(int)token["reply_to_post_number"]].Id,
                    ReplyPostId = (int)token["id"],
                };
                replies.Add(reply);
            }
            var post = DecodePost(token);
            posts[post.PostNumberInTopic] = post;
        }

        private async Task<IEnumerable<JToken>> GetRawPostStream(string topicString, IEnumerable<int> stream)
        {
            var streamParameters = stream.Select(id => Utility.KeyValuePair("post_ids[]", id.ToString())).ToList();
            streamParameters.Add(Utility.KeyValuePair("_", "wtf"));
            var result = await Get(topicString + "/posts", streamParameters.ToArray());

            if (result == null) return new JToken[0];
            else return result["post_stream"]["posts"];
        }

        private static Post DecodePost(JToken post)
        {
            return new Post()
            {
                Created = (DateTime)post["created_at"],
                Deleted = post["deleted_at"] != null,
                HtmlText = (string)post["cooked"],
                Id = (int)post["id"],
                PostNumberInTopic = (int)post["post_number"],
                TopicId = (int)post["topic_id"],
                UserId = (int)post["user_id"],
            };
        }

        private static User ExtractUserFromPost(JToken post)
        {
            return new User()
            {
                Id = (int)post["user_id"],
                AvatarId = (int)post["uploaded_avatar_id"],
                Username = (string)post["username"],
                DisplayName = (string)post["name"],
                Title = (string)post["user_title"],
            };
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
                Utility.KeyValuePair("id", id.ToString()), 
                Utility.KeyValuePair("post_action_type_id", PostActionTypes.Like.ToString()), 
                Utility.KeyValuePair("_", "wtf"));
            if (result == null) return new Like[0];
            return result.Select(user => new Like() { PostId = id, UserId = (int)user["id"] });
        }

        /// <summary>
        /// <para>Note that the value of Reply.OriginalPostId returned here is actually the post number within the current topic :(</para>
        /// </summary>
        internal async Task<Tuple<User,Post,Reply>> GetPost(int id)
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
                    RawText = (string)result["raw"],
                    HtmlText = (string)result["cooked"],
                    UserId = (int)result["user_id"],
                    Created = (DateTime)result["created_at"],
                },
                result["reply_to_post_number"] == null ? null : new Reply()
                {
                    ReplyPostId = id,
                    OriginalPostId = (int)result["reply_to_post_number"]
                });
        }
    }
}
