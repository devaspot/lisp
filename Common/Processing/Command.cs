using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Front.Processing {
	
	/* Связь команд и событий:
	 *   if (Command.Name == Event.Name)
	 *		Command.Execute( Event.Sender, Event.Args );
	 * 
	 * TODO: нуж как-то совместить EventProcessor и CommandActionEventHandler
	 * 
	 */

	/// <summary>Контейнер обработчика команды или события</summary>
	public interface ICommand {
		Name Name { get; }
		string Text { get; set; }
		bool Enabled { get; set; }
		object Data { get; set; }

		bool Visible { get; set; }
		string ToolTip { get; set; }

		// TODO: продумать работу с картинками,
		// их нужно задать целый набор для разных состояний,
		// пир этом могут быть картинки разного размера
		// (а вообще, они должны быть где-то в районе Front.Forms, 
		// либо там должна быть своя команда-потомок
		string Image { get; set; }

		void Execute();
		void Execute(object sender);
		void Execute(object sender, object data);
		void Execute(object sender, params object[] data);
	}

	public interface ICommandManager {
		IEnumerable<ICommand> GetCommands();
		ICommand GetCommand(Name name);
		void AddCommand(ICommand command);
		void RemoveCommand(ICommand command);
		void RemoveCommand(Name name);
		void Clear();

		int Count { get; }

		event EventHandler<CommandEventArgs> AfterAdd;
		event EventHandler<CommandEventArgs> AfterRemove;
		event EventHandler AfterClear;
		// TODO (Pilya): странное название, возможно стоит переименовать!
		event EventHandler<CommandPropertyEventArgs> CommandPropertyChanged;

		bool Execute(string cmd);
		bool Execute(string cmd, object sender); // TODO: может не void а bool?
		bool Execute(string cmd, object sender, object data);
	}

	public interface ICommandGroup : ICommand {
		IList<ICommand> Commands { get; }
		ICommand Current { get; set; }
	}


	/// <summary>обработчик комманд. </summary>
	public delegate void CommandActionEventHandler(object sender, Front.Processing.CommandEventArgs args);



	public class Command : ICommand, INotifyPropertyChanged{
		#region Protected Fields
		//..................................................................
		protected Name InnerName;
		protected string InnerText = "";
		protected bool InnerEnabled = true;
		protected string InnerDisabledReason = "";

		protected bool InnerVisible = true;
		protected string InnerToolTip = "";
		protected string InnerImage = "";

		/// <summary>Вспомагательный "карман", который будет передаваться обработчику</summary>
		protected object InnerData = null; // XXX может WeakRefference?

		// TODO: Это можно сделать публичным событием и описать в интерфейсе
		protected CommandActionEventHandler InnerAction;
		//..................................................................
		#endregion

		#region Constructors
		//..................................................................
		public Command(Name name, CommandActionEventHandler action)
			: this(name, null, null, null, action) { }

		public Command(Name name, string text, string image) 
			: this(name, text, image, null, (CommandActionEventHandler)null) { }

		public Command(Name name, string text, CommandActionEventHandler action)
			: this(name, text, null, null, action) { }

		public Command(Name name, string text, string image, CommandActionEventHandler action)
			: this(name, text, image, null, action) { }

		public Command(Name name, string text, string image, string tooltip)
			: this(name, text, image, tooltip, (CommandActionEventHandler)null) { }

		//public Command(Name name, string text, string image, string tooltip, CommandActionBase action) 
		//    : this(name, text, image, tooltip, (action != null) ? new CommandActionEventHandler(action.Execute) : null ) { }

		public Command(Name name, string text, string image, string tooltip, CommandActionEventHandler action) {
			InnerName = name;
			InnerText = text;
			InnerImage = (image ?? DefaultImage);
			InnerToolTip = (tooltip ?? text);
			InnerAction = action;
			InnerEnabled = true;
		}
		//..................................................................
		#endregion

		#region Public Properties
		//..................................................................
		public static string DefaultImage = "chrystal_ball.png";

		public Name Name { 
			get { return InnerName; } 
		}

		public string Text { 
			get { return InnerText; } 
			set { InnerText = value; OnNotifyPropertyChanged("Text"); }
		}

		public bool Enabled { 
			get { return InnerEnabled; } 
			set { InnerEnabled = value; OnNotifyPropertyChanged("Enabled"); } 
		}

		public bool Visible { 
			get { return InnerVisible; } 
			set { InnerVisible = value; OnNotifyPropertyChanged("Visible"); } 
		}

		public string ToolTip { 
			get {
				if (!Enabled) return DisabledReason;
				return InnerToolTip; 
			} 
			set { InnerToolTip = value; OnNotifyPropertyChanged("Tooltip"); } 
		}

		public string DisabledReason {
			get { return InnerDisabledReason; }
			set { InnerDisabledReason = value; }
		}

		public string Image { 
			get { return InnerImage; } 
			set { InnerImage = value; OnNotifyPropertyChanged("Image"); } 
		}

		public CommandActionEventHandler Action { 
			get { return InnerAction; } 
			set { InnerAction = value; } 
		}

		public object Data {
			get { return InnerData; }
			set { InnerData = value; }
		}

		//..................................................................
		#endregion

		#region Public Events
		//..................................................................
		public event PropertyChangedEventHandler PropertyChanged;
		//..................................................................
		#endregion

		#region Public Methods
		//..................................................................
		public virtual void Execute() {
			Execute(null);
		}

		public virtual void Execute(object sender) {
			Execute(sender, InnerData);
		}

		public virtual void Execute(object sender, object data) {
			if (Enabled) {
				CommandActionEventHandler action = InnerAction;
				if (action != null) {
					action(sender, new CommandEventArgs(this, data)); 
				}
			}
		}

		public virtual void Execute(object sender, params object[] args) {
			if (Enabled) {
				CommandActionEventHandler action = InnerAction;
				if (action != null) {
					action(sender, new CommandEventArgs(this, null, args)); 
				}
			}
		}
		//..................................................................
		#endregion

		#region Protected Methods
		//..................................................................
		protected virtual void OnNotifyPropertyChanged(string name) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(name));
		}
		//..................................................................
		#endregion
	}	


	// TODO: можен унаследовать его от CommandWrapper?
	public class CommandGroup : ICommandGroup, INotifyPropertyChanged {
		#region Protected Fields
		// TODO А может здесь тоже ICommandManager?
		// TODO Или группировка - отдельная сущность - не комманда, хотя почему бы и нет...
		protected List<ICommand> InnerCommands = new List<ICommand>();
		protected ICommand InnerCurrent;
		protected bool InnerEnabled = true;
		protected bool InnerVisible = true;
		protected Name InnerName;
		protected object InnerData;
		#endregion

		#region Constructors
		public CommandGroup(Name name, ICommand current, IEnumerable<ICommand> commands) {
			if (current == null)
				Error.Warning(new ArgumentNullException("current"), typeof(ICommandGroup));
			if (name == null)
				Error.Warning(new ArgumentNullException("name"), typeof(ICommandGroup));

			InnerName = name;
			InnerCurrent = current;
			if (commands != null)
				InnerCommands.AddRange(commands);
		}
		#endregion

		#region Public Properties
		public IList<ICommand> Commands { 
			get { return InnerCommands; } 
		}

		public ICommand Current { 
			get { return InnerCurrent; } 
			set { SetCurrent(value); } 
		}

		public Name Name { 
			get { return InnerName; } 
		}

		public string Text { 
			get { return InnerCurrent.Text; } 
			set { InnerCurrent.Text = value; } 
		}

		public string ToolTip { 
			get { return InnerCurrent.ToolTip; } 
			set { InnerCurrent.ToolTip = value; } 
		}

		public string Image { 
			get { return InnerCurrent.Image; } 
			set { InnerCurrent.Image = value; } 
		}

		public bool Enabled { 
			get { return InnerEnabled; } 
			set { InnerEnabled = value; OnNotifyPropertyChanged("Enabled"); } 
		}

		public bool Visible { 
			get { return InnerVisible; } 
			set { InnerVisible = value; OnNotifyPropertyChanged("Visible"); } 
		}

		public object Data {
			get { return (InnerCurrent != null) ? InnerCurrent.Data : InnerData; }
			set { 
				if (InnerCurrent != null) 
					InnerCurrent.Data = value; 
				else 
					InnerData = value; 
			}
		}
		#endregion

		#region Public Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region Public Methods
		public virtual void Execute() {
			Execute(null);
		}

		public virtual void Execute(object sender) {
			Execute(sender, null);
		}

		public virtual void Execute(object sender, object data) {
			if (InnerCurrent != null)
				InnerCurrent.Execute(sender, data);
		}

		public virtual void Execute(object sender, params object[] data) {
			if (InnerCurrent != null)
				InnerCurrent.Execute(sender, data);
		}

		#endregion 

		#region Protected Methods
		protected virtual void OnNotifyPropertyChanged(string name) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(name));
		}

		protected virtual void OnCurrentPropertyChanged(object sender, PropertyChangedEventArgs args) {
			OnNotifyPropertyChanged(args.PropertyName);
		}

		protected virtual void SetCurrent(ICommand command) {
			if (command == null)
				Error.Warning(new ArgumentNullException("current"), typeof(ICommand));

			if (InnerCurrent is INotifyPropertyChanged) {
				((INotifyPropertyChanged)InnerCurrent).PropertyChanged -= OnCurrentPropertyChanged;
			}
			InnerCurrent = command; 
			OnNotifyPropertyChanged("Current");
			if (InnerCurrent is INotifyPropertyChanged) {
				((INotifyPropertyChanged)InnerCurrent).PropertyChanged += OnCurrentPropertyChanged;
			}
		}
		#endregion
	}


	public class CommandEventArgs : EventArgs {
		protected ICommand InnerCommand;
		protected object InnerData;
		protected object[] InnerArgs;
		protected bool InnerCancel = false;

		public CommandEventArgs(ICommand command) {
			InnerCommand = command;
			InnerData = (InnerCommand != null) ? InnerCommand.Data : null;
		}

		public CommandEventArgs(ICommand command, object data) : this(command) {
			if (data != null)	
				InnerData = data;
		}

		public CommandEventArgs(ICommand command, object data, params object[] args) : this(command, data) {
			InnerArgs = args;
		}

		public ICommand Command { get { return InnerCommand; } }

		public object Data {
			get { return InnerData; }
			set { InnerData = value; }
		}

		public bool Cancel {
			get { return InnerCancel; }
			set { InnerCancel = value; }
		}
	}

	public class CommandPropertyEventArgs : CommandEventArgs {
		protected string InnerPropertyName;

		public CommandPropertyEventArgs(ICommand command, string propertyName) : base(command) {
			InnerPropertyName = propertyName;
		}

		public string PropertyName { get { return InnerPropertyName; } }
	}


}
