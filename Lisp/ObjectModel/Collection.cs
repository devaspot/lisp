using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Runtime.Serialization;

namespace Front.ObjectModel {

	public interface ICollectionObject : IObject, IList {
		string ItemClass { get; }
	}

	// TODO: кастинг догжен производиться с учетом интерфейса IExtendable!
	// потому, что из DataCollectionContaier будут вылазить только IObject (контейнер может и не
	// знать, в каком виде мы хотим результат!)

	// TODO: Кастинг с использованием IExtendable может быть источником доп. тормозов (если кастить зазря!)

	/// <summary>Объект данных - коллекуия объектов</summary>
	
	[Serializable]
	public class CollectionLObject<T> : LObjectBase, ISerializable, ICollectionObject, IList<T>, ICollectionContainer {

		protected bool InnerIsReadOnly = false;
		protected ClassDefinition InnerItemClass;

		#region Constructors
		//.............................................................
		public CollectionLObject() : base() { }

		public CollectionLObject(ClassDefinition definition) : base(definition) { }

		public CollectionLObject(ICollectionContainer container) : this(null, container) { }

		public CollectionLObject(ClassDefinition definition, ICollectionContainer container) : base(definition, container) {
		}

		protected CollectionLObject(SerializationInfo info, StreamingContext context) : base(info, context) {
			string className = info.GetString("InnerItemClass");
			// TODO: получить CustomExtentions, только их нужно будет тоже передавать в Update, что бы правильно нацепились бихейверы!
			InnerIsReadOnly = info.GetBoolean("InnerIsReadOnly");
		}

		public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
			base.GetObjectData(info, context);
			// TODO: написать!
			//InnerClass ередаем только ClassName
			if (InnerItemClass == null)
				info.AddValue("InnerItemClass", "");
			else
				info.AddValue("InnerItemClass", InnerItemClass.Name);

			// TODO: класс объеквта - это "обертка" над классом мета-информации
			//    1. сейчас он имеет калечное название типа "class1-instance", и нет нормальной возможности получить чистое имя класса
			//    2. класс инстанса может иметь специфические расширения, которых нет в Metailnfo-классе, и их список нужно дополнительно
			//       сериализовать!
			// info.AddValue("CustomExtentions", GetCustomExtentions(InnerClass));

			info.AddValue("InnerIsReadOnly", InnerIsReadOnly);
		}
		//.............................................................
		#endregion


		#region ICollectionObject Members
		//.............................................................
		public virtual string ItemClass {
			get { return GetItemClass(); }
		}
		//.............................................................
		#endregion


		#region ICollectionContainer Members
		//.............................................................
		public virtual object RawGetValue(int index) {
			return InnerList.RawGetValue(index);
		}

		public virtual object RawSetValue(int index, object value) {
			return InnerList.RawSetValue(index, value);
		}

		public virtual CollectionEntry GetEntry(int index) {
			return InnerList.GetEntry(index);
		}

		public virtual CollectionEntry GetEntry(object key) {
			return InnerList.GetEntry(key);
		}

		public virtual CollectionEntry NewEntry(params object[] args) {
			return InnerList.NewEntry(args);
		}
		//.............................................................
		#endregion


		#region IList Members
		//.............................................................
		int IList.Add(object value) {
			return InnerList.Add( Wrap(value, false) );
		}

		bool IList.Contains(object value) {
			// Если тип не подходит - просто возвращаем false
			object v = Wrap(value, true);
			if (v != null)
				return Contains((T)v);
			return false;
		}

		int IList.IndexOf(object value) {
			return IndexOf(Wrap(value, false));
		}

		void IList.Insert(int index, object value) {
			Insert(index, Wrap(value, false));
		}

		void IList.Remove(object value) {
			object v = Wrap(value, true);
			if (v != null)
				Remove((T)v);
		}

		object IList.this[int index] {
			get { return this[index]; }
			set { RawSetValue(index, Wrap(value, false)); }
		}

		public virtual void Clear() {			
			InnerList.Clear(); // XXX ай-яй-яй!
		}

		public virtual bool IsFixedSize {
			get {
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public virtual bool IsReadOnly {
			get { return InnerIsReadOnly; }
		}

		public virtual void RemoveAt(int index) {
			InnerList.RemoveAt(index);
		}
		//.............................................................
		#endregion


		#region ICollection Members
		//.............................................................
		public virtual void CopyTo(Array array, int index) {
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual int Count {
			get { return InnerList.Count; }
		}

		public virtual bool IsSynchronized {
			get { return InnerList.IsSynchronized; }
		}

		public virtual object SyncRoot {
			get { return InnerList.SyncRoot; }
		}
		//.............................................................
		#endregion


		#region IList<T> Members
		//.............................................................
		public virtual int IndexOf(T item) {
			return InnerList.IndexOf(item);
		}

		public virtual void Insert(int index, T item) {
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual T this[int index] {
			get { return Wrap( RawGetValue(index), true ); }
			set { RawSetValue(index, UnWrap(value)); }
		}
		//.............................................................
		#endregion


		#region ICollection<T> Members
		//.............................................................
		public virtual void Add(T item) {
			InnerList.Add(item);
			//throw new Exception("The method or operation is not implemented.");
		}

		public virtual bool Contains(T item) {
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual void CopyTo(T[] array, int arrayIndex) {
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual bool Remove(T item) {
			InnerList.Remove(item);
			return true; // XXX странный ход!
			//throw new Exception("The method or operation is not implemented.");
		}
		//.............................................................
		#endregion


		#region IEnumerable, IEnumerable<T> Members
		//.............................................................
		public virtual IEnumerator<T> GetEnumerator() {
			// XXX не очень хорошее решение.
			// то 2 CollectionContainerEnumerator<T> один вокруг другого
			// ...смех, что базовые классы не могут договориться друг с другом
			// (хотя базовый может быть чужим, и тогда у него свой енумератор... и можно было бы оборачивать)
			return new CollectionContainerEnumerator<T>(this, InnerList.GetEnumerator());
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return InnerList.GetEnumerator();
		}
		//.............................................................
		#endregion


		#region Protected Methods
		//.............................................................
		protected virtual ICollectionContainer InnerList {
			get { return InnerDataContainer as ICollectionContainer; }
		}

		protected override IDataContainer InitializeDataContainer() {
			InnerDataContainer = new CollectionContainer();
			return InnerDataContainer;
		}

		protected virtual string GetItemClass() {
			throw new NotImplementedException();
		}

		/// <summary>Адаптирует объект к нужному типу (с учетом возможного IExtendable!)</summary>
		protected virtual T Wrap(object value, bool quite) {

			// TODO: нужно переписать Wrap так, что бы IObjectWrapper превражашся в IObject
			// иначе при добавлении в коллекцию Behavior'ов (которые IObject) буддут
			// храниться они, вместо их Nut'ов
			// (хотя может опустить эту задачу на откуп DataContainer'а и RefferenceHandle)

			if (value == null) return default(T);
			if (value is T) return (T)value;

			IExtendable ex = value as IExtendable;
			if (ex != null) {
				object o = ex.As<T>();
				if (o != null)
					return (T)o;
			}
			if (quite)
				return default(T);
			throw new InvalidCastException();
		}

		protected virtual object UnWrap(T value) {
			return value;
		}
		//.............................................................
		#endregion

	}


	public class CollectionLObject : CollectionLObject<object> {

		#region Constructors
		//.............................................................
		public CollectionLObject() : base() {
		}

		public CollectionLObject(ClassDefinition definition) : base(definition) {
		}

		public CollectionLObject(ICollectionContainer container) : this(null, container) {
		}

		public CollectionLObject(ClassDefinition definition, ICollectionContainer container)
			: base(definition, container) {
		}
		//.............................................................
		#endregion

	}

	// TODO: нужен специальный Enumerator

}
