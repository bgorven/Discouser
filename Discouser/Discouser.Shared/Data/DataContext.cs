using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discouser.Data;
using Discouser.Model;
using System.IO;
using Windows.Storage;

namespace Discouser.Data
{
    class DataContext : IDisposable
    {
        private string _dbString;

        internal ApiConnection Api { get; private set; }
        internal SQLiteConnection PersistentDbConnection { get; private set; }
        internal SQLiteConnection NewDbConnection() {  return new SQLiteConnection(_dbString); }
        internal Guid LocalGuid { get; private set; }
        internal TimeSpan PollDelay { get; private set; }
        internal string FolderName { get; private set; }

        public string Username { get; private set; }
        internal string SiteUrl { get; private set; }
        public string SiteName { get; private set; }
        public StorageFolder StorageDir { get; private set; }
        public Logger Logger { get; private set; }
        public Poller Poller { get; private set; }


        public DataContext(string url, string username, Guid guid)
        {
            SiteUrl = url;
            Username = username;
            LocalGuid = guid;
            Api = new ApiConnection(url, LocalGuid);
            FolderName = SiteUrl.Replace("http:", "").Replace("https:", "").Replace("/", "");
            Logger = new Logger();
            Poller = new Poller(this);
        }

        public DataContext() : this("meta.discourse.org", "", Guid.Empty) { }

        /// <summary>
        /// Attempts to login with the login username and password if supplied, or the authorization
        /// token stored in the HttpClient's cookies.
        /// </summary>
        /// <param name="username">The user name to log in with. If null, the contents of the Username property will be used. If the property
        /// is also null, the site will be queried and the name of the currently logged in session will be returned.
        /// </param>
        /// <param name="password">If username and password are not null, will log in using the supplied credentials.</param>
        /// <returns>The name of the logged in session, or null if no session is active.</returns>
        internal async Task<string> Authorize(string username = null, string password = null)
        {
            var login = username ?? Username;

            if (login != null && password != null)
            {
                await Api.PostSession(login: login, password: password);
            }

            var session = await Api.GetSessionCurrent();

            return (session ?? new Session()).Username;
        }

        internal async Task Initialize()
        {
            StorageDir = await ApplicationData.Current.RoamingFolder.CreateFolderAsync(FolderName, CreationCollisionOption.OpenIfExists);
            _dbString = Path.Combine(StorageDir.Path, Username + ".db");

            PersistentDbConnection = NewDbConnection();

            using (var db = NewDbConnection())
            {
                db.CreateTable<Category>();
                db.CreateTable<UserInfo>();
                db.CreateTable<Topic>();
                db.CreateTable<Reply>();
                db.CreateTable<Like>();
                db.CreateTable<Post>();
                db.CreateTable<User>();
                db.CreateTable<Site>();
            }
        }

        public async Task<IEnumerable<Category>> AllCategories()
        {
            var categories = await Api.GetCategories();

            using (var db = NewDbConnection())
            {
                db.InsertAll(categories, "OR REPLACE");

                return db.Table<Category>().ToList();
            }
        }

        internal void LatestTopicMessage(int topicId, int messageId)
        {
            using (var db = NewDbConnection())
            {
                var topic = db.Get<Topic>(topicId);
                if (topic != null)
                {
                    topic.LatestMessage = messageId;
                    db.Update(topic);
                }
            }
        }

        internal void DeletePost(int postId)
        {
            using (var db = NewDbConnection())
            {
                var post = db.Get<Post>(postId);
                if (post != null)
                {
                    post.Deleted = true;
                    db.Update(post);
                }
            }
        }

        internal async Task<Post> DownloadLikes(int id)
        {
            using (var db = NewDbConnection())
            {
                db.InsertAll(await Api.GetLikes(id), "OR REPLACE");
                return db.Get<Post>(id);
            }
        }

        internal async Task<Post> DownloadPost(int id)
        {
            using (var db = NewDbConnection())
            {
                db.InsertOrReplace(await Api.GetPost(id));
                return db.Get<Post>(id);
            }
        }

        public void Dispose()
        {
            if (PersistentDbConnection != null)
            {
                PersistentDbConnection.Dispose();
                PersistentDbConnection = null;
            }
            if (Api != null)
            {
                Api.Dispose();
                Api = null;
            }
        }
    }
}
