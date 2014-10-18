using Discouser.Api;
using SQLite;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    class Topic : ViewModelBase<Model.Topic>
    {
        public Topic(Model.Topic model, SQLiteConnection db, ApiConnection api) : base(model, db, api) { }
        public Topic(int id, SQLiteConnection db, ApiConnection api) : base(id, db, api) { }

        public override async Task Load()
        {
            _model = _db.Get<Model.Topic>(_model.Id);
            Posts = await Task.Run(() =>
            {
                return new ObservableCollection<Post>(
                    _db.Table<Model.Post>()
                    .Where(t => t.TopicId == _model.Id)
                    .OrderByDescending(t => t.Created)
                    .ToList()
                    .Select(p => new Post(p, _db, _api)));
            });

            RaisePropertyChanged(new string[] { "Name", "CategoryId", "Activity", "Posts" });
            Changes = false;
        }

        public int Id { get { return _model.Id; } }

        public string Name { get { return _model.Name; } }

        public int? CategoryId { get { return _model.CategoryId; } }

        public DateTime Activity { get { return _model.Activity; } }

        public int LatestMessage { get { return _model.LatestMessage; } }

        public ObservableCollection<Post> Posts { get; private set; }

        internal void UpdatePost(dynamic post_number)
        {
            throw new NotImplementedException();
        }

        internal void UpdatePostInfo(dynamic post_number)
        {
            throw new NotImplementedException();
        }

        internal void DeletePost(int postNumber)
        {
            throw new NotImplementedException();
        }
    }
}
