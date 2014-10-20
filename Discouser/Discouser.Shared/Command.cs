using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Discouser
{
    class Command<TParameter> : ICommand
    {
        /// <summary>
        /// Creates a command that executes if the parameter is of the correct type
        /// </summary>
        public Command(Action<TParameter> action) : this(anything => true, action) { }

        /// <summary>
        /// Creates a command that executes if the parameter is of the correct type and the predicate returns true
        /// </summary>
        public Command(Func<TParameter, bool> predicate, Action<TParameter> action)
        {
            _predicate = predicate;
            _action = action;
        }

        private Action<TParameter> _action;
        private Func<TParameter, bool> _predicate;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return parameter is TParameter && _predicate((TParameter)parameter);
        }

        public void Execute(object parameter)
        {
            if (parameter is TParameter)
            {
                _action((TParameter)parameter);
            }
        }
    }

    class Command : ICommand
    {
        /// <summary>
        /// Creates a command that executes if the parameter is of the correct type
        /// </summary>
        public Command(Action action) : this(() => true, action) { }

        /// <summary>
        /// Creates a command that executes if the parameter is of the correct type and the predicate returns true
        /// </summary>
        public Command(Func<bool> predicate, Action action)
        {
            _predicate = predicate;
            _action = action;
        }

        private Action _action;
        private Func<bool> _predicate;

        public event EventHandler CanExecuteChanged;

        internal void Notify()
        {
            OnCanExecuteChanged();
        }

        protected virtual void OnCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            return _predicate();
        }

        public void Execute(object parameter)
        {
            _action();
        }
    }
}
