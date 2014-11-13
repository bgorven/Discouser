using Discouser.Data;
using SQLite;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    abstract class ViewModelBase<TModel> : IViewModel where TModel : class, Model.IModel, new()
    {
        protected DataContext _context;
        protected TModel _model;
        protected static readonly Task _nullTask = Task.Delay(0);

        internal ViewModelBase(DataContext context = null, TModel model = null)
        {
            _context = context;
            _model = model;
            RefreshCommand = new Command(() => this.CanRefresh, Refresh);
        }

        internal async Task<TModel> LoadModel()
        {
            return await _context.Transaction(db => db.Get<TModel>(_model.Id));
        }

        public virtual Task Initialize() { return _nullTask; }

        /// <summary>
        /// Creates a callback that executes <code>NotifyChanges</code> on the same thread as
        /// this method was originally called from.
        /// </summary>
        public Func<TModel, Task> Callback()
        {
            var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
            return async model => await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () => await NotifyChanges(model));
        }

        /// <summary>
        /// <para>Called by the DataContext when there is a change available.</para>
        /// <para>Updates the underlying data, then sets <code>Changes</code> to true.</para>
        /// </summary>
        public virtual async Task NotifyChanges(TModel changedModel = null)
        {
            _model = changedModel ?? await LoadModel();
            _changedProperties = GetType().GetTypeInfo().DeclaredProperties.Select(property => property.Name).ToArray();
        }

        private volatile bool _changes = false;
        public bool CanRefresh
        {
            get { return _changes; }
            set
            {
                _changes = value;
                RaisePropertyChanged("Changes");
            }
        }

        private bool _initialized = false;
        public bool Initialized
        {
            get { return _initialized; }
            set
            {
                _initialized = value;
                RaisePropertyChanged("Initialized");
            }
        }

        public virtual Task Refresh()
        {
            RaisePropertyChanged();
            CanRefresh = false;
            return _nullTask;
        }

        public virtual async void Loaded()
        {
            await OnLoad();
        }

        public virtual async Task OnLoad()
        {
            await Initialize();
            await NotifyChanges();
            if (CanRefresh) await Refresh();
            Initialized = true;
        }

        public async void Unloaded()
        {
            await OnUnload();
        }

        public virtual Task OnUnload()
        {
            return _nullTask;
        }

        public Command RefreshCommand { get; private set; }

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
