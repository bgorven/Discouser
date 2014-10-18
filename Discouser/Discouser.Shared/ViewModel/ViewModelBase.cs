using Discouser.Api;
using SQLite;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    abstract class ViewModelBase<TModel> : INotifyPropertyChanged where TModel : Model.Model, new()
    {
        protected DataContext _context;
        protected TModel _model;

        internal ViewModelBase() { }

        internal ViewModelBase(int id, DataContext context)
        {
            _context = context;
            _model = _context.Db.Get<TModel>(id);
        }

        internal TModel LoadModel()
        {
            return _context.Db.Get<TModel>(_model.Id);
        }

        private volatile bool _changes = false;
        /// <summary>
        /// Used by the UI to indicate that there are changes available to load.
        /// </summary>
        internal bool Changes
        {
            get { return _changes; }
            set
            {
                _changes = true;
                RaisePropertyChanged("Changes");
            }
        }

        /// <summary>
        /// Called by the DataContext when there is a change available.
        /// </summary>
        public abstract void NotifyChanges(TModel model);

        /// <summary>
        /// Called by the UI when the user is ready to see the changed data.
        /// </summary>
        public virtual Task LoadChanges()
        {
            RaisePropertyChanged();
            Changes = false;
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected string[] _changedProperties;

        internal void RaisePropertyChanged()
        {
            RaisePropertyChanged(_changedProperties);
        }

        internal void RaisePropertyChanged(string[] changedProperties)
        {
            if (changedProperties != null)
            {
                foreach (var name in changedProperties)
                {
                    RaisePropertyChanged(name);
                }
            }
        }

        internal void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null && !string.IsNullOrEmpty(propertyName))
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
