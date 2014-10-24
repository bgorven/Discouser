using Discouser.Data;
using SQLite;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    abstract class ViewModelBase<TModel> : INotifyPropertyChanged where TModel : Model.IModel, new()
    {
        protected DataContext _context;
        protected TModel _model;

        internal ViewModelBase()
        {
            LoadDataCommand = new Command(() => this.Changes, LoadData);
        }

        internal ViewModelBase(DataContext context) : this()
        {
            _context = context;
        }

        internal ViewModelBase(int id, DataContext context) : this(context)
        {
            _model = context.PersistentDbConnection.Get<TModel>(id);

        }

        internal ViewModelBase(TModel model, DataContext context) : this(context)
        {
            _model = model;
        }

        internal TModel LoadModel()
        {
            return _context.PersistentDbConnection.Get<TModel>(_model.Id);
        }

        /// <summary>
        /// <para>Called by the DataContext when there is a change available.</para>
        /// <para>Updates the underlying data, then sets <code>Changes</code> to true.</para>
        /// </summary>
        public virtual void NotifyChanges(TModel changedModel)
        {
            _model = changedModel;
            _changedProperties = GetType().GetTypeInfo().DeclaredProperties.Select(property => property.Name).ToArray();
        }

        private volatile bool _changes = false;
        /// <summary>
        /// Used by the UI to indicate that there are changes available.
        /// </summary>
        public bool Changes
        {
            get { return _changes; }
            set
            {
                _changes = value;
                RaisePropertyChanged("Changes");
            }
        }

        /// <summary>
        /// <para>Called by the UI when the user is ready to see the changed data.</para>
        /// <para>Raises property changed events for all properties changed by <code>NotifyChanges</code>, and clears 
        /// the <code>Changes</code> property.</para>
        /// </summary>
        public virtual Task LoadData()
        {
            RaisePropertyChanged();
            Changes = false;
            return null;
        }

        public Command LoadDataCommand { get; private set; }

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
