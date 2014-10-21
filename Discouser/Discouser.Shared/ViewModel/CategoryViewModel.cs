using Discouser.Api;
using SQLite;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Discouser.ViewModel
{
    class Category : ViewModelBase<Model.Category>
    {
        public Category(int id, DataContext context) : base(id, context) { }

        public override void NotifyChanges(Model.Category model)
        {
            model = model ?? LoadModel();

            var topics = new ObservableCollection<Topic>(
                    _context.PersistentDbConnection.Table<Model.Topic>()
                        .Where(t => t.CategoryId == model.Id)
                        .OrderByDescending(t => t.Activity)
                        .ToList()
                        .Select(t => new Topic(t, _context)));

            if (model.Name != _model.Name || model.Description != _model.Description || topics.First().Activity != Topics.First().Activity )
            {
                _changedProperties = new string[] { model.Name == _model.Name ? "" : "Name", model.Description == _model.Description ? "" : "Description", topics.First().Activity == Topics.First().Activity ? "" : "Topics" };
                Changes = true;
            }
        }

        public string Name
        {
            get { return _model.Name; }
            private set { }
        }

        public string Description
        {
            get { return _model.Description; }
            private set { }
        }


        public ObservableCollection<Topic> Topics { get; private set; }
    }
}
