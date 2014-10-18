using Discouser.Api;
using SQLite;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    abstract class ViewModelBase<TModel> : INotifyPropertyChanged where TModel : Model.Model, new()
    {
        internal readonly SQLiteConnection _db;
        internal readonly ApiConnection _api;
        internal TModel _model;

        internal ViewModelBase(TModel model, SQLiteConnection db, ApiConnection api)
        {
            _api = api;
            _model = model;
            _db = db;
        }

        internal ViewModelBase(int id, SQLiteConnection db, ApiConnection api)
        {
            _model = db.Get<TModel>(id);
            _api = api;
            _db = db;
        }

        private bool _changes = false;
        internal bool Changes
        {
            get { return _changes; }
            set
            {
                _changes = true;
                RaisePropertyChanged("Changes");
            }
        }

        public abstract Task Load();

        public event PropertyChangedEventHandler PropertyChanged;

        internal void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        internal void RaisePropertyChanged(string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                RaisePropertyChanged(name);
            }
        }
    }
}
