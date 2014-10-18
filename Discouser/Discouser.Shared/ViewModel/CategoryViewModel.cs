using Discouser.Api;
using SQLite;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    class Category : ViewModelBase<Model.Category>
    {
        public Category(Model.Category model, SQLiteConnection db, ApiConnection api) : base(model, db, api) { }
        public Category(int id, SQLiteConnection db, ApiConnection api) : base(id, db, api) { }

        public override async Task Load()
        {
            _model = _db.Get<Model.Category>(_model.Id);
            Topics = await Task.Run(() =>
            {
                return new ObservableCollection<Topic>(
                    (_db.Table<Model.Topic>()
                    .Where(t => t.CategoryId == _model.Id)
                    .OrderByDescending(t => t.Activity)
                    .ToList())
                    .Select(t => new Topic(t, _db, _api)));
            });

            RaisePropertyChanged(new string[] { "Name", "Description", "Topics" });
            Changes = false;
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
