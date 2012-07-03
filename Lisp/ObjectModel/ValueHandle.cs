using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Front.ObjectModel {

	public interface IRefferenceHanlde {
		IObject Target { get; }
	}


	public interface ICollectionEntry {
		event EventHandler<CollectionEventArgs> AfterUnbind;
		event EventHandler<CollectionEventArgs> BeforeUpdate;
		event EventHandler<CollectionEventArgs> AfterUpdate;

		IList Owner { get; }
		int Index { get; }
		object Key { get; }

		bool IsBound { get; }
		bool IsReadOnly { get; }
	}


	[Serializable]
	public class ValueHandle : ISerializable, IXmlSerializable {
		[NonSerialized]
		[XmlIgnore]
		protected object InnerValue;
		public bool IsValid = true;

		public ValueHandle() {
		}

		protected ValueHandle(SerializationInfo info, StreamingContext context) {
		}

		public ValueHandle(object value) {
			InnerValue = value;
		}

		public virtual object Value {
			get { return GetValue(); }
			set { SetValue(value); }
		}

		#region Protected Methods
		//...............................................................
		protected virtual object GetValue() {
			return InnerValue;
		}

		protected virtual object SetValue(object value) {
			InnerValue = value;
			return value;
		}
		//...............................................................
		#endregion


		#region ISerializable Members
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
		}
		#endregion

		#region IXmlSerializable Members

		public System.Xml.Schema.XmlSchema GetSchema() {
			return null;
			//throw new Exception("The method or operation is not implemented.");
		}

		public void ReadXml(System.Xml.XmlReader reader) {
			IsValid = false;
			//throw new Exception("The method or operation is not implemented.");
		}

		public void WriteXml(System.Xml.XmlWriter writer) {
			//throw new Exception("The method or operation is not implemented.");
		}

		#endregion
	}

	[Serializable]
	public class RefferenceHandle : ValueHandle, IRefferenceHanlde {
		[XmlIgnore]
		[NonSerialized]
		protected IObject InnerTarget;
		protected string InnerTargetClassName;

		protected RefferenceHandle() { }

		public RefferenceHandle(string clsName, IDataContainer tgt) : base(tgt) {
			InnerTargetClassName = clsName;
		}

		public RefferenceHandle(IObject tgt) : base(tgt) {
			InnerTarget = tgt;
			if (tgt != null) {
				ClassDefinition cd = tgt.Definition;
				if (cd != null)
					InnerTargetClassName = cd.FullName;
			}
		}

		protected RefferenceHandle(SerializationInfo info, StreamingContext context) : base(info, context) {
			InnerTargetClassName = info.GetString("InnerTargetClassName");
		}


		public virtual string TargetClassName {
			get { return InnerTargetClassName; }
		}

		public virtual IObject Target {
			get {
				if (InnerTarget == null) 
					InnerTarget = UnWrap(InnerValue);
				return InnerTarget;
			}
		}

		public override object Value {
			get { return Target; }
			set {
				throw new NotImplementedException();
			}
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context) {
			base.GetObjectData(info, context);
			info.AddValue("InnerTargetClassName", InnerTargetClassName);
		}

		/// <summary>Этот метод вызывается, когда нужно воссоздать IObjcet по имеющемуся DataContainer'у (для других RefferenceHandle Это может быть дозагрузка по Id и т.п.)</summary>
		protected virtual IObject UnWrap(object value) {
			throw new NotImplementedException();
		}
	}


	/// <summary>Специальный "Placeholder", который остается актуальным, даже при сдвиге индексов.</summary>
	public class CollectionEntry : ValueHandle, ICollectionEntry {

		public CollectionEntry(object value) : base(value) {
		}

		public event EventHandler<CollectionEventArgs> AfterUnbind;
		public event EventHandler<CollectionEventArgs> BeforeUpdate;
		public event EventHandler<CollectionEventArgs> AfterUpdate;


		#region ICollectionEntry
		//..................................................................
		public virtual IList Owner {
			get {
				throw new NotImplementedException();
			}
		}

		public virtual int Index {
			get {
				throw new NotImplementedException();
			}
		}

		public virtual object Key {
			get {
				throw new NotImplementedException();
			}
		}

		public virtual bool IsBound {
			get {
				throw new NotImplementedException();
			}
		}

		public virtual bool IsReadOnly {
			get {
				if (IsBound) {
					IList l = Owner;
					if (l != null)
						return l.IsReadOnly;
				}
				return true; // несвязанный хендл или readonly коллекция
			}
		}
		//..................................................................
		#endregion


		#region Protected Methods
		//...............................................................
		protected override object GetValue() {
			Object res = base.GetValue();
			if (res == null)
				throw new NotImplementedException();
			return res;
		}

		//protected override object SetValue(object value) {
		//	throw new NotImplementedException();
		//}
		//...............................................................
		#endregion
	}


	#region Некоторые специальные ValueHandl-ы (Empty, Null, Uninitialized, Default..)
	//...........................................................................
	public class Empty : ValueHandle {
		new public static readonly Empty Value = new Empty();

		protected Empty() {
		}
	}

	//...........................................................................
	#endregion

}
