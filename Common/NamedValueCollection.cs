// $Id$

using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Front.Collections {
	
	/// <summary>Ўаблон интерфейса дл€ коллекции именованых значений.</summary>
	/// <typeparam name="T">“ип значени€, который должен поддерживать интерфейс <see cref="INamed"/>.</typeparam>
	public interface INamedValueCollection<T> : IEnumerable, ICollection<T>, ICollection, IDictionary where T : INamed {
		
		/// <summary>ѕолучить значение по строковому представлению имени.</summary>
		/// <param name="name">им€</param>
		T this[string name] { get; set; }

		/// <summary>ѕолучить значение имени.</summary>
		/// <param name="name">им€</param>
		T this[Name name] { get; set; }
		
		/// <summary>ѕолучить значение по строковому представлению имени.</summary>
		/// <param name="name">им€</param>
		/// <param name="index">пор€дковый номер значени€ среди значений с таким же именем.</param>
		/// <returns>«начение с указаным именем, пор€док которого равен <c>index</c>.</returns>
		/// <example>
		/// INamedValueCollection < NamedValue >; NVC = new NameValueCollection< NamedValue >();
		/// NVC.Add( new NamedValue("MyName", 1));
		/// NVC.Add( new NamedValue("HisName", 2));
		/// NVC.Add( new NamedValue("MyName", 3));
		/// NamedValue x = NVC["MyName"];
		/// System.Console.WriteLine(x.Value);  // напишет 1
		/// x = NVC["MyName",1];
		/// System.Console.WriteLine(x.Value); // напишет 3
		/// </example>
		T this[string name, int index] { get; set; }
	
		/// <summary>ѕолучить значение по имени.</summary>
		/// <param name="name">им€</param>
		/// <param name="index">пор€дковый номер значени€ среди значений с таким же именем.</param>
		/// <returns>«начение с указаным именем, пор€док которого равен <c>index</c>.</returns>
		T this[Name name, int index] { get; set; }

		/// <summary>ѕолучить/”становить значение по индексу</summary>
		T this[int index] { get; set; }

		/// <summary>»ндекс объекта по его имени.</summary>
		int IndexOf(string name);
		int IndexOf(string name, int index);
		
		int IndexOf(Name name);
		int IndexOf(Name name, int index);

		/// <summary>коллекци€ имен.</summary>
		ICollection<string> Names { get; }

		bool ContainsName(string name);

		bool ContainsName(Name name);

		void AddRange(IEnumerable coll);

		new bool IsReadOnly { get; set; }
		
		// TODO DF-47: нужен какой-то метод типа FilterNamespace или просто Filter
		INamedValueCollection<T> FilterNamespace(Name nspace);
		INamedValueCollection<T> FilterNamespace(string nspace);

		// TODO DF-57: нужно разобратьс€ с Enumerat'орами
		new IEnumerator GetEnumerator();
		
		/// <summary>„исло элементов в коллекции. </summary>
		/// <remarks><see cref="INamedValueCollection"/> наследует два интерфейса коллекций, и
		/// потому нужно внести €сность относительно некоторых одинаковых методов.</remarks>
		new int Count { get;}

		new void Clear();
		void Insert(int index, T value);

		event EventHandler<CollectionChangeEventArgs> BeforeChange;
		event EventHandler<CollectionChangedEventArgs> AfterChanged;
	}

	public class CollectionChangedEventArgs : EventArgs {
		protected ModifyAction action;
		protected int index;
		protected INamed value;
		protected INamed old_value;
			
		public CollectionChangedEventArgs( ModifyAction action, int index, INamed value, INamed old_value ) {
			this.action = action;
			this.index = index;
			this.value = value;
			this.old_value = old_value;
		}

		public ModifyAction ModifyAction { get { return action; } }
		public virtual int Index { get { return index; } set {} }
		public string Name { get { return (value != null) ? value.Name : ""; } }
		public virtual INamed Value { get { return value; } set { } }
		public INamed OldValue { get { return old_value; } }
	}

	public class CollectionChangeEventArgs : CollectionChangedEventArgs {
		protected bool cancel = false;
		
		public CollectionChangeEventArgs( ModifyAction action, int index, INamed value, INamed old_value ) 
			: base (action, index, value, old_value) {
		}

		public override INamed Value { get { return value; } set { this.value = value; } }
		public override int Index { get { return index; } set { this.index = value; } }
		public bool Cancel { get { return this.cancel; } set { this.cancel = value; } }
	}

	
	/// <summary>—уть производимых изменений.</summary>
	public enum ModifyAction { Insert, Change, Remove, Clear } 

	
	/// <summary>ѕрототип класса коллекции именованых значений.</summary>
	// TODO DF-64: проверить!
	// внести €сность с точками входа, вызовами событий и методов типа OnUpdate, OnRemove, OnInsert...
	[Serializable]
	public class NamedValueCollection<T> 
		: CollectionBase, IList<T>, INamedValueCollection<T>, ICloneable where T : INamed {
		
		protected bool InnerReadOnly = false;

		public NamedValueCollection() { }

		public NamedValueCollection(IEnumerable coll) : this() {
			AddRange(coll);
		}


		[field: NonSerialized]
		public event EventHandler<CollectionChangeEventArgs> BeforeChange;

		[field: NonSerialized]
		public event EventHandler<CollectionChangedEventArgs> AfterChanged;


		#region IDictionary implementation
		//...............................................................
		object IDictionary.this[object key] {
			get { 
				if (key == null) return null;
				if (key as Name != null)
					return this[(Name)key];
				else if (key as string != null)
					return this[(string)key];
				throw new ArgumentException("Key Should be string or Name");
			}
			set { 
				if (key == null) return;
				if (key as Name != null)
					this[(Name)key] = (T)value;
				else if (key as string != null)
					this[(string)key] = (T)value;
				throw new ArgumentException("Key Should be string or Name");
			}
		}

		void IDictionary.Add(object key, object value) {
			if (IsReadOnly) throw new ReadOnlyException("Try to modify ReadOnly Collection!");
			this.Add((T)value);
		}


		bool IDictionary.Contains(object key) {
			if (key == null) return false;

			if (key as Name != null)
				return ContainsName((Name)key);
			if (key as string != null)
				return ContainsName((string)key);
			return false;
		}

		void IDictionary.Remove(object key) {
			if (IsReadOnly) throw new ReadOnlyException( "Try to modify ReadOnly Collection!" );
			if (key as string != null)
				RemoveAt( this.IndexOf( (string)key ) );
			else if (key as Name != null)
				RemoveAt( this.IndexOf( (Name)key ) );
			else
				throw new ArgumentException( "Key Should be string or Name" );
		}

		ICollection IDictionary.Keys {
			get {
				ArrayList a = new ArrayList();
				foreach (T obj in this)
					if (!a.Contains( obj.Name )) a.Add( obj.Name );
				return a;
			}
		}

		ICollection IDictionary.Values {
			get { return (ICollection)this; }
		}

		public virtual ICollection<T> Values {
			get { return this; }
		}

		IDictionaryEnumerator IDictionary.GetEnumerator() {
			throw new NotImplementedException();
		}
		#endregion

		#region ICollection implementation
		//...............................................................
		public virtual bool IsReadOnly {
			get { return InnerReadOnly; }
			set { InnerReadOnly = value; }
		}

		public virtual bool IsFixedSize {
			get { return false; }
		}

		void ICollection.CopyTo(Array array, int arrayIndex) {
			if (array == null) throw new ArgumentNullException( "array" );
			if (arrayIndex >= array.Length) throw new ArgumentException( "arrayIndex" );
			if (arrayIndex < 0) throw new ArgumentOutOfRangeException( "arrayIndex" );

			int maxLength = Count;
			if (array.Length <= maxLength + arrayIndex)
				maxLength = array.Length - arrayIndex;

			for (int i = 0; i < maxLength; i++)
				array.SetValue( this[i], i + arrayIndex );
		}

		public virtual void CopyTo(T[] array, int arrayIndex) {
			if (array == null) throw new ArgumentNullException( "array" );
			if (arrayIndex >= array.Length) throw new ArgumentException( "arrayIndex" );
			if (arrayIndex < 0) throw new ArgumentOutOfRangeException( "arrayIndex" );

			int maxLength = Count;
			if (array.Length <= maxLength + arrayIndex)
				maxLength = array.Length - arrayIndex;

			for (int i = 0; i < maxLength; i++)
				array[i + arrayIndex] = this[i];
		}
		
		public virtual ICollection<string> Names { 
			get {
				ICollection<string>a = new Collection<string>();
				foreach (T obj in this)
					if (!a.Contains(obj.Name)) a.Add(obj.Name);
				return a;
			}
		}

		public virtual bool Remove(T item) {
			int index = this.IndexOf( item );
			if (index < 0) return false;
			this.RemoveAt( index );
			return true;
		}
		#endregion

		#region IEnumerable implementation
		//...............................................................
		IEnumerator<T> IEnumerable<T>.GetEnumerator() {
			return this.InternalGetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return (IEnumerator<T>)this.InternalGetEnumerator();
		}

		protected virtual IEnumerator<T> InternalGetEnumerator() {
			return new SimpleEnumerator<T>(this);
		}
		#endregion

		#region INamedValueCollection<T> implementation
		//...............................................................
		public virtual T this[string name] {
			get { return InternalGet( (name == null)? null : new Name( name ), 0 ); }
			set { InternalSet( (name == null) ? null : new Name( name ), 0, value ); }
		}

		public virtual T this[string name, int index] {
			get { return InternalGet( (name == null)? null : new Name( name ), index ); }
			set { InternalSet( (name == null) ? null :new Name( name ), index, value ); }
		}

		public virtual T this[Name name] {
			get { return InternalGet( name, 0 ); }
			set { InternalSet( name, 0, value ); }
		}

		public virtual T this[Name name, int index] {
			get { return InternalGet( name, 0 ); }
			set { InternalSet( name, 0, value ); }
		}

		public virtual int IndexOf(Name name) {
			return IndexOf(name, -1);
		}
		
		public virtual int IndexOf(Name name, int index) {
			if (name == null) return -1; 
			int c = 0;
			foreach (T v in this) {
				if (name.Equals(v.Name))
					if (c < index)
						c++;
					else
						return this.IndexOf(v);
			}
			return -1;
		}

		public virtual int IndexOf(string name) {
			return IndexOf(name, -1);
		}

		public virtual int IndexOf(string name, int index) {
			return IndexOf((name == null) ? null : new Name(name), index);
		}

		public virtual int CountNames(Name name) {
			int c = 0;
			foreach (T v in this)
				if (name.Equals(v.Name)) c++;
			return c;
		}

		public virtual bool ContainsName(Name name) {
			return IndexOf( name ) >= 0;
		}

		public virtual bool ContainsName(string name) {
			return IndexOf( name ) >= 0;
		}

		public virtual INamedValueCollection<T> FilterNamespace(Name nspace) {
			if (nspace == null) nspace = new Name("");
			INamedValueCollection<T> res = new NamedValueCollection<T>();
			foreach (T n in this)
				if (nspace.Equals( n.FullName.StartsWith( nspace ) ))
					res.Add( n );
			return res;
			// можно создать FilteredCollection как CollectionWrapper, и возвращать ее...
		}

		IEnumerator INamedValueCollection<T>.GetEnumerator() {
			return this.GetEnumerator();
		}

		public virtual INamedValueCollection<T> FilterNamespace(string nspace) {
			return FilterNamespace( (nspace != null) ? new Name( nspace ) : null );
		}

		public virtual void AddRange(IEnumerable coll) {
			if (IsReadOnly) throw new ReadOnlyException("Tyr to modify ReadOnly Collection!");
			if (coll != null)
				foreach (T obj in coll) this.Add(obj);
		}

		protected virtual T InternalGet(Name name, int index) {
			int i = IndexOf( name, index );
			return (i >= 0) ? this[i] : default(T);
		}

		protected virtual void InternalSet(Name name, int index, T value) {
			if (IsReadOnly) throw new ReadOnlyException( "Tyr to modify ReadOnly Collection!" );
			int i = IndexOf( name, index );
			if (i < 0) 
				Add(value);
			else
				this[i] = value;
		}
		#endregion

		#region IList<T> Implementation 
		public virtual void Insert(int idx, T value) {
			OnSet(idx, null, value);
			OnValidate(value);
			InnerList.Insert(idx, value);
			OnSetComplete(idx, null, value);
		}
		#endregion

		// все сводитс€ к вызову этих методов:
		//----------------------------------------------------------------
		// ... и еще некоторым, унаследованным от CollectionBase :-/		
		public virtual T this[int index] {
			get { return (T)InnerList[index]; }
			set {
				if (IsReadOnly) throw new ReadOnlyException();
				object old_value = (InnerList.Count < index) ? InnerList[index] : null;
				OnSet(index, old_value, value);
				OnValidate(value);
				InnerList[index] = value;
				OnSetComplete(index, old_value, value);
			}
		}

		public virtual void Add(T item) {
			if (IsReadOnly) throw new ReadOnlyException();
			int index = InnerList.Count;
			OnInsert(index, item);
			OnValidate(item);
			InnerList.Insert(index, item);
			OnInsertComplete(index, item);
		}

		public virtual bool Contains(T obj) {
			return InnerList.Contains( obj );
		}

		public virtual int IndexOf(T obj) {
			return InnerList.IndexOf( obj );
		}

		// Cloning
		object ICloneable.Clone() { return this.Clone(); }

		public virtual NamedValueCollection<T> Clone() {
			using (GenericClone.StartClone) {
				NamedValueCollection<T> res = (NamedValueCollection<T>)this.MemberwiseClone();

				ArrayList list = new ArrayList();
				foreach (object o in InnerList)
					list.Add( GenericClone.Clone(o) );

				Type t = typeof(CollectionBase);
				System.Reflection.FieldInfo fi = t.GetField("list",
					System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				fi.SetValue(res, list);

				return res;
			}
		}
		

		//..........................................................................
		// TODO Ќе обрабатываетс€ Cancel и изменени€ Index и Value!

		protected override void OnSet(int index, Object oldValue, Object newValue) {
			base.OnSet(index, oldValue, newValue);
			EventHandler<CollectionChangeEventArgs> eh = BeforeChange;
			if (eh != null) {
				CollectionChangeEventArgs args = new CollectionChangeEventArgs(
					ModifyAction.Change, index, (INamed)newValue, (INamed)oldValue);
				eh(this, args);
			}
		}

		protected override void OnSetComplete(int index, Object oldValue, Object newValue) {
			base.OnSetComplete(index, oldValue, newValue);
			EventHandler<CollectionChangedEventArgs> eh = AfterChanged;
			if (eh != null) {
				CollectionChangedEventArgs args = new CollectionChangedEventArgs(
					ModifyAction.Change, index, (INamed)newValue, (INamed)oldValue);
				eh(this, args);
			}
		}

		protected override void OnInsert(int index, object value) {
			base.OnInsert(index, value);
			EventHandler<CollectionChangeEventArgs> eh = BeforeChange;
			if (eh != null) {
				CollectionChangeEventArgs args = new CollectionChangeEventArgs(
					ModifyAction.Insert, index, (INamed)value, null);
				eh(this, args);
			}
		}

		protected override void OnInsertComplete(int index, Object value) {
			base.OnInsertComplete(index, value);
			EventHandler<CollectionChangedEventArgs> eh = AfterChanged;
			if (eh != null) {
				CollectionChangedEventArgs args = new CollectionChangedEventArgs(
					ModifyAction.Insert, index, (INamed)value, null);
				eh(this, args);
			}
		}

		protected override void OnRemove(int index, object value) {
			base.OnRemove(index, value);
			EventHandler<CollectionChangeEventArgs> eh = BeforeChange;
			if (eh != null) {
				CollectionChangeEventArgs args = new CollectionChangeEventArgs(
					ModifyAction.Remove, index, null, (INamed)value);
				eh(this, args);
			}			
		}

		protected override void OnRemoveComplete(int index, Object value) {
			base.OnRemoveComplete(index, value);
			EventHandler<CollectionChangedEventArgs> eh = AfterChanged;
			if (eh != null) {
				CollectionChangedEventArgs args = new CollectionChangedEventArgs(
					ModifyAction.Remove, index, null, (INamed)value);
				eh(this, args);
			}
		}

		protected override void OnClear() {
			base.OnClear();
			EventHandler<CollectionChangeEventArgs> eh = BeforeChange;
			if (eh != null) {
				CollectionChangeEventArgs args = new CollectionChangeEventArgs(
					ModifyAction.Clear, -1, null, null);
				eh(this, args);
			}			
		}

		protected override void OnClearComplete() {
			base.OnClearComplete();
			EventHandler<CollectionChangedEventArgs> eh = AfterChanged;
			if (eh != null) {
				CollectionChangedEventArgs args = new CollectionChangedEventArgs(
					ModifyAction.Clear, -1, null, null);
				eh(this, args);
			}
		}

	}
	
	[Serializable]
	public class JointNamedValueCollection<T> : NamedValueCollection<T> where T : INamed {
		protected ICollection<T>[] InnerCollections;
		public JointNamedValueCollection(params ICollection<T>[] collectionList) {
			InnerCollections = collectionList;
		}
		protected override IEnumerator<T> InternalGetEnumerator() {
			return new MultiCollectionEnumerator<T>(InnerCollections);
		}

	}


	[Serializable]
	public class NamedValueCollection : NamedValueCollection<INamed>, INamedValueCollection<INamed> {
		public NamedValueCollection() : base() { }

		public NamedValueCollection(IEnumerable coll) : base( coll ) { }

		/// <summary> —оздает коллекци€ю именованых значений по произвольному списку значений.</summary>
		/// <remarks>—писок должен иметь структуру: [ name0, value0, name1, value1,....].
		/// ≈сли <c>nameX</c> не €вл€етс€ строкий, то дл€ него вызываетс€ <c>ToString()</c>. 
		/// ≈сли <c>namrX</c> пустое, то генерируетс€ им€ "item X".</remarks>
		public static NamedValueCollection<NamedValue> Create(params object[] items) {
			NamedValueCollection<NamedValue> res = new NamedValueCollection<NamedValue>();
			if (items != null && items.Length > 0) {
				for (int i = 0; i < items.Length; i += 2) {
					string name = items[i] as string;
					if (name == null)
						name = (items[i] != null) ? items[i].ToString() : ("item " + (i / 2).ToString());
					object value = (items.Length > i + 1) ? items[i + 1] : null;
					res.Add(new NamedValue(name, value));
				}
			}
			return res;
		}
	}


	public class SimpleEnumerator<T>: IEnumerator<T> where T : INamed{

		protected INamedValueCollection<T> InnerList;
		protected int position = -1;

		public SimpleEnumerator(INamedValueCollection<T> lst) {
			InnerList = lst;
		}

		object IEnumerator.Current {
			get { return this.Current; }
		}

		public virtual T Current {
			get {
				if (position < 0 || position >= InnerList.Count)
					throw new InvalidOperationException();
				return InnerList[position];
			}
		}

		public virtual bool MoveNext() {
			if (position < InnerList.Count) position++;
			return (position < InnerList.Count);
		}

		public virtual void Reset() {
			position = -1;
		}

		public virtual void Dispose() {
		}
	}
}
