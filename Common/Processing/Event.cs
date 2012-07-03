using System;
using System.Collections.Generic;
using System.Text;

namespace Front.Processing {

	// TODO: продумать механизм написания обработчиков событий!
	// TODO: подружить EventQ/Event с Command/CommandActionEventHandler

	// делегат возвращает признак "можно ли выполнять события дальше"
	// (тоесть, обычно - true, и false - если режем обработку для более детальных имен)
	public delegate bool EventProcessor(Event e);


	[Serializable]
	/// <summary>
	/// Объект, который помещается в очередь обработки или передается 
	/// в качестве сигнала о возникновении события.
	/// </summary>
	public class Event {
		// TODO: ID нужно генерить
		public long ID; // это зачем?
		
		/// <summary>Имеет формат [name.space].[код]
		/// обработка событий может производиться для всего namespace-а
		/// </summary>
		public string Name;

		public string SessionID; // TODO: этого туту не должно быть!
		public int Status; // XXX Это что? почему int а не enum?

		public object Sender;
		public object[] Args;


		protected Event() {}

		public Event(object sender, string name): this (sender, name, new object[]{}) {
		}

		public Event(object sender, string name, params object[] args) {
			Sender = sender;
			Name = name;
			Args = args;
		}
	}
}