using Discouser.Data;
using SQLite;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Discouser.ViewModel
{
    class Category : ViewModelBase<Model.Category>
    {
        public Category(Model.Category model, DataContext context) : base(context, model) { }

        public override async Task Initialize()
        {
            if (!_model.Initialized)
            {
                await _context.InitializeCategory(_model);
            }
        }

        public override async Task NotifyChanges(Model.Category model)
        {
            model = model ?? await LoadModel();

            var topicsChanged = false;

            var topics = await _context.DbTransaction( db => db.Table<Model.Topic>()
                .Where(t => t.CategoryId == model.Id)
                .OrderByDescending(t => t.Activity)
                .ToList()
                .Select(t => new Topic(t, _context))
            );

            if (topics != null && topics.Any())
            {
                topicsChanged = _topics == null || !_topics.Any() || topics.First().Activity != _topics.First().Activity;
                if (topicsChanged)
                {
                    _topics = new ObservableCollection<Topic>(topics);
                }
            }

            if (model.Name != _model.Name || model.Description != _model.Description || topicsChanged )
            {
                _changedProperties = new string[] { topicsChanged ? "" : "Topics", model.Name == _model.Name ? "" : "Name", model.Description == _model.Description ? "" : "Description", };
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
