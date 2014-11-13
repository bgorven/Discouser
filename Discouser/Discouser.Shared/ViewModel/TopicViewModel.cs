using Discouser.Data;
using SQLite;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Discouser.Model;

namespace Discouser.ViewModel
{
    class Topic : ViewModelBase<Model.Topic>
    {
        public Topic(Model.Topic model, DataContext context)
        {
            _model = model;
            _context = context;
        }

        public override async Task Initialize()
        {
            await _context.InitializeTopic(_model, Callback());
        }

        public override async Task NotifyChanges(Model.Topic model = null)
        {
            _model = model ?? await LoadModel();

            var postList = await _context.Transaction(Db => Db.Table<Model.Post>()
                    .Where(t => t.TopicId == _model.Id)
                    .OrderByDescending(t => t.Created)
                    .ToList());

            if (postList != null)
            {
                Posts = new ObservableCollection<Post>(postList.Select(p => new Post(p, _context)));
            }

            _changedProperties = new string[] { "Name", "CategoryId", "Activity", "Posts" };

            CanRefresh = true;
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
