// $Id$
// (c) Pilikn Programmers Group

using System;
using System.Collections;
using System.Data;
using System.Text;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.ComponentModel;

namespace Front.ObjectModel {

	public interface IObject : IDataContainer, ICloneable {
		ClassDefinition Definition { get; }

		/// <summary>Возвращает слот по имени. Если слота нет - возвращает null. Можно использовать в качестве Contains(slotname)</summary>
		SlotDefinition GetSlot(string slotName);

		/// <summary>Возвращает список слотов (список слотов может не соответствовать списку слотов класса!)</summary>
		List<SlotDefinition> GetSlots();

		object GetSlotValue(string slotName);
		object SetSlotValue(string slotName, object value);

		event EventHandler<SlotChangeEventArgs> AfterSetSlotValue;
		event EventHandler<SlotChangeEventArgs> BeforeSetSlotValue;

		/// <summary>Событие вызывается если возникла ошибка при установке значения слота в том случае, если fixup не исправил ошибку!</summary>
		event EventHandler<SlotErrorEventArgs> AfterSlotError;

		new IObject Clone();
	}

	public interface IExtendable {
		T As<T>();
		object As(System.Type t);
		object As(string cname); // имя Extension'а в логической модели

		void Extend(object extension);
		void Shrink(System.Type t);

		// TODO: дополнить методами получения списка расширений (для рефлексии и диспечеризации)
	}


	/// <summary>Экземпляр класса (объект данных), хранит значения слотов</summary>
	public class LObject : LObjectBase {

		protected LObject() : base() {}

		protected LObject( bool initialize ) : base( initialize ) {
		}

		public LObject(ClassDefinition definition) : base(definition) { }

		protected override IDataContainer InitializeDataContainer() {
			// TODO: Это очень плохо!
			InnerDataContainer = new DataContainer();
			return InnerDataContainer;
		}
	}


	
}
