﻿#region Header

// <copyright file="StaticCommand.cs" company="Nito Programs">
//     Copyright (c) 2009 Nito Programs.
// </copyright>

#endregion Header

using System;
using System.Windows.Input;

namespace Torshify.Radio.Framework.Commands
{
    /// <summary>
    /// Delegate-based parameterless command that may be executed at any time.
    /// </summary>
    public sealed class StaticCommand : ICommand
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticCommand"/> class with a null delegate.
        /// </summary>
        public StaticCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticCommand"/> class with the specified delegate.
        /// </summary>
        /// <param name="execute">The delegate invoked to execute this command.</param>
        public StaticCommand(Action execute)
        {
            this.Execute = execute;
        }

        #endregion Constructors

        #region Events

        /// <summary>
        /// Unused. <see cref="StaticCommand"/> instances may always execute, so this event is never raised.
        /// </summary>
        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        #endregion Events

        #region Properties

        /// <summary>
        /// Gets or sets the delegate invoked to execute this command. Setting this does not raise <see cref="CanExecuteChanged"/>.
        /// </summary>
        public Action Execute
        {
            get; set;
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Determines whether this command can be executed. Always returns true, since <see cref="StaticCommand"/> instances may always execute.
        /// </summary>
        /// <param name="parameter">This parameter is ignored.</param>
        /// <returns>Always returns true.</returns>
        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Executes this command.
        /// </summary>
        /// <param name="parameter">This parameter is ignored.</param>
        void ICommand.Execute(object parameter)
        {
            this.Execute();
        }

        #endregion Methods
    }
}