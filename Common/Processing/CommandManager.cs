using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.ComponentModel;

using Front.Collections.Generic;
using Front.Processing;

namespace Front.Forms {

	public class CommandManager : ICommandManager {
		#region Protected Fields
		protected Dictionary<Name, ICommand> InnerCommands = new Dictionary<Name, ICommand>();
		#endregion

		#region Constructors & Destructors
		public CommandManager() { }
		public CommandManager(IEnumerable<ICommand> commands) {
			foreach (ICommand command in commands)
				AddCommand(command);
		}
		public CommandManager(params ICommand[] commands) {
			foreach (ICommand command in commands)
				AddCommand(command);
		}
		#endregion

		#region Public Properties
		public int Count { get { return InnerCommands.Count; } }
		#endregion

		#region Public Events
		public event EventHandler<CommandEventArgs> AfterAdd;
		public event EventHandler<CommandEventArgs> AfterRemove;
		public event EventHandler AfterClear;
		public event EventHandler<CommandPropertyEventArgs> CommandPropertyChanged;
		#endregion

		#region Public Methods
		public virtual IEnumerable<ICommand> GetCommands() {
			return InnerCommands.Values;
		}

		public virtual ICommand GetCommand(Name name) {
			ICommand command = null;
			InnerCommands.TryGetValue(name, out command);
			return command;
		}

		public virtual void AddCommand(ICommand command) {
			if (command == null)
				Error.Warning(new ArgumentNullException("command"), typeof(ICommandManager));
			else {
				InnerCommands[command.Name] = command;
				if (command is INotifyPropertyChanged)
					((INotifyPropertyChanged)command).PropertyChanged += PropertyChangedHandler;

				OnAfterAdd(new CommandEventArgs(command));
			}
		}

		public virtual void RemoveCommand(ICommand command) {
			if (command == null)
				Error.Warning(new ArgumentNullException("command"), typeof(ICommandManager));
			else {
				RemoveCommand(command.Name);
			}
		}

		public virtual void RemoveCommand(Name name) {
			if (name == null)
				Error.Warning(new ArgumentNullException("name"), typeof(ICommandManager));
			else {
				if (InnerCommands.ContainsKey(name)) {
					ICommand command = GetCommand(name);
					if (command is INotifyPropertyChanged)
						((INotifyPropertyChanged)command).PropertyChanged -= PropertyChangedHandler;

					InnerCommands.Remove(name);
					OnAfterRemove(new CommandEventArgs(command));
				}
			}
		}

		public virtual void Replace(ICommand command) {
			if(command != null) {
				if (ContainsCommand(command))
					InnerCommands[command.Name] = command;
				else
					AddCommand(command);
			}
		}

		public virtual bool ContainsCommand(ICommand command) {
			if (command != null)
				return InnerCommands.ContainsKey(command.Name);
			else
				return false;
		}

		public virtual void Clear() {
			foreach (ICommand command in GetCommands())
				if (command is INotifyPropertyChanged)
					((INotifyPropertyChanged)command).PropertyChanged -= PropertyChangedHandler;

			InnerCommands.Clear();
			OnAfterClear();
		}

		public virtual bool Execute(string cmd) {
			return Execute(cmd, this, null);
		}

		public virtual bool Execute(string cmd, object sender) {
			return Execute(cmd, sender, null);
		}

		public virtual bool Execute(string cmd, object sender, object data) {
			ICommand cmd_x = GetCommand(cmd);
			if (cmd_x == null) 
				return false;
			else {
				if (sender == this && data == null)
					cmd_x.Execute();
				else if (data != null)
					cmd_x.Execute(data);
				else
					cmd_x.Execute(sender, data);
				return true;
			}
		}

		#endregion

		#region Protected Methods
		protected virtual void OnAfterAdd(CommandEventArgs args) {
			EventHandler<CommandEventArgs> handler = AfterAdd;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterRemove(CommandEventArgs args) {
			EventHandler<CommandEventArgs> handler = AfterRemove;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterClear() {
			EventHandler handler = AfterClear;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected virtual void PropertyChangedHandler(object sender, PropertyChangedEventArgs args) {
			OnCommandPropertyChanged(new CommandPropertyEventArgs(sender as ICommand, args.PropertyName));
		}

		protected virtual void OnCommandPropertyChanged(CommandPropertyEventArgs args) {
			EventHandler<CommandPropertyEventArgs> handler = CommandPropertyChanged;
			if (handler != null)
				handler(this, args);
		}
		#endregion
	}



}
