using Discouser.Api;
using SQLite;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    class Post : ViewModelBase<Model.Post>
    {
        public Post(Model.Post model, SQLiteConnection db, ApiConnection api) : base(model, db, api) { }
        public Post(int id, SQLiteConnection db, ApiConnection api) : base(id, db, api) { }

        public override async Task Load()
        {
            var model = _db.Get<Model.Post>(_model.Id);

            var rawText = await Task.Run(() => new RawText(_db.Get<Model.RawText>(_model.Text), _db, _api).Text);

            if (rawText == _raw_text)
            {
                _model = model;
                _raw_text = rawText;
                RaisePropertyChanged(new string[] { "Text", "RepliesTo", "Html" });
            }

            var likeCount = await Task.Run(() => _db.ExecuteScalar<int>("SELECT COUNT(*) FROM SELECT like FROM likes WHERE like.PostId = ?", _model.Id));
            if (likeCount != _likeCount)
            {
                _likeCount = likeCount;
                RaisePropertyChanged(new string[] { "LikeCount", "Likes" });
            }

            var replyCount = await Task.Run(() => _db.ExecuteScalar<int>("SELECT COUNT(*) FROM SELECT reply FROM replies WHERE reply.OriginalPostId = ?", _model.Id));
            if (replyCount != _replyCount)
            {
                _replyCount = replyCount;
                RaisePropertyChanged(new string[] { "ReplyCount", "Replies" });
            }

            Changes = false;
        }

        public User User
        {
            get
            {
                return new User(_db.Get<Model.User>(_model.UserId), _db, _api);
            }
        }

        public DateTime Created
        {
            get { return _model.Created; }
        }

        private string _raw_text;
        public string RawText { get { return _raw_text; } }

        public string Html
        {
            get
            {
                return _api.GetPostHtml(_model.Id.ToString()).Result;
            }
        }

        private int _likeCount;
        public int LikeCount { get { return _likeCount; } }

        public ObservableCollection<Like> Likes
        {
            get
            {
                return new ObservableCollection<Like>(
                    _db.Table<Model.Like>()
                    .Where(l => l.PostId == _model.Id)
                    .ToList()
                    .Select(model => new Like(model, _db, _api)));
            }
        }

        private int _replyCount;
        public int ReplyCount { get { return _replyCount; } }

        public ObservableCollection<Post> Replies
        {
            get
            {
                return new ObservableCollection<Post>(
                    _db.Table<Model.Post>()
                    .Join(
                        _db.Table<Model.Reply>().Where(r => r.OriginalPostId == _model.Id),
                        post => post.Id,
                        reply => reply.ReplyPostId,
                        (post, reply) => post)
                    .ToList()
                    .Select(post => new Post(post, _db, _api)));
            }
        }

        public ObservableCollection<Post> RepliesTo
        {
            get
            {
                return new ObservableCollection<Post>(
                    _db.Table<Model.Post>()
                    .Join(
                        _db.Table<Model.Reply>().Where(r => r.ReplyPostId == _model.Id),
                        post => post.Id,
                        reply => reply.OriginalPostId,
                        (post, reply) => post)
                    .ToList()
                    .Select(post => new Post(post, _db, _api)));
            }
        }
    }
}
