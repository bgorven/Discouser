using Discouser.Data.Messages;
using Discouser.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Discouser.Data
{
    //The other half of this class is in Request.cs -- will be decoupled ‘eventually’.
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
            _client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            _client.DefaultRequestHeaders.Referer = new Uri(_host);
    }

        public void Dispose()
        {
            _client.Dispose();
        }

        private static readonly string[] _jsonRoot = new string[0];
        internal async Task PostSession(string login, string password)
        {
            await Post("session", _jsonRoot, Utility.KeyValuePair("login", login), Utility.KeyValuePair("password", password));
        }

        private static readonly string[] _sessionUserPath = new string[] { "current_user", "username" };
        internal async Task<Session> GetSessionCurrent()
        {
            var result = await Get("session/current", _sessionUserPath);
            if (result == null) return null;
            return new Session() { Username = (string)result };
        }

        private static readonly string[] _categoriesPath = new string[] { "site", "categories" };
        internal async Task<IEnumerable<Category>> GetAllCategories()
        {
            var result = await Get("site", _categoriesPath);
            if (result == null) return new Category[0];

            var byId = result.ToDictionary(category => (int)category["id"]);

            return result.Select(c => new Category() { 
                Id = (int)c["id"], 
                Description = (string)c["description"], 
                Name = (string)c["name"], 
                Color = DecodeColor(c["color"]), 
                TextColor = DecodeColor(c["text_color"]),
                Path = c["parent_category_id"] == null || c["parent_category_id"].Type != JTokenType.Integer ? 
                    c["slug"] + "/none" : 
                    byId[(int)c["parent_category_id"]]["slug"] + "/" + c["slug"]
            });
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
            var result = await Post("message-bus/" + _guid + "/poll", _jsonRoot, channels.ToArray());
            if (result == null) return new Message[0];
            return result.Select(Message.Decode);
        }

        internal class TopicResult
        {
            internal TopicResult(Topic topic, Dictionary<int, Model.Post> posts, Dictionary<int, User> users, List<Reply> replies)
            {
                Posts = posts.Values;
                Replies = replies;
                Users = users.Values;
                Topic = topic;
            }

            internal IEnumerable<Post> Posts;
            internal IEnumerable<Reply> Replies;
            internal IEnumerable<User> Users;
            internal Topic Topic;
        }

        private static string ChannelString(Topic topic)
        {
            return "/topic/" + topic.Id;
        }

        private static string RequestString(Topic topic)
        {
            return "/t/" + topic.Id;
        }

        internal async Task<Tuple<int[], TopicResult>> DownloadTopicInitial(Topic topic)
        {
            topic.LatestMessage = -1;

            var channel = Utility.KeyValuePair(ChannelString(topic), "-1");
            var pollResult = await Post("message-bus/" + _guid + "/poll?dlp=t", _jsonRoot, new KeyValuePair<string, string>[] { channel });
            if (pollResult != null)
            {
                var message = Message.Decode(pollResult.First()) as StatusMessage;
                if (message != null) topic.LatestMessage = message.Statuses.Where(status => status.Key == ChannelString(topic)).First().Value;
            }

            var result = await Get(RequestString(topic), _jsonRoot, Utility.KeyValuePair("include_raw", "1"));
            if (result == null) return null;

            topic.Activity = (DateTime)result["last_posted_at"];
            topic.CategoryId = (int)result["category_id"];
            topic.Name = (string)result["title"];

            var poststream = result["post_stream"]["posts"];
            var stream = result["post_stream"]["stream"].Select(value => (int)value).ToArray();

            var posts = new Dictionary<int, Post>();
            var users = new Dictionary<int, User>();
            var replies = new List<Reply>();

            foreach (var token in poststream)
            {
                DecodePostToken(token, posts, users, replies);
            }

            return Tuple.Create(stream, new TopicResult(topic, posts, users, replies));
        }

        internal async Task<TopicResult> DownloadTopicStream(Topic topic, IEnumerable<int> stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var posts = new Dictionary<int, Post>();
            var users = new Dictionary<int, User>();
            var replies = new List<Reply>();

            foreach (var task in GetRawPostStream(RequestString(topic), stream))
            {
                var jarray = await task as JArray;
                if (jarray != null)
                {
                    foreach (var token in jarray)
                    {
                        DecodePostToken(token, posts, users, replies);
                    }
                }
                else
                {
                    topic.LatestMessage = -1;
                }
            }

            return new TopicResult(topic, posts, users, replies);
        }

        private const int POSTS_PER_REQUEST = 150;
        private static readonly string[] _postStreamPath = new string[] { "post_stream", "posts" };
        private IEnumerable<Task<JToken>> GetRawPostStream(string topicString, IEnumerable<int> _stream)
        {
            return _stream.Split(POSTS_PER_REQUEST).Select(stream =>
            {
                var streamParameters = stream.Select(id => Utility.KeyValuePair("post_ids[]", id.ToString())).ToList();
                streamParameters.Add(Utility.KeyValuePair("include_raw", "1"));

                return Get(topicString + "/posts", _postStreamPath, streamParameters.ToArray());
            });
        }

        private void DecodePostToken(JToken token, Dictionary<int, Post> posts, Dictionary<int, User> users, List<Reply> replies)
        {
            try
            {
                var user = ExtractUserFromPost(token);
                if (user != null) users[user.Id] = user;

                var post = DecodePost(token);

                if (token["raw"] == null)
                {
                    ;
                } 

                if (post != null)
                {
                    posts[post.PostNumberInTopic] = post;

                    if (token["reply_to_post_number"] != null && token["reply_to_post_number"].Type == JTokenType.Integer)
                    {
                        var reply = new Reply()
                        {
                            OriginalPostId = posts[(int)token["reply_to_post_number"]].Id,
                            ReplyPostId = (int)token["id"],
                        };
                        replies.Add(reply);
                    }
                }
            }
            catch (Exception é)
            {
                _logger.Log(é);
            }
        }

        private Post DecodePost(JToken post)
        {
            try
            {
                return new Post()
                {
                    Created = (DateTime)post["created_at"],
                    Deleted = post["deleted_at"] != null,
                    Html = (string)post["cooked"],
                    Text = (string)post["raw"],
                    Id = (int)post["id"],
                    PostNumberInTopic = (int)post["post_number"],
                    TopicId = (int)post["topic_id"],
                    UserId = (int)post["user_id"],
                };
            }
            catch (Exception é)
            {
                _logger.Log(é);
                return null;
            }
        }

        public const int AVATAR_SIZE = 64;
        private  User ExtractUserFromPost(JToken post)
        {
            try
            {
                return new User()
                {
                    Id = (int)post["user_id"],
                    AvatarPath = ((string)post["avatar_template"]).Replace("{size}", AVATAR_SIZE.ToString()),
                    Username = (string)post["username"],
                    DisplayName = (string)post["name"],
                    Title = (string)post["user_title"],
                };
            }
            catch (Exception é)
            {
                _logger.Log(é);
                return null;
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
            IEnumerable<JToken> result = await Get("post_actions/users", _jsonRoot,
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
            var result = await Get("posts/" + id, _jsonRoot);
            if (result == null) return null;
            return Tuple.Create(
                ExtractUserFromPost(result),
                DecodePost(result),
                result["reply_to_post_number"] == null ? null : new Reply()
                {
                    ReplyPostId = id,
                    OriginalPostId = (int)result["reply_to_post_number"]
                });
        }

        private static readonly string[] _categoryTopicsPath = new string[] { "topic_list" };
        internal async Task<Tuple<IEnumerable<Model.Topic>, bool>> GetCategoryPage(string path, int pageToGet)
        {
            var result = await Get("c/" + path, _categoryTopicsPath) as JObject;
            if (result == null) return new Tuple<IEnumerable<Model.Topic>, bool>(new Topic[0], false);
            var morePages = result["more_topics_url"] != null && result["more_topics_url"].Type != JTokenType.Null;

            return new Tuple<IEnumerable<Topic>, bool>(result["topics"].Select(topic => new Topic() {
                Id = (int)topic["id"],
                CategoryId = (int)topic["category_id"],
                Name = (string)topic["title"],
                Activity = (DateTime)topic["bumped_at"],
                LatestMessage = -1,
            }), morePages);
        }
    }
}
