﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Discouser
{
    class Command<TParameter> : ICommand
    {
        /// <summary>
        /// Creates a command that executes if the parameter is of the correct type
        /// </summary>
        public Command(Func<TParameter, Task> action) : this(anything => true, action) { }

        /// <summary>
        /// Creates a command that executes if the parameter is of the correct type and the predicate returns true
        /// </summary>
        public Command(Func<TParameter, bool> predicate, Func<TParameter,Task> action)
        {
            _predicate = predicate;
            _action = action;
        }

        private Func<TParameter, Task> _action;
        private Func<TParameter, bool> _predicate;

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
            return parameter is TParameter && _predicate((TParameter)parameter);
        }

        public async void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                await _action((TParameter)parameter);
            }
        }
    }

    class Command : ICommand
    {
        /// <summary>
        /// Creates a command that executes unconditionally
        /// </summary>
        public Command(Func<Task> action) : this(() => true, action) { }

        /// <summary>
        /// Creates a command that executes if the predicate returns true
        /// </summary>
        public Command(Func<bool> predicate, Func<Task> action)
        {
            _predicate = predicate;
            _action = action;
        }

        private Func<Task> _action;
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

        public async void Execute(object parameter)
        {
            if (CanExecute(null))
            {
                await _action();
            }
        }
    }
}