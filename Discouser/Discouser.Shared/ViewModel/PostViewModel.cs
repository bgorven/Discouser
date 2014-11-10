using Discouser.Data;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    class Post : ViewModelBase<Model.Post>
    {
        public Post(Model.Post model, DataContext context)
        {
            _model = model;
            _context = context;
        }

        public override async Task NotifyChanges(Model.Post model)
        {
            model = model ?? await LoadModel();
            var changedProperties = new List<string>();

            if (model.Text != _model.Text)
            {
                changedProperties.Add("Text");
            }

            if (model.Html != _model.Html)
            {
                changedProperties.Add("Html");
            }

            await _context.Transaction(Db =>
            {
                if (Db.Table<Model.Reply>().Where(reply => reply.ReplyPostId == _model.Id).Count() != RepliesTo.Count)
                {
                    RepliesTo = new ObservableCollection<Post>(Db.Table<Model.Reply>()
                        .Where(reply => reply.ReplyPostId == _model.Id).ToList()
                        .Select(reply => new Post(Db.Get<Model.Post>(reply.OriginalPostId), _context)));
                    changedProperties.Add("RepliesTo");
                }

                if (Db.Table<Model.Reply>().Where(reply => reply.OriginalPostId == _model.Id).Count() != Replies.Count)
                {
                    Replies = new ObservableCollection<Post>(Db.Table<Model.Reply>()
                        .Where(reply => reply.OriginalPostId == _model.Id).ToList()
                        .Select(reply => new Post(Db.Get<Model.Post>(reply.ReplyPostId), _context)));
                    changedProperties.Add("Replies");
                }

                var likeCount = Db.Table<Model.Like>().Where(like => like.PostId == _model.Id).Count();
                if (likeCount != LikeCount)
                {
                    RaisePropertyChanged(new string[] { "LikeCount", "Likes" });
                }

                if (User == null)
                {
                    User = new User(Db.Get<Model.User>(model.UserId), Db.Get<Model.UserInfo>(model.UserId), _context);
                    changedProperties.Add("User");
                }
            });

            Changes = changedProperties.Any();
            _changedProperties = changedProperties.ToArray();
        }

        public User User { get; private set; }

        public DateTime Created
        {
            get { return _model.Created; }
			set
            {
                _model.Created = value;
                RaisePropertyChanged("Created");
            }
        }

        public string Text { get { return _model.Text; } }

        public string Html { get { return _model.Html; } }

        public int LikeCount { get { return Likes.Count; } }

        public ObservableCollection<User> Likes { get; private set; }

        public int ReplyCount { get { return Replies.Count; } }

        public ObservableCollection<Post> Replies { get; private set; }

        public ObservableCollection<Post> RepliesTo { get; private set; }
    }
}
