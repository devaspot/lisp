// $Id$

using System;
using System.Collections;
using System.Data;
using System.Text;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Front.ObjectModel {

	
	/// <summary>Базовый класс для объектов данных. Задает механизм доступа к полям, но не реализует схему хранения данных</summary> 
	public abstract class LObjectBase : ISerializable, IBehavioredObject {

		/* возможны варианты хранения: 
		 *    1. Массив, с индексами в ClassDefinition, 
		 *    2. Hashtable
		 *    3. Массив + Hashtable
		 *    4. DataRow + DataSet ?
		 * 
		 * 	TODO: подумать, как будем отличать null, DBNull, Empty, Uninitialized и Default значения
		 */

		#region Protected Properties
		//................................................................
		protected ClassDefinition InnerClass;				
		protected BehaviorDispatcher InnerBehavior;			
		protected IDataContainer InnerDataContainer;		
		//................................................................
		#endregion


		#region Constructors
		//................................................................
		protected LObjectBase() : this( true ) {
		}

		protected LObjectBase( bool initialize ) {
			if (initialize)				
				InitializeDataContainer();
		}

		protected LObjectBase( ClassDefinition cls , IDataContainer container) : this(false) {
			// TODO: может какраз тут и создавать ClassWrapper?

			UpdateMetaInfo(cls);

			if (container != null)
				InnerDataContainer = container;
			else
				InitializeDataContainer();
		}

		protected LObjectBase( ClassDefinition cls ) : this( cls, true) {
		}

		protected LObjectBase( ClassDefinition cls, bool init ) : this(cls, null) { 
			// XXX С конструкторами лажа!!!
		}

		protected LObjectBase(SerializationInfo info, StreamingContext context)  {
			string className = info.GetString("InnerClassName");
			UpdateMetaInfo( MetaInfo.Current.GetClass(className) );
			// TODO: получить CustomExtentions, только их нужно будет тоже передавать в Update, что бы правильно нацепились бихейверы!
			InnerDataContainer = info.GetValue("DataContainer", typeof(IDataContainer)) as IDataContainer;
		}

		public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
			// TODO: написать!
			//InnerClass ередаем только ClassName
			info.AddValue("InnerClassName", InnerClass.Name); 

			// TODO: класс объеквта - это "обертка" над классом мета-информации
			//    1. сейчас он имеет калечное название типа "class1-instance", и нет нормальной возможности получить чистое имя класса
			//    2. класс инстанса может иметь специфические расширения, которых нет в Metailnfo-классе, и их список нужно дополнительно
			//       сериализовать!
			// info.AddValue("CustomExtentions", GetCustomExtentions(InnerClass));
			
			info.AddValue("DataContainer", InnerDataContainer);
		}
		//................................................................
		#endregion


		public event EventHandler<SlotChangeEventArgs> AfterSetSlotValue;
		public event EventHandler<SlotChangeEventArgs> BeforeSetSlotValue;
		public event EventHandler<SlotErrorEventArgs> AfterSlotError;


		#region Public Properties
		//................................................................
		public ClassDefinition Definition { 
			get { return InnerClass; }
		}

		public virtual SchemeNode SchemeNode { 
			get { return InnerClass; }
		}

		public object this[string slotName] {
			get { return GetSlotValue(slotName); }
			set { SetSlotValue(slotName, value); }
		}
		//................................................................
		#endregion


		#region IObject Methods
		//................................................................
		public virtual SlotDefinition GetSlot(string slotName) {
			if (InnerClass != null)
				return InnerClass.GetSlot(slotName);
			return null;
		}

		public virtual List<SlotDefinition> GetSlots() {
			if (InnerClass != null)
				return InnerClass.GetSlots();
			return null;
		}

		public virtual object GetSlotValue(string pname) {
			SlotReaderDelegate r = GetReader(pname);
			if (r == null)
				return RawGetSlotValue(pname);
			else
				return r(this, pname);
		}

		public virtual object SetSlotValue(string pname, object value) {
			object original = null;
			try {
				original = RawGetSlotValue(pname); 
			} catch (Exception ex) {
				// TODO: это не хорошо! должен быть какой-то TryRawGetSlotValue!
			}

			if (OnBeforeSetSlotValue(pname, original, value)) {
				SlotWriterDelegate w = GetWriter(pname);
				try {
					Object nv = (w != null) 
						? w(this, pname, value) 
						: RawSetSlotValue(pname, value);
					OnAfterSetSlotValue(pname, original, nv);
					return nv;
				} catch (Exception ex) { // XXX  Очень плохо тут душить исключения!!!
					OnSlotError(pname, original, value, ex);
				}
			}
			return null;
		}
		//................................................................
		#endregion


		#region IDataContainer methods
		//................................................................
		public virtual object RawGetSlotValue(string slotName) {
			return InnerDataContainer.RawGetSlotValue(slotName);
		}

		public virtual object RawSetSlotValue(string slotName, object value) {
			return InnerDataContainer.RawSetSlotValue(slotName, value);
		}
		//................................................................
		#endregion


		#region IObject Members
		//.........................................................................		
		public virtual IObject Clone() {
			// TODO: написать!
			throw new NotImplementedException();
		}

		object ICloneable.Clone() {
			return Clone();
		}
		//.........................................................................
		#endregion


		#region IBehaviored implementation
		//................................................................
		public virtual bool HasBehavior(string name) {
			return (GetBehavior(name) != null);
		}

		public virtual ObjectBehavior GetBehavior(string name) {
			// TODO: поискать у себя, потом спросить у класса...
			// но похоже наклевывается кокой-то Mixer:
			// ускать все у себя, в контексте вызова. в контексте объекта и у класса...
			// возможно BehaviorDispatcher это все спрячет в себе, тогда искать нужно только у себя!
			return InnerBehavior.GetBehavior(name) as ObjectBehavior;
		}

		public virtual MethodDefinition[] GetMethod(string name, params object[] args) {
			// TODO: написать (см. GetBehavior)
			throw new NotImplementedException();
		}

		public virtual MethodDefinition[] GetMethod(string name, params Type[] args) {
			// TODO: написать (см. GetBehavior)
			throw new NotImplementedException();
		}

		public virtual object Invoke(string method, params object[] args) {
			return InnerBehavior.Invoke(method, args);
		}

		public virtual bool CanInvoke(string method) {
			return InnerBehavior.CanInvoke(method);
		}

		public virtual ClassDefinition UpdateMetaInfo(ClassDefinition cls) {
			if (cls != null) {
				InnerClass = new ClassDefinition(cls, cls.Name + "-instance", false, new ClassDefinition[] { cls });
			} else {
				InnerClass = new ClassDefinition("noname");
			}
			InnerClass.AttachBehavior(new BehaviorDispatcher(cls, cls != null ? cls.Behavior : null, this));
			InnerBehavior = InnerClass.Behavior;
			return InnerClass;
		}
		//................................................................
		#endregion


		#region Protected Methods
		//................................................................
		protected abstract IDataContainer InitializeDataContainer();

		protected virtual SlotReaderDelegate GetReader(string pname) {
			SlotReaderDelegate d = null;
			if (pname != null) {
				string mname = string.Format("get_{0}", pname);
				MethodDefinition md = InnerBehavior.GetMethod(mname);
				if (md != null)
					d = delegate(object sender, string propName) {
						return InnerBehavior.Invoke(mname);
					};
			}

			return d;
		}

		protected virtual SlotWriterDelegate GetWriter(string pname) {
			SlotWriterDelegate d = null;
			if (pname != null) {
				string mname = string.Format("set_{0}", pname);
				MethodDefinition md = InnerBehavior.GetMethod(mname);
				if (md != null)
					d = delegate(object sender, string propName, object value) {
						return InnerBehavior.Invoke(mname, value);
					};
			}

			return d;
		}

		protected virtual void OnSlotError(string pname, object original, object value, Exception ex) {
			EventHandler<SlotErrorEventArgs> h = AfterSlotError;
			if (h != null) {
				SlotDefinition sd = GetSlot(pname);
				SlotErrorEventArgs args = (sd != null)
						? new SlotErrorEventArgs(ex, sd, value)
						: new SlotErrorEventArgs(ex, pname, value);
				args.OriginalValue = original;
				h(this, args);
			}
		}

		// TODO: возможно параметризация поменяется!
		protected virtual bool OnBeforeSetSlotValue(string pname, object original, object value) {
			EventHandler<SlotChangeEventArgs> h = BeforeSetSlotValue;
			if (h != null) {
				SlotDefinition sd = GetSlot(pname);
				SlotChangeEventArgs args = (sd != null) 
						? new SlotChangeEventArgs(sd, value)
						: new SlotChangeEventArgs(pname, value);
				args.OriginalValue = original;
				h(this, args);
				return !args.Cancel;
			}
			return true;
		}

		protected virtual void OnAfterSetSlotValue(string pname, object original, object value) {
			EventHandler<SlotChangeEventArgs> h = AfterSetSlotValue;
			if (h != null) {
				SlotDefinition sd = GetSlot(pname);
				SlotChangeEventArgs args = (sd != null)
						? new SlotChangeEventArgs(sd, value)
						: new SlotChangeEventArgs(pname, value);
				args.OriginalValue = original;
				h(this, args);
			}
		}

		protected virtual void InitSlot(SlotDefinition slot) {
			// TODO: пересмотреть... как-то странно
			if (slot == null)
				Error.Warning(new ArgumentNullException("slot"), typeof(LObject));
			else {
				if (RawGetSlotValue(slot.Name) == null) {
					object v = slot.Evaluate();
					if (v != null)
						RawSetSlotValue(slot.Name, v);
				}
			}
		}

		protected virtual void OnChangeType() {
			// TODO: дописать! продумать параметризацию, и вызывать!

		}
		//................................................................
		#endregion
	}
	
	// TODO: в аксесоры можно добавить еще параметр с DefaultValue

	public delegate object SlotReaderDelegate(object sender, string pname);

	public delegate object SlotWriterDelegate(object sender, string pname, object value);

}
