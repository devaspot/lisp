// $Id$
// (c) Pilikn Programmers Group


using System;
using System.Collections;
using System.Data;
using System.Text;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Front.ObjectModel {

	// Поведение:
	//	* может расширять набор слотов класса (или объекта)
	//	* задает методы доступа к полям объекта
	//	* переопределяет или дополняет набор методов объекта
	//	* может ззадаваться для класса или для отдельного объекта
	//	* может зависеть от контекста объекта или контекста исполнения
	//	* может применяться для одного или нескольких полей или методов объекта
	//		(вариант использования поведения в качестве "аспекта")
	//	* поведение может состоять из нескольких поведений 
	//		(например поведени, которое устанавливает несколько поведений для полей)
	// TODO: для начала разберемся с методами и поведением для класса

	//  Поведение на лиспе задается как набор функций, которые применяются или отменяются
	//  для объекта/класса совместно.
	//	хотя в дальнейшем, каждую из них можно переопределить в отдельности.
	//	можно так же доопределить поведение новыми функциями - тогда они появятся во
	//	всех классах/объектах, где используется данное поведение.


	/// <summary>Интерфейс для объектов, наделенных поведением</summary>
	/// <remarks>Поведение может быть задано на уровне описания, но можеты быть изменено для конкретного объекта.</remarks>
	public interface IBehaviored {

		/// <summary>ссылка на элемент описания, для которого описываются поведения (может отсутствовать, если поведение задано на уровне объекта)</summary>
		SchemeNode SchemeNode { get; }

		bool HasBehavior(string name);

		ObjectBehavior GetBehavior(string name);
		MethodDefinition[] GetMethod(string name, params object[] args);
		MethodDefinition[] GetMethod(string name, params Type[] args);

		object Invoke(string method, params object[] args);
		bool CanInvoke(string method);
	}

	public interface IBehavioredObject : IObject, IBehaviored {
	}


	/// <summary>Признак того, что объект является оберткой вокруг объекта с данными (при этом он может реализовать интерфейс объекта данных)</summary>
	public interface IObjectWrapper {
		IObject DataObject {
			get;
		}
	}


	// TODO: IExtendable декларируется в Lisp, но в лисп нету ни одной его реализации
	// (кроме прокси в ObjectBehavior)

	/// <summary>Обертка вокруг объекта.</summary>
	[Serializable]
	public class ObjectBehavior : IObjectWrapper, IExtendable, IBehavioredObject, ISerializable {

		#region Protected Fields
		//.........................................................................
		protected IObject InnerObject;
		//.........................................................................
		#endregion
		
		#region Public Properties
		//.........................................................................
		public virtual IObject DataObject {
			get { return InnerObject; }
		}
		//.........................................................................
		#endregion

		#region Constructors
		//.........................................................................
		protected ObjectBehavior() { }

		protected ObjectBehavior(IObject obj) {
			InnerObject = obj;
		}

		protected ObjectBehavior(SerializationInfo info, StreamingContext context) {
			InnerObject = info.GetValue("InnerObject", typeof(IObject)) as IObject;
		}

		public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
			info.AddValue("InnerObject", InnerObject);
		}
		//.........................................................................
		#endregion

		#region IExtendable (редирект на InnerObject)
		//................................................................
		public virtual T As<T>() {
			IExtendable x = InnerObject as IExtendable;
			return (x != null)? x.As<T>() : default(T);
		}

		public virtual object As(string cname) {
			IExtendable x = InnerObject as IExtendable;
			return (x != null)? x.As(cname) : null;
		}

		public virtual object As(System.Type t) {
			IExtendable x = InnerObject as IExtendable;
			return (x != null)? x.As(t) : null;
		}

		public virtual void Extend(object extention) {
			IExtendable x = InnerObject as IExtendable;
			if (x != null) x.Extend(extention);
		}

		public virtual void Shrink(System.Type t) {
			IExtendable x = InnerObject as IExtendable;
			if (x != null) x.Shrink(t); 
		}
		//................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual ObjectBehavior CloneBehavior() {
			return CloneBehavior(null);
		}

		public virtual ObjectBehavior CloneBehavior(IObject nut) {
			ObjectBehavior res = (ObjectBehavior)MemberwiseClone();
			res.InnerObject = nut;
			OnCloneBehavior(res, nut);
			if (nut != null)
				res.OnAfterAttache();
			return res;
		}

		public virtual object CallNextMethod(params object[] args) {
			return BehaviorDispatcher.CallNextMethod(args);
		}
		//.........................................................................
		#endregion


		#region IObject Members
		//.........................................................................
		public ClassDefinition Definition {
			get { return InnerObject.Definition; }
		}

		public virtual SlotDefinition GetSlot(string slotName) {
			return InnerObject.GetSlot(slotName);
		}

		public virtual List<SlotDefinition> GetSlots() {
			return InnerObject.GetSlots();
		}

		public virtual object GetSlotValue(string slotName) {
			return InnerObject.GetSlotValue(slotName);
		}

		public object SetSlotValue(string slotName, object value) {
			return InnerObject.SetSlotValue(slotName, value);
		}

		public event EventHandler<SlotChangeEventArgs> AfterSetSlotValue {
			add { InnerObject.AfterSetSlotValue += value; }
			remove { InnerObject.AfterSetSlotValue -= value; }
		}

		public event EventHandler<SlotChangeEventArgs> BeforeSetSlotValue {
			add { InnerObject.BeforeSetSlotValue += value; }
			remove { InnerObject.BeforeSetSlotValue -= value; }
		}

		public event EventHandler<SlotErrorEventArgs> AfterSlotError {
			add { InnerObject.AfterSlotError += value; }
			remove { InnerObject.AfterSlotError -= value; }
		}

		public IObject Clone() {
			return InnerObject.Clone();
		}
		//.........................................................................
		#endregion


		#region IDataContainer Members
		//.........................................................................
		public virtual object RawGetSlotValue(string slotName) {
			return InnerObject.RawGetSlotValue(slotName);
		}

		public virtual object RawSetSlotValue(string slotName, object value) {
			return InnerObject.RawSetSlotValue(slotName, value);
		}
		//.........................................................................
		#endregion


		#region ICloneable Members
		//.........................................................................
		object ICloneable.Clone() {
			return CloneBehavior();
		}
		//.........................................................................
		#endregion


		#region IBehaviored Members
		//.........................................................................
		public SchemeNode SchemeNode {
			get { 
				IBehaviored o = InnerObject as IBehaviored;
				return o != null ? o.SchemeNode : null;
			}
		}

		public virtual bool HasBehavior(string name) {
			IBehaviored o = InnerObject as IBehaviored;
			return o != null ? o.HasBehavior(name) : false;
		}

		public ObjectBehavior GetBehavior(string name) {
			IBehaviored o = InnerObject as IBehaviored;
			return o != null ? o.GetBehavior(name) : null;
		}

		public MethodDefinition[] GetMethod(string name, params object[] args) {
			IBehaviored o = InnerObject as IBehaviored;
			return o != null ? o.GetMethod(name, args) : null;
		}

		public MethodDefinition[] GetMethod(string name, params Type[] args) {
			IBehaviored o = InnerObject as IBehaviored;
			return o != null ? o.GetMethod(name, args) : null;
		}

		public object Invoke(string method, params object[] args) {
			IBehaviored o = InnerObject as IBehaviored;
			return o != null ? o.Invoke(method, args) : null;
		}

		public bool CanInvoke(string method) {
			IBehaviored o = InnerObject as IBehaviored;
			return o != null ? o.CanInvoke(method) : false;
		}
		//.........................................................................
		#endregion

		/// <summary>Вызывается после связывания бихейвера с Nut.</summary>		
		protected virtual void OnAfterAttache() {
		}

		protected virtual ObjectBehavior OnCloneBehavior(ObjectBehavior res, IObject nut) {
			return res;
		}
	}
}
