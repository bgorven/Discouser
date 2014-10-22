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

        public Category(Model.Category model, DataContext context) : base(model, context) { }

        public override void NotifyChanges(Model.Category model)
        {
            model = model ?? LoadModel();

            var topics =_context.PersistentDbConnection.Table<Model.Topic>()
                .Where(t => t.CategoryId == model.Id)
                .OrderByDescending(t => t.Activity)
                .ToList()
                .Select(t => new Topic(t, _context));

            var topicsChanged = topics.First().Activity != _topics.First().Activity;
            if (topicsChanged) _topics = new ObservableCollection<Topic>(topics);

            if (model.Name != _model.Name || model.Description != _model.Description || topicsChanged )
            {
                _changedProperties = new string[] { model.Name == _model.Name ? "" : "Name", model.Description == _model.Description ? "" : "Description", topicsChanged ? "" : "Topics" };
                Changes = true;
            }
        }

        public string Color
        {
            get { return _model.Color; }
            set
            {
                _model.Color = value;
                RaisePropertyChanged("Color");
            }
        }

        public string TextColor
        {
            get { return _model.TextColor; }
			set
            {
                _model.TextColor = value;
                RaisePropertyChanged("TextColor");
            }
        }

        public string Name
        {
            get { return _model.Name; }
			set
            {
                _model.Name = value;
                RaisePropertyChanged("Name");
            }
        }

        public string Description
        {
            get { return _model.Description; }
			set
            {
                _model.Description = value;
                RaisePropertyChanged("Description");
            }
        }

        private ObservableCollection<Topic> _topics;
        public ObservableCollection<Topic> Topics
        {
            get { return _topics; }
            set
            {
                _topics = value;
                RaisePropertyChanged("Topics");
            }
        }
    }
}
