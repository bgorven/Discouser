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
        public Post(int id, DataContext context) : base(id, context) { }
        public Post(Model.Post model, DataContext context)
        {
            _model = model;
            _context = context;
        }

        public override void NotifyChanges(Model.Post model)
        {
            model = model ?? LoadModel();
            var changedProperties = new List<string>();

            var text = model.TextCache ?? new LongText(_model.Text, _context).Text;
            if (text != Text)
            {
                Text = text;
                changedProperties.Add("Text");
            }

            var html = model.HtmlCache ?? new LongText(_model.Html, _context).Text;
            if (text != Text)
            {
                Html = html;
                changedProperties.Add("Html");
            }

            if (_context.PersistentDbConnection.Table<Model.Reply>().Where(reply => reply.ReplyPostId == _model.Id).Count() != RepliesTo.Count)
            {
                RepliesTo = new ObservableCollection<Post>(_context.PersistentDbConnection.Table<Model.Reply>()
                    .Where(reply => reply.ReplyPostId == _model.Id).ToList()
                    .Select(reply => new Post(reply.OriginalPostId, _context)));
                changedProperties.Add("RepliesTo");
            }

            if (_context.PersistentDbConnection.Table<Model.Reply>().Where(reply => reply.OriginalPostId == _model.Id).Count() != Replies.Count)
            {
                Replies = new ObservableCollection<Post>(_context.PersistentDbConnection.Table<Model.Reply>()
                    .Where(reply => reply.OriginalPostId == _model.Id).ToList()
                    .Select(reply => new Post(reply.ReplyPostId, _context)));
                changedProperties.Add("Replies");
            }

            var likeCount = _context.PersistentDbConnection.Table<Model.Like>().Where(like => like.PostId == _model.Id).Count();
            if (likeCount != LikeCount)
            {
                RaisePropertyChanged(new string[] { "LikeCount", "Likes" });
            }

            Changes = false;
        }

        public User User { get { return new User(_model.UserId, _context); } }

        public DateTime Created
        {
            get { return _model.Created; }
			set
            {
                _model.Created = value;
                RaisePropertyChanged("Created");
            }
        }

        public string Text { get; private set; }

        public string Html { get; private set; }

        public int LikeCount { get { return Likes.Count; } }

        public ObservableCollection<User> Likes { get; private set; }

        public int ReplyCount { get { return Replies.Count; } }

        public ObservableCollection<Post> Replies { get; private set; }

        public ObservableCollection<Post> RepliesTo { get; private set; }
    }
}
