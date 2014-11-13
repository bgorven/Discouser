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
                /*var repliesTo = Db.Table<Model.Reply>().Where(reply => reply.ReplyPostId == _model.Id);
                if (RepliesTo == null || repliesTo.Count() != RepliesTo.Count)
                {
                    RepliesTo = new ObservableCollection<Post>(from reply in repliesTo.AsEnumerable()
                                                               join post in Db.Table<Model.Post>() on reply.OriginalPostId equals post.Id
                                                               select new Post(post, _context));
                    changedProperties.Add("RepliesTo");
                }

                var replies = Db.Table<Model.Reply>().Where(reply => reply.OriginalPostId == _model.Id);
                if (Replies == null || replies.Count() != Replies.Count)
                {
                    Replies = new ObservableCollection<Post>(from reply in replies.AsEnumerable()
                                                             join post in Db.Table<Model.Post>() on reply.ReplyPostId equals post.Id
                                                             select new Post(post, _context));
                    changedProperties.Add("Replies");
                }

                var likes = Db.Table<Model.Like>().Where(like => like.PostId == _model.Id);
                if (Likes == null || likes.Count() != LikeCount)
                {
                    Likes = new ObservableCollection<User>(from like in likes.AsEnumerable()
                                                           join user in Db.Table<Model.User>() on like.UserId equals user.Id
                                                           select new User(user, null, _context));
                    changedProperties.Add("LikeCount");
                    changedProperties.Add("Likes");
                }*/

                if (User == null)
                {
                    var u = Db.Table<Model.User>().Where(user => user.Id == model.UserId);
                    if (u.Any())
                    {
                        User = new User(u.First(), null, _context);
                        changedProperties.Add("User");
                    }
                }
            });

            CanRefresh = changedProperties.Any();
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
