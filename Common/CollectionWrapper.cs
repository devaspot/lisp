using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Data;
using System.Runtime.Serialization;

namespace Front.Collections {

	/// <summary>Простая обертка вокруг коллекции. Является базой для наследования при создании 
	/// оберток вокруг коллекций.</summary>
	[Serializable]
	public class CollectionWrapper<T> : Wrapper<ICollection<T>>, ICollection<T> {
	
		protected CollectionWrapper() {}

		//public CollectionWrapper(ICollection obj) : base(obj) {}

		public CollectionWrapper(ICollection<T> obj) : base(obj) { }

		// ICollection<T>
		// ...........................................................................
		public int Count { get { return Wrapped.Count; } }	
		public virtual bool IsReadOnly { get { return Wrapped.IsReadOnly; } }
		public virtual void Add(T item) { Wrapped.Add((T)item);}
		public virtual void Clear() { Wrapped.Clear(); }
		public virtual bool Contains(T item) { return Wrapped.Contains((T)item); }
		public virtual void CopyTo(T[] array, int arrayIndex) { Wrapped.CopyTo(array as T[], arrayIndex); }
		public virtual bool Remove(T item) {return Wrapped.Remove((T)item); }

		// IEnumerator<T> 
		// ...........................................................................
		// XXX не правильно!
		public virtual IEnumerator<T> GetEnumerator() {	return Wrapped.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
	}

	
	[Serializable]
	public class NamedValueCollectionWrapper<T> : CollectionWrapper<T>, IWrapper< INamedValueCollection<T> >, INamedValueCollection<T>, 
			IDeserializationCallback 
			where T : INamed {

		protected bool InnerReadOnly = false;

		protected NamedValueCollectionWrapper() {}
		
		public NamedValueCollectionWrapper( IEnumerable coll) 
			: this(new NamedValueCollection<T>(coll), false) {}

		public NamedValueCollectionWrapper( IEnumerable coll, bool readOnly) 
			: this(new NamedValueCollection<T>(coll), readOnly) { }

		public NamedValueCollectionWrapper( INamedValueCollection<T> coll) 
			: this(coll, false) { }

		public NamedValueCollectionWrapper( INamedValueCollection<T> coll, bool readOnly ) 
			: base(coll) {
			InnerReadOnly = readOnly;
			Attach((INamedValueCollection<T>)Wrapped);
		}

		protected virtual void Attach(INamedValueCollection<T> obj) {
			if (obj == null) return;
			obj.BeforeChange += delegate(object sender, CollectionChangeEventArgs args) {
					EventHandler<CollectionChangeEventArgs> eh = BeforeChange;
					if (eh != null) 
						eh(this, args);
				};

			obj.AfterChanged += delegate(object sender, CollectionChangedEventArgs args) {
					EventHandler<CollectionChangedEventArgs> eh = AfterChanged;
					if (eh != null)	
						eh(this, args);
				};
		}

		// IWrapper< INameValueCollection<T> >
		// ..........................................................................
		INamedValueCollection<T> IWrapper< INamedValueCollection<T> >.Wrapped {
			get { return NVC; }
		}

		internal INamedValueCollection<T> NVC {
			get { return (INamedValueCollection<T>)Wrapped; }
		}
			
		
		// INameValueCollection<T>
		// ..........................................................................
		[field:NonSerialized]
		public event EventHandler<CollectionChangeEventArgs> BeforeChange;
		[field: NonSerialized]
		public event EventHandler<CollectionChangedEventArgs> AfterChanged;

		public virtual object SyncRoot {
			get { return NVC.SyncRoot; }
		}

		public virtual bool IsSynchronized {
			get { return NVC.IsSynchronized; }
		}
		
		public virtual T this[ int index ] {
			get { return NVC[ index ]; }
			set {
				if (IsReadOnly) throw new ReadOnlyException("Tyr to modify ReadOnly Collection!");
				NVC[ index ] = value;
			}
		}
		
		public T this[string name] {
			get { return InternalGet(new Name(name), 0); }
			set { InternalSet(new Name(name), 0, value); }
		}

		public T this[string name, int index] {
			get { return InternalGet(new Name(name), index); }
			set { InternalSet(new Name(name), index, value); }
		}

		public T this[Name name] {
			get { return InternalGet(name, 0); }
			set { InternalSet(name, 0, value); }
		}

		public T this[Name name, int index] {
			get { return InternalGet(name, 0); }
			set { InternalSet(name, 0, value); }
		}

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
			this.Add((T)value);
		}

		public override void Add(T value) {
			if (IsReadOnly) throw new ReadOnlyException("Tyr to modify ReadOnly Collection!");
			NVC.Add(value);
		}

		bool IDictionary.Contains(object key) {
			if (key == null) return false;

			if (key as Name != null)
				return ContainsName((Name)key);
			if (key as string != null)
				return ContainsName((string)key);
			return false;
		}

		public virtual bool ContainsName(Name name) {
			return IndexOf(name) >= 0;
		}

		public virtual bool ContainsName(string name) {
			return IndexOf(name) >= 0;
		}

		void ICollection.CopyTo(Array array, int arrayIndex) {
			((ICollection)NVC).CopyTo(array, arrayIndex);
		}

		void IDictionary.Remove(object key) {
			if (IsReadOnly) throw new ReadOnlyException("Tyr to modify ReadOnly Collection!");
			((IDictionary)NVC).Remove(key);
		}

		ICollection IDictionary.Keys { 
			get { return NVC.Keys; }
		}
		
		public virtual ICollection<string> Names { 
			get { return NVC.Names; }
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
		
		new public virtual bool IsReadOnly {
			get { return InnerReadOnly || NVC.IsReadOnly; }
			set {
				if (NVC.IsReadOnly && !value)
					throw new ReadOnlyException("Tyr to modify ReadOnly Collection!");
				InnerReadOnly = value; 
			}
		}
		
		bool ICollection<T>.IsReadOnly {
			get { return this.IsReadOnly; }
		}


		bool IDictionary.IsReadOnly {
			get { return this.IsReadOnly; }
		}

		public virtual bool IsFixedSize {
			get { return NVC.IsFixedSize; }
		}

		public virtual int IndexOf(Name name) {
			return IndexOf(name, -1);
		}
		
		public virtual int IndexOf(Name name, int index) {
			try
			{
				return NVC.IndexOf(name, index);
			}
			catch (Exception ex) {
				throw ex;
			}
		}

		public virtual int IndexOf(string name) {
			return IndexOf(name, -1);
		}

		public virtual int IndexOf(string name, int index) {
			return IndexOf(new Name(name), index);
		}

		public virtual void AddRange(IEnumerable coll) {
			if (IsReadOnly) throw new ReadOnlyException("Tyr to modify ReadOnly Collection!");
			NVC.AddRange( coll );
		}

		public virtual void Insert(int idx, T value) {
			NVC.Insert(idx, value);
		}

		protected virtual T InternalGet(Name name, int index) {
			return NVC[name, index];
		}

		protected virtual void InternalSet(Name name, int index, T value) {
			if (IsReadOnly) throw new ReadOnlyException("Tyr to modify ReadOnly Collection!");
			NVC[name, index] = value;
		}

		public virtual INamedValueCollection<T> FilterNamespace(Name nspace) {
			INamedValueCollection<T> res = new NamedValueCollection<T>();
			foreach (T n in this)
				if (nspace.Equals(n.FullName.StartsWith(nspace)))
					res.Add(n);
			return res;
			// этот метож тоже годится. но тогда будет сильно много врапперов
			// return new NamedValueCollectionWrapper<T>( NVC.FilterNamespace(nspace) );
		}

		public virtual INamedValueCollection<T> FilterNamespace(string nspace) {
			return FilterNamespace(new Name(nspace));
		}

		IEnumerator INamedValueCollection<T>.GetEnumerator() {
			// TODO 59: нужно возвращать специальный Enumerator
			return new SimpleEnumerator<T>(this);
		}

		#region IDeserializationCallback Members
		// ..........................................................................
		public virtual void OnDeserialization(object sender) {
			Attach((INamedValueCollection<T>)Wrapped);
		}

		#endregion
	}

	/// <summary>Обертка вокруг коллекции именованных значений, проводящая адаптацию типа 
	/// элементов коллекции.</summary>
	public class NamedValueCollectionAdapter<T,P> : NamedValueCollectionWrapper<P>, INamedValueCollection<T> 
			where P : T
			where T : INamed {

		protected NamedValueCollectionAdapter() : base() {}

		public NamedValueCollectionAdapter( INamedValueCollection<P> coll ) : base (coll) {}
		public NamedValueCollectionAdapter( INamedValueCollection<P> coll, bool readOnly) : base(coll, readOnly) {}
		public NamedValueCollectionAdapter( IEnumerable coll ) : base( coll ) {}
		public NamedValueCollectionAdapter( IEnumerable coll, bool readOnly ) : base( coll, readOnly ) {
		}

		#region INamedValueCollection Implementation
		// ..........................................................................
		T INamedValueCollection<T>.this[string name] {
			get { return (T)this[name]; }
			// колекция деградирует при чтении, но записывать нужно только тип потомка!
			set { this[name] = (P)value; }
		}

		T INamedValueCollection<T>.this[Name name] {
			get { return (T)this[name]; }
			set { this[name] = (P)value; }
		}

		T INamedValueCollection<T>.this[string name, int index] {
			get { return (T)this[name, index]; }
			set { this[name, index] = (P)value; }
		}

		T INamedValueCollection<T>.this[Name name, int index] {
			get { return (T)this[name, index]; }
			set { this[name, index] = (P)value; }
		}

		T INamedValueCollection<T>.this[int index] {
			get { return (T)this[index]; }
			set { this[index] = (P)value; }
		}

//		int INamedValueCollection<T>.IndexOf(string name) { }

//		int INamedValueCollection<T>.IndexOf(string name, int index) { }

//		int INamedValueCollection<T>.IndexOf(Name name) { }

//		int INamedValueCollection<T>.IndexOf(Name name, int index) { }

//		ICollection<string> INamedValueCollection<T>.Names { get { } }

//		bool INamedValueCollection<T>.ContainsName(string name) { }

//		bool INamedValueCollection<T>.ContainsName(Name name) { }

//		void INamedValueCollection<T>.AddRange(IEnumerable coll) {}

//		bool INamedValueCollection<T>.IsReadOnly { get {} set {} }

		INamedValueCollection<T> INamedValueCollection<T>.FilterNamespace(Name nspace) {
			return new NamedValueCollectionAdapter<T,P>( FilterNamespace(nspace) );
		}

		INamedValueCollection<T> INamedValueCollection<T>.FilterNamespace(string nspace) {
			return new NamedValueCollectionAdapter<T, P>(FilterNamespace(nspace));
		}

		IEnumerator INamedValueCollection<T>.GetEnumerator() {
			return GetEnumerator();
		}

//		int INamedValueCollection<T>.Count { get { } }

//		void INamedValueCollection<T>.Clear() { }

		void INamedValueCollection<T>.Insert(int index, T value) {
			Insert(index, (P)value);
		}

//		event EventHandler<CollectionChangeEventArgs> INamedValueCollection<T>.BeforeChange
//		event EventHandler<CollectionChangedEventArgs> INamedValueCollection<T>.AfterChanged

		#endregion

		#region ICollection<T> Implementation
		//..........................................................................
		void ICollection<T>.Add(T item) {
			Add( (P)item );
		}

//		void ICollection<T>.Clear() { }

		bool ICollection<T>.Contains(T item) {
			if (item is P) 
				return Contains( (P)item );
			else
				return false;
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex) {
			if (array == null) throw new ArgumentNullException( "array" );
			if (arrayIndex >= array.Length) throw new ArgumentException( "arrayIndex" );
			if (arrayIndex < 0) throw new ArgumentOutOfRangeException( "arrayIndex" );

			int maxLength = Count;
			if (array.Length <= maxLength + arrayIndex)
				maxLength = array.Length - arrayIndex;

			for (int i = 0; i < maxLength; i++)
				array[i + arrayIndex] = this[i];
		}

//		int ICollection<T>.Count { get {} }

//		bool ICollection<T>.IsReadOnly { get {} }

		bool ICollection<T>.Remove(T item) {
			return Remove((P)item);
		}

		#endregion

		#region IEnumerable<T> Members
		//..........................................................................
		IEnumerator<T> IEnumerable<T>.GetEnumerator() {
			throw new NotImplementedException();
		}

		#endregion

	}

}