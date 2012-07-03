using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

using Front.Collections.Generic;

namespace Front {

	// TODO Навести порядок тута!!!

	/// <summary>Generic Обертка вокруг коллекций</summary>
	/// <typeparam name="T">тип элементов коллекции</typeparam>
	public interface IListWrapper<T> : IList<T>, IWrapper {
		event EventHandler<AddItemEventArgs<T>> BeforeAdd;
		event EventHandler<AddItemEventArgs<T>> AfterAdd;
		event EventHandler<InsertItemEventArgs<T>> BeforeInsert;
		event EventHandler<InsertItemEventArgs<T>> AfterInsert;
		event EventHandler<RemoveItemEventArgs<T>> BeforeRemove;
		event EventHandler<RemoveItemEventArgs<T>> AfterRemove;
		event EventHandler<SetItemEventArgs<T>> BeforeSet;
		event EventHandler<SetItemEventArgs<T>> AfterSet;
		event EventHandler BeforeClear;
		event EventHandler AfterClear;
		event EventHandler<RemoveAtEventArgs> BeforeRemoveAt;
		event EventHandler<RemoveAtEventArgs> AfterRemoveAt;
	}


	/// <summary>Обертка вокруг коллекций</summary>
	public interface IListWrapper : IList, IWrapper {
		event EventHandler<AddItemEventArgs> BeforeAdd;
		event EventHandler<AddItemEventArgs> AfterAdd;
		event EventHandler<InsertItemEventArgs> BeforeInsert;
		event EventHandler<InsertItemEventArgs> AfterInsert;
		event EventHandler<RemoveItemEventArgs> BeforeRemove;
		event EventHandler<RemoveItemEventArgs> AfterRemove;
		event EventHandler<SetItemEventArgs> BeforeSet;
		event EventHandler<SetItemEventArgs> AfterSet;
		event EventHandler BeforeClear;
		event EventHandler AfterClear;
		event EventHandler<RemoveAtEventArgs> BeforeRemoveAt;
		event EventHandler<RemoveAtEventArgs> AfterRemoveAt;
	}


	/// <summary>Обертка вокруг IList-ов</summary>
	[Serializable]
	public class SimpleListWrapper : Wrapper<IList>, IListWrapper {

		#region Constructors
		//.........................................................................
		public SimpleListWrapper(IList list) : base(list) { }
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public object this[int index] {
			get { return InternalGet(index); }
			set { InternalSet(index, value); }
		}

		public virtual bool IsReadOnly {
			get { return Wrapped.IsReadOnly; }
		}

		public virtual bool IsFixedSize {
			get { return Wrapped.IsFixedSize; }
		}

		public virtual int Count {
			get { return Wrapped.Count; }
		}

		public virtual bool IsSynchronized {
			get { return Wrapped.IsSynchronized; }
		}

		public virtual object SyncRoot {
			get { return Wrapped.SyncRoot; }
		}
		//.........................................................................
		#endregion


		#region Events
		//.........................................................................
		public event EventHandler<AddItemEventArgs> BeforeAdd;
		public event EventHandler<AddItemEventArgs> AfterAdd;
		public event EventHandler<InsertItemEventArgs> BeforeInsert;
		public event EventHandler<InsertItemEventArgs> AfterInsert;
		public event EventHandler<RemoveItemEventArgs> BeforeRemove;
		public event EventHandler<RemoveItemEventArgs> AfterRemove;
		public event EventHandler<SetItemEventArgs> BeforeSet;
		public event EventHandler<SetItemEventArgs> AfterSet;
		public event EventHandler BeforeClear;
		public event EventHandler AfterClear;
		public event EventHandler<RemoveAtEventArgs> BeforeRemoveAt;
		public event EventHandler<RemoveAtEventArgs> AfterRemoveAt;
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual int Add(object value) {
			AddItemEventArgs args = new AddItemEventArgs(value);
			OnBeforeAdd(args);
			int index = Wrapped.Add(args.Item);
			OnAfterAdd(args);
			return index;
		}

		public virtual int IndexOf(object value) {
			return Wrapped.IndexOf(value);
		}

		public virtual void Insert(int index, object value) {
			InsertItemEventArgs args = new InsertItemEventArgs(value, index);
			OnBeforeInsert(args);
			Wrapped.Insert(args.Index, args.Item);
			OnAfterInsert(args);
		}

		public virtual void Remove(object value) {
			RemoveItemEventArgs args = new RemoveItemEventArgs(value);
			OnBeforeRemove(args);
			Wrapped.Remove(args.Item);
			OnAfterRemove(args);
		}

		public virtual void RemoveAt(int index) {
			object item = Wrapped[index];
			RemoveAtEventArgs args = new RemoveAtEventArgs(index);
			OnBeforeRemoveAt(args);
			OnBeforeRemove(new RemoveItemEventArgs(item));
			Wrapped.RemoveAt(args.Index);
			OnAfterRemoveAt(args);
			OnAfterRemove(new RemoveItemEventArgs(item));
		}

		public virtual bool Contains(object value) {
			return Wrapped.Contains(value);
		}

		public virtual void CopyTo(Array array, int arrayIndex) {
			Wrapped.CopyTo(array, arrayIndex);
		}

		public virtual IEnumerator GetEnumerator() {
			return Wrapped.GetEnumerator();
		}

		public virtual void AddRange(IEnumerable collection) {
			if (collection != null) {
				IEnumerator e = collection.GetEnumerator();
				e.Reset();
				while (e.MoveNext())
					Add(e.Current);
			}
		}

		public virtual void Clear() {
			OnBeforeClear();
			Wrapped.Clear();
			OnAfterClear();
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual void OnBeforeClear() {
			EventHandler handler = BeforeClear;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected virtual void OnAfterClear() {
			EventHandler handler = AfterClear;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected virtual void OnBeforeRemoveAt(RemoveAtEventArgs args) {
			EventHandler<RemoveAtEventArgs> handler = BeforeRemoveAt;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterRemoveAt(RemoveAtEventArgs args) {
			EventHandler<RemoveAtEventArgs> handler = AfterRemoveAt;
			if (handler != null)
				handler(this, args);
		}

		protected virtual object InternalGet(int index) {
			return Wrapped[index];
		}

		protected virtual void InternalSet(int index, object value) {
			SetItemEventArgs args = new SetItemEventArgs(value, index);
			OnBeforeSet(args);
			Wrapped[args.Index] = args.Item;
			OnAfterSet(args);
		}

		protected virtual void OnBeforeAdd(AddItemEventArgs args) {
			EventHandler<AddItemEventArgs> handler = BeforeAdd;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterAdd(AddItemEventArgs args) {
			EventHandler<AddItemEventArgs> handler = AfterAdd;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnBeforeInsert(InsertItemEventArgs args) {
			EventHandler<InsertItemEventArgs> handler = BeforeInsert;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterInsert(InsertItemEventArgs args) {
			EventHandler<InsertItemEventArgs> handler = AfterInsert;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnBeforeRemove(RemoveItemEventArgs args) {
			EventHandler<RemoveItemEventArgs> handler = BeforeRemove;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterRemove(RemoveItemEventArgs args) {
			EventHandler<RemoveItemEventArgs> handler = AfterRemove;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnBeforeSet(SetItemEventArgs args) {
			EventHandler<SetItemEventArgs> handler = BeforeSet;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterSet(SetItemEventArgs args) {
			EventHandler<SetItemEventArgs> handler = AfterSet;
			if (handler != null)
				handler(this, args);
		}
		//.........................................................................
		#endregion

	}

	/// <summary>Generic-обертка вокруг IList-ов</summary>
	/// <typeparam name="T">желаемый тип элементов коллекции</typeparam>
	[Serializable]
	public class SimpleListWrapper<T> : Wrapper<IList>, IListWrapper<T> {

		#region Constructors
		//.........................................................................
		public SimpleListWrapper(IList list) : base(list) { }
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public T this[int index] {
			get { return InternalGet(index); }
			set { InternalSet(index, value); }
		}

		public int Count {
			get { return Wrapped.Count; }
		}

		public bool IsReadOnly {
			get { return Wrapped.IsReadOnly; }
		}
		//.........................................................................
		#endregion


		#region Events
		//.........................................................................
		public event EventHandler<AddItemEventArgs<T>> BeforeAdd;
		public event EventHandler<AddItemEventArgs<T>> AfterAdd;
		public event EventHandler<InsertItemEventArgs<T>> BeforeInsert;
		public event EventHandler<InsertItemEventArgs<T>> AfterInsert;
		public event EventHandler<RemoveItemEventArgs<T>> BeforeRemove;
		public event EventHandler<RemoveItemEventArgs<T>> AfterRemove;
		public event EventHandler<SetItemEventArgs<T>> BeforeSet;
		public event EventHandler<SetItemEventArgs<T>> AfterSet;
		public event EventHandler BeforeClear;
		public event EventHandler AfterClear;
		public event EventHandler<RemoveAtEventArgs> BeforeRemoveAt;
		public event EventHandler<RemoveAtEventArgs> AfterRemoveAt;
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual void Add(T item) {
			AddItemEventArgs<T> args = new AddItemEventArgs<T>(item);
			OnBeforeAdd(args);
			Wrapped.Add(args.Item);
			OnAfterAdd(args);
		}

		public virtual int IndexOf(T item) {
			return Wrapped.IndexOf(item);
		}

		public virtual void Insert(int index, T item) {
			InsertItemEventArgs<T> args = new InsertItemEventArgs<T>(item, index);
			OnBeforeInsert(args);
			Wrapped.Insert(args.Index, args.Item);
			OnAfterInsert(args);
		}

		public virtual bool Remove(T item) {
			if (Contains(item)) {
				RemoveItemEventArgs<T> args = new RemoveItemEventArgs<T>(item);
				OnBeforeRemove(args);
				Wrapped.Remove(args.Item);
				OnAfterRemove(args);

				return true;
			} else
				return false;
		}

		public virtual void RemoveAt(int index) {
			T item = this[index];
			RemoveAtEventArgs args = new RemoveAtEventArgs(index);
			OnBeforeRemoveAt(args);
			OnBeforeRemove(new RemoveItemEventArgs<T>(item));
			Wrapped.RemoveAt(args.Index);
			OnAfterRemoveAt(args);
			OnAfterRemove(new RemoveItemEventArgs<T>(item));
		}

		public virtual bool Contains(T item) {
			return Wrapped.Contains(item);
		}

		public virtual void CopyTo(T[] array, int arrayIndex) {
			Wrapped.CopyTo(array, arrayIndex);
		}

		public virtual void Clear() {
			OnBeforeClear();
			Wrapped.Clear();
			OnAfterClear();
		}

		public virtual IEnumerator<T> GetEnumerator() {
			return new EnumeratorWrapper<T>(Wrapped.GetEnumerator());
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual void OnBeforeClear() {
			EventHandler handler = BeforeClear;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected virtual void OnAfterClear() {
			EventHandler handler = AfterClear;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected virtual void OnBeforeRemoveAt(RemoveAtEventArgs args) {
			EventHandler<RemoveAtEventArgs> handler = BeforeRemoveAt;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterRemoveAt(RemoveAtEventArgs args) {
			EventHandler<RemoveAtEventArgs> handler = AfterRemoveAt;
			if (handler != null)
				handler(this, args);
		}

		protected virtual T InternalGet(int index) {
			return (T)Wrapped[index];
		}

		protected virtual void InternalSet(int index, T value) {
			SetItemEventArgs<T> args = new SetItemEventArgs<T>(value, index);
			OnBeforeSet(args);
			Wrapped[args.Index] = args.Item;
			OnAfterSet(args);
		}

		protected virtual void OnBeforeAdd(AddItemEventArgs<T> args) {
			EventHandler<AddItemEventArgs<T>> handler = BeforeAdd;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterAdd(AddItemEventArgs<T> args) {
			EventHandler<AddItemEventArgs<T>> handler = AfterAdd;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnBeforeInsert(InsertItemEventArgs<T> args) {
			EventHandler<InsertItemEventArgs<T>> handler = BeforeInsert;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterInsert(InsertItemEventArgs<T> args) {
			EventHandler<InsertItemEventArgs<T>> handler = AfterInsert;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnBeforeRemove(RemoveItemEventArgs<T> args) {
			EventHandler<RemoveItemEventArgs<T>> handler = BeforeRemove;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterRemove(RemoveItemEventArgs<T> args) {
			EventHandler<RemoveItemEventArgs<T>> handler = AfterRemove;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnBeforeSet(SetItemEventArgs<T> args) {
			EventHandler<SetItemEventArgs<T>> handler = BeforeSet;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterSet(SetItemEventArgs<T> args) {
			EventHandler<SetItemEventArgs<T>> handler = AfterSet;
			if (handler != null)
				handler(this, args);
		}
		//.........................................................................
		#endregion

	}


	/// <summary>Generic-обертка вокруг generic-IList-ов</summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class GenericListWrapper<T> : Wrapper<IList<T>>, IListWrapper<T> {

		#region Constructors
		//.........................................................................
		public GenericListWrapper(IList<T> list) : base(list) { }
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public T this[int index] {
			get { return InternalGet(index); }
			set { InternalSet(index, value); }
		}

		public int Count {
			get { return Wrapped.Count; }
		}

		public bool IsReadOnly {
			get { return Wrapped.IsReadOnly; }
		}
		//.........................................................................
		#endregion


		#region Events
		//.........................................................................
		public event EventHandler<AddItemEventArgs<T>> BeforeAdd;
		public event EventHandler<AddItemEventArgs<T>> AfterAdd;
		public event EventHandler<InsertItemEventArgs<T>> BeforeInsert;
		public event EventHandler<InsertItemEventArgs<T>> AfterInsert;
		public event EventHandler<RemoveItemEventArgs<T>> BeforeRemove;
		public event EventHandler<RemoveItemEventArgs<T>> AfterRemove;
		public event EventHandler<SetItemEventArgs<T>> BeforeSet;
		public event EventHandler<SetItemEventArgs<T>> AfterSet;
		public event EventHandler BeforeClear;
		public event EventHandler AfterClear;
		public event EventHandler<RemoveAtEventArgs> BeforeRemoveAt;
		public event EventHandler<RemoveAtEventArgs> AfterRemoveAt;
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual void Add(T item) {
			AddItemEventArgs<T> args = new AddItemEventArgs<T>(item);
			OnBeforeAdd(args);
			Wrapped.Add(args.Item);
			OnAfterAdd(args);
		}

		public virtual int IndexOf(T item) {
			return Wrapped.IndexOf(item);
		}

		public virtual void Insert(int index, T item) {
			InsertItemEventArgs<T> args = new InsertItemEventArgs<T>(item, index);
			OnBeforeInsert(args);
			Wrapped.Insert(args.Index, args.Item);
			OnAfterInsert(args);
		}

		public virtual bool Remove(T item) {
			if (Contains(item)) {
				RemoveItemEventArgs<T> args = new RemoveItemEventArgs<T>(item);
				OnBeforeRemove(args);
				bool b = Wrapped.Remove(args.Item);
				OnAfterRemove(args);

				return b;
			} else
				return false;
		}

		public virtual void RemoveAt(int index) {
			T item = this[index];
			RemoveAtEventArgs args = new RemoveAtEventArgs(index);
			OnBeforeRemoveAt(args);
			OnBeforeRemove(new RemoveItemEventArgs<T>(item));
			Wrapped.RemoveAt(args.Index);
			OnAfterRemoveAt(args);
			OnAfterRemove(new RemoveItemEventArgs<T>(item));
		}

		public virtual bool Contains(T item) {
			return Wrapped.Contains(item);
		}

		public virtual void CopyTo(T[] array, int arrayIndex) {
			Wrapped.CopyTo(array, arrayIndex);
		}

		public virtual void Clear() {
			OnBeforeClear();
			Wrapped.Clear();
			OnAfterClear();
		}

		public virtual IEnumerator<T> GetEnumerator() {
			return Wrapped.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return ((IEnumerable)Wrapped).GetEnumerator();
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual void OnBeforeClear() {
			EventHandler handler = BeforeClear;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected virtual void OnAfterClear() {
			EventHandler handler = AfterClear;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected virtual void OnBeforeRemoveAt(RemoveAtEventArgs args) {
			EventHandler<RemoveAtEventArgs> handler = BeforeRemoveAt;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterRemoveAt(RemoveAtEventArgs args) {
			EventHandler<RemoveAtEventArgs> handler = AfterRemoveAt;
			if (handler != null)
				handler(this, args);
		}

		protected virtual T InternalGet(int index) {
			return Wrapped[index];
		}

		protected virtual void InternalSet(int index, T value) {
			SetItemEventArgs<T> args = new SetItemEventArgs<T>(value, index);
			OnBeforeSet(args);
			Wrapped[args.Index] = args.Item;
			OnAfterSet(args);
		}

		protected virtual void OnBeforeAdd(AddItemEventArgs<T> args) {
			EventHandler<AddItemEventArgs<T>> handler = BeforeAdd;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterAdd(AddItemEventArgs<T> args) {
			EventHandler<AddItemEventArgs<T>> handler = AfterAdd;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnBeforeInsert(InsertItemEventArgs<T> args) {
			EventHandler<InsertItemEventArgs<T>> handler = BeforeInsert;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterInsert(InsertItemEventArgs<T> args) {
			EventHandler<InsertItemEventArgs<T>> handler = AfterInsert;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnBeforeRemove(RemoveItemEventArgs<T> args) {
			EventHandler<RemoveItemEventArgs<T>> handler = BeforeRemove;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterRemove(RemoveItemEventArgs<T> args) {
			EventHandler<RemoveItemEventArgs<T>> handler = AfterRemove;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnBeforeSet(SetItemEventArgs<T> args) {
			EventHandler<SetItemEventArgs<T>> handler = BeforeSet;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterSet(SetItemEventArgs<T> args) {
			EventHandler<SetItemEventArgs<T>> handler = AfterSet;
			if (handler != null)
				handler(this, args);
		}
		//.........................................................................
		#endregion

	}

	// TODO Сделать serializable. появляется list - создается врапперы

	// TODO Вынести работу с NamedTypeDispatcher-ом в отдельный класс!
	// TODO Держать отдельно реализации IListWrapper и IListWrapper<T> и функции завязывать на них!
	/// <summary>Фасадная обертка вокруг generic и неgeneric IList-ов</summary>
	/// <typeparam name="T">желаемый тип элементов коллекции</typeparam>
	[Serializable]
	public class ListWrapper<T> : Wrapper, IListWrapper, IListWrapper<T> {

		#region Protected Fields
		//.........................................................................
		[NonSerialized]
		protected NamedTypeDispatcher<AnyInvoker> _InnerDispatcher = new NamedTypeDispatcher<AnyInvoker>();
		// XXX Можем нарваться на то что, _InnerDispatcher будет выставлен, а методы не зарегестрированы
		protected NamedTypeDispatcher<AnyInvoker> InnerDispatcher {
			get {
				if (_InnerDispatcher == null) {
					bool f = HasInnerWrapped;
					_InnerDispatcher = new NamedTypeDispatcher<AnyInvoker>();
					RegisterMethods();
					if (InnerWrapped != null && !f)
						Wrap(InnerWrapped);
				}

				return _InnerDispatcher;
			}
		}

		[NonSerialized]
		protected bool HasInnerWrapped;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		protected ListWrapper() {
			RegisterMethods();
		}

		public ListWrapper(IEnumerable list) : this() {
			Wrap(list);
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Invoke(Name name, params object[] args) {
			return Invoke(name, InnerWrapped.GetType(), args);
		}

		public virtual TResult Invoke<TResult>(Name name, params object[] args) {
			return Invoke<TResult>(name, InnerWrapped.GetType(), args);
		}

		public virtual TResult Invoke<TResult>(Name name, Type t, params object[] args) {
			object o = Invoke(name, t, args);
			TResult result = default(TResult);
			try {
				result = (TResult)o;
			} catch { }
			return result;
		}

		public virtual object Invoke(Name name, Type t, params object[] args) {
			object result = null;
			AnyInvoker invoker = InnerDispatcher[name, t];
			if (invoker != null)
				result = invoker(args);

			return result;
		}

		public virtual object InvokeBase(Name name, params object[] args) {
			return InvokeBase(name, InnerWrapped.GetType(), args);
		}

		public virtual TResult InvokeBase<TResult>(Name name, params object[] args) {
			return InvokeBase<TResult>(name, InnerWrapped.GetType(), args);
		}

		public virtual TResult InvokeBase<TResult>(Name name, Type t, params object[] args) {
			object o = InvokeBase(name, t, args);
			TResult result = default(TResult);
			try {
				result = (TResult)o;
			} catch { }
			return result;
		}

		public virtual object InvokeBase(Name name, Type t, params object[] args) {
			object result = null;
			AnyInvoker invoker = InnerDispatcher.Base(name, t);
			if (invoker != null)
				result = invoker(args);

			return result;
		}

		public virtual void Wrap(object list) {
			if (list == null)
				Error.Critical(new ArgumentNullException("list"), typeof(ListWrapper<T>));
			InnerWrapped = list;
			HasInnerWrapped = true;
			Invoke("Wrap");
		}
		//.........................................................................
		#endregion


		#region IListWrapper Members
		//.........................................................................
		event EventHandler<AddItemEventArgs> IListWrapper.BeforeAdd {
			add { Invoke("BeforeAdd_Add", typeof(IListWrapper), value); }
			remove { Invoke("BeforeAdd_Remove", typeof(IListWrapper), value); }
		}

		event EventHandler<AddItemEventArgs> IListWrapper.AfterAdd {
			add { Invoke("AfterAdd_Add", typeof(IListWrapper), value); }
			remove { Invoke("AfterAdd_Remove", typeof(IListWrapper), value); }
		}

		event EventHandler<InsertItemEventArgs> IListWrapper.BeforeInsert {
			add { Invoke("BeforeInsert_Add", typeof(IListWrapper), value); }
			remove { Invoke("BeforeInsert_Remove", typeof(IListWrapper), value); }
		}

		event EventHandler<InsertItemEventArgs> IListWrapper.AfterInsert {
			add { Invoke("AfterInsert_Add", typeof(IListWrapper), value); }
			remove { Invoke("AfterInsert_Remove", typeof(IListWrapper), value); }
		}

		event EventHandler<RemoveItemEventArgs> IListWrapper.BeforeRemove {
			add { Invoke("BeforeRemove_Add", typeof(IListWrapper), value); }
			remove { Invoke("BeforeRemove_Remove", typeof(IListWrapper), value); }
		}

		event EventHandler<RemoveItemEventArgs> IListWrapper.AfterRemove {
			add { Invoke("AfterRemove_Add", typeof(IListWrapper), value); }
			remove { Invoke("AfterRemove_Remove", typeof(IListWrapper), value); }
		}

		event EventHandler<SetItemEventArgs> IListWrapper.BeforeSet {
			add { Invoke("BeforeSet_Add", typeof(IListWrapper), value); }
			remove { Invoke("BeforeSet_Remove", typeof(IListWrapper), value); }
		}

		event EventHandler<SetItemEventArgs> IListWrapper.AfterSet {
			add { Invoke("AfterSet_Add", typeof(IListWrapper), value); }
			remove { Invoke("AfterSet_Remove", typeof(IListWrapper), value); }
		}

		event EventHandler IListWrapper.BeforeClear {
			add { Invoke("BeforeClear_Add", typeof(IListWrapper), value); }
			remove { Invoke("BeforeClear_Remove", typeof(IListWrapper), value); }
		}

		event EventHandler IListWrapper.AfterClear {
			add { Invoke("AfterClear_Add", typeof(IListWrapper), value); }
			remove { Invoke("AfterClear_Remove", typeof(IListWrapper), value); }
		}

		event EventHandler<RemoveAtEventArgs> IListWrapper.BeforeRemoveAt {
			add { Invoke("BeforeRemoveAt_Add", typeof(IListWrapper), value); }
			remove { Invoke("BeforeRemoveAt_Remove", typeof(IListWrapper), value); }
		}

		event EventHandler<RemoveAtEventArgs> IListWrapper.AfterRemoveAt {
			add { Invoke("AfterRemoveAt_Add", typeof(IListWrapper), value); }
			remove { Invoke("AfterRemoveAt_Remove", typeof(IListWrapper), value); }
		}
		//.........................................................................
		#endregion


		#region IList Members
		//.........................................................................
		int IList.Add(object value) {
			return Invoke<int>("Add", typeof(IList), value);
		}

		void IList.Clear() {
			Invoke("Clear");
		}

		bool IList.Contains(object value) {
			return Invoke<bool>("Contains", typeof(IList), value);
		}

		int IList.IndexOf(object value) {
			return Invoke<int>("IndexOf", typeof(IList), value);
		}

		void IList.Insert(int index, object value) {
			Invoke("Insert", index, typeof(IList), value);
		}

		bool IList.IsFixedSize {
			get { return Invoke<bool>("IsFixedSize_Get", typeof(IList)); }
		}

		bool IList.IsReadOnly {
			get { return Invoke<bool>("IsReadOnly_Get", typeof(IList)); }
		}

		void IList.Remove(object value) {
			Invoke("Remove", typeof(IList), value);
		}

		void IList.RemoveAt(int index) {
			Invoke("RemoveAt", typeof(IList), index);
		}

		object IList.this[int index] {
			get {
				return Invoke("this_Get", typeof(IList), index);
			}
			set {
				Invoke("this_Set", typeof(IList), index);
			}
		}
		//.........................................................................
		#endregion


		#region ICollection Members
		//.........................................................................
		void ICollection.CopyTo(Array array, int index) {
			Invoke("CopyTo", typeof(ICollection), array, index);
		}

		int ICollection.Count {
			get { return Invoke<int>("Count_Get", typeof(ICollection)); }
		}

		bool ICollection.IsSynchronized {
			get { return Invoke<bool>("IsSynchronized_Get", typeof(ICollection)); }
		}

		object ICollection.SyncRoot {
			get { return Invoke("SyncRoot_Get", typeof(ICollection)); }
		}
		//.........................................................................
		#endregion


		#region IEnumerable Members
		//.........................................................................
		IEnumerator IEnumerable.GetEnumerator() {
			return Invoke<IEnumerator>("GetEnumerator", typeof(IEnumerable));
		}
		//.........................................................................
		#endregion


		#region IListWrapper<T> Members
		//.........................................................................
		event EventHandler<AddItemEventArgs<T>> IListWrapper<T>.BeforeAdd {
			add { Invoke("BeforeAdd_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("BeforeAdd_Remove", typeof(IListWrapper<T>), value); }
		}

		event EventHandler<AddItemEventArgs<T>> IListWrapper<T>.AfterAdd {
			add { Invoke("AfterAdd_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("AfterAdd_Remove", typeof(IListWrapper<T>), value); }
		}

		event EventHandler<InsertItemEventArgs<T>> IListWrapper<T>.BeforeInsert {
			add { Invoke("BeforeInsert_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("BeforeInsert_Remove", typeof(IListWrapper<T>), value); }
		}

		event EventHandler<InsertItemEventArgs<T>> IListWrapper<T>.AfterInsert {
			add { Invoke("AfterInsert_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("AfterInsert_Remove", typeof(IListWrapper<T>), value); }
		}

		event EventHandler<RemoveItemEventArgs<T>> IListWrapper<T>.BeforeRemove {
			add { Invoke("BeforeRemove_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("BeforeRemove_Remove", typeof(IListWrapper<T>), value); }
		}

		event EventHandler<RemoveItemEventArgs<T>> IListWrapper<T>.AfterRemove {
			add { Invoke("AfterRemove_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("AfterRemove_Remove", typeof(IListWrapper<T>), value); }
		}

		event EventHandler<SetItemEventArgs<T>> IListWrapper<T>.BeforeSet {
			add { Invoke("BeforeSet_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("BeforeSet_Remove", typeof(IListWrapper<T>), value); }
		}

		event EventHandler<SetItemEventArgs<T>> IListWrapper<T>.AfterSet {
			add { Invoke("AfterSet_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("AfterSet_Remove", typeof(IListWrapper<T>), value); }
		}

		event EventHandler IListWrapper<T>.BeforeClear {
			add { Invoke("BeforeClear_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("BeforeClear_Remove", typeof(IListWrapper<T>), value); }
		}

		event EventHandler IListWrapper<T>.AfterClear {
			add { Invoke("AfterClear_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("AfterClear_Remove", typeof(IListWrapper<T>), value); }
		}

		event EventHandler<RemoveAtEventArgs> IListWrapper<T>.BeforeRemoveAt {
			add { Invoke("BeforeRemoveAt_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("BeforeRemoveAt_Remove", typeof(IListWrapper<T>), value); }
		}

		event EventHandler<RemoveAtEventArgs> IListWrapper<T>.AfterRemoveAt {
			add { Invoke("AfterRemoveAt_Add", typeof(IListWrapper<T>), value); }
			remove { Invoke("AfterRemoveAt_Remove", typeof(IListWrapper<T>), value); }
		}
		//.........................................................................
		#endregion


		#region IList<T> Members
		//.........................................................................
		int IList<T>.IndexOf(T item) {
			return Invoke<int>("IndexOf", typeof(IList<T>), item);
		}

		void IList<T>.Insert(int index, T item) {
			Invoke("Insert", typeof(IList<T>), index, item);
		}

		void IList<T>.RemoveAt(int index) {
			Invoke("RemoveAt", typeof(IList<T>), index);
		}

		T IList<T>.this[int index] {
			get {
				return Invoke<T>("this_Get", typeof(IList<T>), index);
			}
			set {
				Invoke("this_Set", typeof(IList<T>), index, value);
			}
		}
		//.........................................................................
		#endregion


		#region ICollection<T> Members
		//.........................................................................
		void ICollection<T>.Add(T item) {
			Invoke("Add", typeof(ICollection<T>), item);
		}

		void ICollection<T>.Clear() {
			Invoke("Clear", typeof(ICollection<T>));
		}

		bool ICollection<T>.Contains(T item) {
			return Invoke<bool>("Contains", typeof(ICollection<T>), item);
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex) {
			Invoke("CopyTo", typeof(ICollection<T>), array, arrayIndex);
		}

		int ICollection<T>.Count {
			get { return Invoke<int>("Count_Get", typeof(ICollection<T>)); }
		}

		bool ICollection<T>.IsReadOnly {
			get { return Invoke<bool>("IsReadOnly_Get", typeof(ICollection<T>)); }
		}

		bool ICollection<T>.Remove(T item) {
			return Invoke<bool>("Remove", typeof(ICollection<T>), item);
		}
		//.........................................................................
		#endregion


		#region IEnumerable<T> Members
		//.........................................................................
		IEnumerator<T> IEnumerable<T>.GetEnumerator() {
			return Invoke<IEnumerator<T>>("GetEnumerator", typeof(IEnumerable<T>));
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual void RegisterMethods() {
			InnerDispatcher["Wrap", typeof(IList)] = WrapList;
			InnerDispatcher["Wrap", typeof(IList<T>)] = WrapGenericList;
			InnerDispatcher["Wrap", typeof(IListWrapper)] = WrapListWrapper;
		}

		protected virtual object WrapList(params object[] args) {
			IListWrapper listWrapper = new SimpleListWrapper(InnerWrapped as IList);
			IListWrapper<T> gListWrapper = new SimpleListWrapper<T>(InnerWrapped as IList);

			CreateMethods(listWrapper, gListWrapper);

			return null;
		}

		protected virtual object WrapGenericList(params object[] args) {
			IListWrapper<T> gListWrapper = new GenericListWrapper<T>(InnerWrapped as IList<T>);

			CreateMethods(null, gListWrapper);

			return null;
		}

		protected virtual object WrapListWrapper(params object[] args) {
			IListWrapper listWrapper = InnerWrapped as IListWrapper;
			IListWrapper<T> gListWrapper = InnerWrapped as IListWrapper<T>;

			CreateMethods(listWrapper, gListWrapper);

			return null;
		}

		// TODO Привести в порядок!
		protected virtual void CreateMethods(IListWrapper listWrapper, IListWrapper<T> gListWrapper) {
			// TODO если listWrapper == null - ломимся в gListWrapper
			// TODO если gListWrapper == null - ломимся в listWrapper
			#region IListWrapper Methods
			if (listWrapper == null) {
				// XXX
			}

			InnerDispatcher["BeforeAdd_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<AddItemEventArgs> h = p[0] as EventHandler<AddItemEventArgs>;
				if (h != null)
					listWrapper.BeforeAdd += h;
				return h;
			};
			InnerDispatcher["BeforeAdd_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<AddItemEventArgs> h = p[0] as EventHandler<AddItemEventArgs>;
				if (h != null)
					listWrapper.BeforeAdd -= h;
				return h;
			};
			InnerDispatcher["AfterAdd_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<AddItemEventArgs> h = p[0] as EventHandler<AddItemEventArgs>;
				if (h != null)
					listWrapper.AfterAdd += h;
				return h;
			};
			InnerDispatcher["AfterAdd_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<AddItemEventArgs> h = p[0] as EventHandler<AddItemEventArgs>;
				if (h != null)
					listWrapper.AfterAdd -= h;
				return h;
			};
			InnerDispatcher["BeforeInsert_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<InsertItemEventArgs> h = p[0] as EventHandler<InsertItemEventArgs>;
				if (h != null)
					listWrapper.BeforeInsert += h;
				return h;
			};
			InnerDispatcher["BeforeInsert_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<InsertItemEventArgs> h = p[0] as EventHandler<InsertItemEventArgs>;
				if (h != null)
					listWrapper.BeforeInsert -= h;
				return h;
			};
			InnerDispatcher["AfterInsert_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<InsertItemEventArgs> h = p[0] as EventHandler<InsertItemEventArgs>;
				if (h != null)
					listWrapper.AfterInsert += h;
				return h;
			};
			InnerDispatcher["AfterInsert_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<InsertItemEventArgs> h = p[0] as EventHandler<InsertItemEventArgs>;
				if (h != null)
					listWrapper.AfterInsert -= h;
				return h;
			};
			InnerDispatcher["BeforeRemove_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<RemoveItemEventArgs> h = p[0] as EventHandler<RemoveItemEventArgs>;
				if (h != null)
					listWrapper.BeforeRemove += h;
				return h;
			};
			InnerDispatcher["BeforeRemove_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<RemoveItemEventArgs> h = p[0] as EventHandler<RemoveItemEventArgs>;
				if (h != null)
					listWrapper.BeforeRemove -= h;
				return h;
			};
			InnerDispatcher["AfterRemove_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<RemoveItemEventArgs> h = p[0] as EventHandler<RemoveItemEventArgs>;
				if (h != null)
					listWrapper.AfterRemove += h;
				return h;
			};
			InnerDispatcher["AfterRemove_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<RemoveItemEventArgs> h = p[0] as EventHandler<RemoveItemEventArgs>;
				if (h != null)
					listWrapper.AfterRemove -= h;
				return h;
			};
			InnerDispatcher["BeforeSet_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<SetItemEventArgs> h = p[0] as EventHandler<SetItemEventArgs>;
				if (h != null)
					listWrapper.BeforeSet += h;
				return h;
			};
			InnerDispatcher["BeforeSet_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<SetItemEventArgs> h = p[0] as EventHandler<SetItemEventArgs>;
				if (h != null)
					listWrapper.BeforeSet -= h;
				return h;
			};
			InnerDispatcher["AfterSet_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<SetItemEventArgs> h = p[0] as EventHandler<SetItemEventArgs>;
				if (h != null)
					listWrapper.AfterSet += h;
				return h;
			};
			InnerDispatcher["AferSet_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<SetItemEventArgs> h = p[0] as EventHandler<SetItemEventArgs>;
				if (h != null)
					listWrapper.AfterSet -= h;
				return h;
			};
			InnerDispatcher["BeforeClear_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler h = p[0] as EventHandler;
				if (h != null)
					listWrapper.BeforeClear += h;
				return h;
			};
			InnerDispatcher["BeforeClear_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler h = p[0] as EventHandler;
				if (h != null)
					listWrapper.BeforeClear -= h;
				return h;
			};
			InnerDispatcher["AfterClear_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler h = p[0] as EventHandler;
				if (h != null)
					listWrapper.AfterClear += h;
				return h;
			};
			InnerDispatcher["AfterClear_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler h = p[0] as EventHandler;
				if (h != null)
					listWrapper.AfterClear -= h;
				return h;
			};
			InnerDispatcher["BeforeRemoveAt_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<RemoveAtEventArgs> h = p[0] as EventHandler<RemoveAtEventArgs>;
				if (h != null)
					listWrapper.BeforeRemoveAt += h;
				return h;
			};
			InnerDispatcher["BeforeRemoveAt_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<RemoveAtEventArgs> h = p[0] as EventHandler<RemoveAtEventArgs>;
				if (h != null)
					listWrapper.BeforeRemoveAt -= h;
				return h;
			};
			InnerDispatcher["AfterRemoveAt_Add", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<RemoveAtEventArgs> h = p[0] as EventHandler<RemoveAtEventArgs>;
				if (h != null)
					listWrapper.AfterRemoveAt += h;
				return h;
			};
			InnerDispatcher["AfterRemoveAt_Remove", typeof(IListWrapper)] = delegate(object[] p) {
				EventHandler<RemoveAtEventArgs> h = p[0] as EventHandler<RemoveAtEventArgs>;
				if (h != null)
					listWrapper.AfterRemoveAt -= h;
				return h;
			};
			#endregion

			#region IList Methods
			InnerDispatcher["Add", typeof(IList)] = delegate(object[] p) {
				listWrapper.Add(p[0]);
				return null;
			};
			InnerDispatcher["Clear", typeof(IList)] = delegate(object[] p) {
				listWrapper.Clear();
				return null;
			};
			InnerDispatcher["Contains", typeof(IList)] = delegate(object[] p) {
				return listWrapper.Contains(p[0]);
			};
			InnerDispatcher["IndexOf", typeof(IList)] = delegate(object[] p) {
				return listWrapper.IndexOf(p[0]);
			};
			InnerDispatcher["Insert", typeof(IList)] = delegate(object[] p) {
				listWrapper.Insert((int)p[0], p[1]);
				return null;
			};
			InnerDispatcher["IsFixedSize_Get", typeof(IList)] = delegate(object[] p) {
				return listWrapper.IsFixedSize;
			};
			InnerDispatcher["IsReadOnly_Get", typeof(IList)] = delegate(object[] p) {
				return listWrapper.IsReadOnly;
			};
			InnerDispatcher["Remove", typeof(IList)] = delegate(object[] p) {
				listWrapper.Remove(p[0]);
				return null;
			};
			InnerDispatcher["RemoveAt", typeof(IList)] = delegate(object[] p) {
				listWrapper.RemoveAt((int)p[0]);
				return null;
			};
			InnerDispatcher["this_Get", typeof(IList)] = delegate(object[] p) {
				return listWrapper[(int)p[0]];
			};
			InnerDispatcher["this_Set", typeof(IList)] = delegate(object[] p) {
				listWrapper[(int)p[0]] = p[1];
				return null;
			};
			#endregion

			#region ICollection Methods
			InnerDispatcher["CopyTo", typeof(ICollection)] = delegate(object[] p) {
				listWrapper.CopyTo((Array)p[0], (int)p[1]);
				return null;
			};
			InnerDispatcher["Count_Get", typeof(ICollection)] = delegate(object[] p) {
				return listWrapper.Count;
			};
			InnerDispatcher["IsSynchronized_Get", typeof(ICollection)] = delegate(object[] p) {
				return listWrapper.IsSynchronized;
			};
			InnerDispatcher["SyncRoot", typeof(ICollection)] = delegate(object[] p) {
				return listWrapper.SyncRoot;
			};
			#endregion

			#region IEnumerable Methods
			InnerDispatcher["GetEnumerator", typeof(IEnumerable)] = delegate(object[] p) {
				return listWrapper.GetEnumerator();
			};
			#endregion

			#region IListWrapper<T> Methods
			InnerDispatcher["BeforeAdd_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<AddItemEventArgs<T>> h = p[0] as EventHandler<AddItemEventArgs<T>>;
				if (h != null)
					gListWrapper.BeforeAdd += h;
				return h;
			};
			InnerDispatcher["BeforeAdd_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<AddItemEventArgs<T>> h = p[0] as EventHandler<AddItemEventArgs<T>>;
				if (h != null)
					gListWrapper.BeforeAdd -= h;
				return h;
			};
			InnerDispatcher["AfterAdd_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<AddItemEventArgs<T>> h = p[0] as EventHandler<AddItemEventArgs<T>>;
				if (h != null)
					gListWrapper.AfterAdd += h;
				return h;
			};
			InnerDispatcher["AfterAdd_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<AddItemEventArgs<T>> h = p[0] as EventHandler<AddItemEventArgs<T>>;
				if (h != null)
					gListWrapper.AfterAdd -= h;
				return h;
			};
			InnerDispatcher["BeforeInsert_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<InsertItemEventArgs<T>> h = p[0] as EventHandler<InsertItemEventArgs<T>>;
				if (h != null)
					gListWrapper.BeforeInsert += h;
				return h;
			};
			InnerDispatcher["BeforeInsert_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<InsertItemEventArgs<T>> h = p[0] as EventHandler<InsertItemEventArgs<T>>;
				if (h != null)
					gListWrapper.BeforeInsert -= h;
				return h;
			};
			InnerDispatcher["AfterInsert_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<InsertItemEventArgs<T>> h = p[0] as EventHandler<InsertItemEventArgs<T>>;
				if (h != null)
					gListWrapper.AfterInsert += h;
				return h;
			};
			InnerDispatcher["AfterInsert_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<InsertItemEventArgs<T>> h = p[0] as EventHandler<InsertItemEventArgs<T>>;
				if (h != null)
					gListWrapper.AfterInsert -= h;
				return h;
			};
			InnerDispatcher["BeforeRemove_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<RemoveItemEventArgs<T>> h = p[0] as EventHandler<RemoveItemEventArgs<T>>;
				if (h != null)
					gListWrapper.BeforeRemove += h;
				return h;
			};
			InnerDispatcher["BeforeRemove_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<RemoveItemEventArgs<T>> h = p[0] as EventHandler<RemoveItemEventArgs<T>>;
				if (h != null)
					gListWrapper.BeforeRemove -= h;
				return h;
			};
			InnerDispatcher["AfterRemove_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<RemoveItemEventArgs<T>> h = p[0] as EventHandler<RemoveItemEventArgs<T>>;
				if (h != null)
					gListWrapper.AfterRemove += h;
				return h;
			};
			InnerDispatcher["AfterRemove_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<RemoveItemEventArgs<T>> h = p[0] as EventHandler<RemoveItemEventArgs<T>>;
				if (h != null)
					gListWrapper.AfterRemove -= h;
				return h;
			};
			InnerDispatcher["BeforeSet_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<SetItemEventArgs<T>> h = p[0] as EventHandler<SetItemEventArgs<T>>;
				if (h != null)
					gListWrapper.BeforeSet += h;
				return h;
			};
			InnerDispatcher["BeforeSet_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<SetItemEventArgs<T>> h = p[0] as EventHandler<SetItemEventArgs<T>>;
				if (h != null)
					gListWrapper.BeforeSet -= h;
				return h;
			};
			InnerDispatcher["AfterSet_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<SetItemEventArgs<T>> h = p[0] as EventHandler<SetItemEventArgs<T>>;
				if (h != null)
					gListWrapper.AfterSet += h;
				return h;
			};
			InnerDispatcher["AferSet_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<SetItemEventArgs<T>> h = p[0] as EventHandler<SetItemEventArgs<T>>;
				if (h != null)
					gListWrapper.AfterSet -= h;
				return h;
			};
			InnerDispatcher["BeforeClear_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler h = p[0] as EventHandler;
				if (h != null)
					gListWrapper.BeforeClear += h;
				return h;
			};
			InnerDispatcher["BeforeClear_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler h = p[0] as EventHandler;
				if (h != null)
					gListWrapper.BeforeClear -= h;
				return h;
			};
			InnerDispatcher["AfterClear_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler h = p[0] as EventHandler;
				if (h != null)
					gListWrapper.AfterClear += h;
				return h;
			};
			InnerDispatcher["AfterClear_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler h = p[0] as EventHandler;
				if (h != null)
					gListWrapper.AfterClear -= h;
				return h;
			};
			InnerDispatcher["BeforeRemoveAt_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<RemoveAtEventArgs> h = p[0] as EventHandler<RemoveAtEventArgs>;
				if (h != null)
					gListWrapper.BeforeRemoveAt += h;
				return h;
			};
			InnerDispatcher["BeforeRemoveAt_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<RemoveAtEventArgs> h = p[0] as EventHandler<RemoveAtEventArgs>;
				if (h != null)
					gListWrapper.BeforeRemoveAt -= h;
				return h;
			};
			InnerDispatcher["AfterRemoveAt_Add", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<RemoveAtEventArgs> h = p[0] as EventHandler<RemoveAtEventArgs>;
				if (h != null)
					gListWrapper.AfterRemoveAt += h;
				return h;
			};
			InnerDispatcher["AfterRemoveAt_Remove", typeof(IListWrapper<T>)] = delegate(object[] p) {
				EventHandler<RemoveAtEventArgs> h = p[0] as EventHandler<RemoveAtEventArgs>;
				if (h != null)
					gListWrapper.AfterRemoveAt -= h;
				return h;
			};
			#endregion

			#region IList<T> Methods
			InnerDispatcher["IndexOf", typeof(IList<T>)] = delegate(object[] p) {
				return gListWrapper.IndexOf((T)p[0]);
			};
			InnerDispatcher["Insert", typeof(IList<T>)] = delegate(object[] p) {
				gListWrapper.Insert((int)p[0], (T)p[1]);
				return null;
			};
			InnerDispatcher["RemoveAt", typeof(IList<T>)] = delegate(object[] p) {
				gListWrapper.RemoveAt((int)p[0]);
				return null;
			};
			InnerDispatcher["this_Get", typeof(IList<T>)] = delegate(object[] p) {
				return gListWrapper[(int)p[0]];
			};
			InnerDispatcher["this_Set", typeof(IList<T>)] = delegate(object[] p) {
				return gListWrapper[(int)p[0]] = (T)p[1];
			};
			#endregion

			#region ICollection<T> Methods
			InnerDispatcher["Add", typeof(ICollection<T>)] = delegate(object[] p) {
				gListWrapper.Add((T)p[0]);
				return null;
			};
			InnerDispatcher["Clear", typeof(ICollection<T>)] = delegate(object[] p) {
				gListWrapper.Clear();
				return null;
			};
			InnerDispatcher["Contains", typeof(ICollection<T>)] = delegate(object[] p) {
				return gListWrapper.Contains((T)p[0]);
			};
			InnerDispatcher["CopyTo", typeof(ICollection<T>)] = delegate(object[] p) {
				gListWrapper.CopyTo((T[])p[0], (int)p[1]);
				return null;
			};
			InnerDispatcher["Count_Get", typeof(ICollection<T>)] = delegate(object[] p) {
				return gListWrapper.Count;
			};
			InnerDispatcher["IsReadOnly_Get", typeof(ICollection<T>)] = delegate(object[] p) {
				return gListWrapper.IsReadOnly;
			};
			InnerDispatcher["Remove", typeof(ICollection<T>)] = delegate(object[] p) {
				return gListWrapper.Remove((T)p[0]);
			};
			#endregion

			#region IEnumerable<T> Methods
			InnerDispatcher["GetEnumerator", typeof(IEnumerable<T>)] = delegate(object[] p) {
				return gListWrapper.GetEnumerator();
			};
			#endregion
		}
		//.........................................................................
		#endregion

	}

	/// <summary>Фабрика врапперов вокруг списков</summary>
	public interface IListWrapperFactory {
		IListWrapper GetWrapper(IList collection);
		IListWrapper<T> GetWrapper<T>(IList collection);
		IListWrapper<T> GetWrapper<T>(IList<T> collection);
		void RegisterFactory(Type itemType, ListWrapperFactoryDelegate factory);
	}

	/// <summary>"Делегат"-фабрика IListWrapper-а и generic-IListWrapper-а</summary>
	public abstract class ListWrapperFactoryDelegate {
		public abstract IListWrapper<T> Invoke<T>(IEnumerable collection);
		public abstract IListWrapper Invoke(IEnumerable collection);
	}

	/// <summary>Базовая фабрика list-врапперов</summary>
	public abstract class ListWrapperFactoryBase : IListWrapperFactory {

		#region Protected Fields
		//.........................................................................
		protected TypeDispatcher<ListWrapperFactoryDelegate> InnerFactories =
			new TypeDispatcher<ListWrapperFactoryDelegate>();
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		protected ListWrapperFactoryBase() {
			RegisterStandardFactories();
		}
		//.........................................................................
		#endregion


		#region IListWrapperFactory Implementation
		//.........................................................................
		public virtual IListWrapper GetWrapper(IList collection) {
			IListWrapper wrapper = null;
			if (collection != null) {
				ListWrapperFactoryDelegate d = InnerFactories[collection.GetType()];
				if (d != null)
					wrapper = d.Invoke(collection);
			}

			return wrapper;
		}

		public virtual IListWrapper<T> GetWrapper<T>(IList collection) {
			IListWrapper<T> wrapper = null;
			if (collection != null) {
				ListWrapperFactoryDelegate d = InnerFactories[collection.GetType()];
				if (d != null)
					wrapper = d.Invoke<T>(collection);
			}

			return wrapper;
		}

		public virtual IListWrapper<T> GetWrapper<T>(IList<T> collection) {
			IListWrapper<T> wrapper = null;
			if (collection != null) {
				ListWrapperFactoryDelegate d = InnerFactories[collection.GetType()];
				if (d != null)
					wrapper = d.Invoke<T>(collection);
			}

			return wrapper;
		}

		public virtual void RegisterFactory(Type itemType, ListWrapperFactoryDelegate factory) {
			InnerFactories[itemType] = factory;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		// TODO как-то странно......
		protected abstract void RegisterStandardFactories();
		//.........................................................................
		#endregion

	}

	/// <summary>Фабрика врапперов - генерирует generic-ListWrapper</summary>
	public class ListWrapperFactory : ListWrapperFactoryBase {

		#region Protected Methods
		//.........................................................................
		protected override void RegisterStandardFactories() {
			RegisterFactory(typeof(IEnumerable), new FactoryHandler());
		}
		//.........................................................................
		#endregion


		#region Nested Types
		//.........................................................................
		protected class FactoryHandler : ListWrapperFactoryDelegate {
			#region Public Methods
			public override IListWrapper<T> Invoke<T>(IEnumerable collection) {
				return new ListWrapper<T>(collection);
			}

			public override IListWrapper Invoke(IEnumerable collection) {
				return new ListWrapper<object>(collection);
			}
			#endregion
		}
		//.........................................................................
		#endregion

	}

	/// <summary>Обертка вокруг IEnumerator-ов, превращающая обычный IEnumerator в generic-аналог</summary>
	public class EnumeratorWrapper<T> : Wrapper<IEnumerator>, IEnumerator<T> {

		#region Constructors
		//.........................................................................
		public EnumeratorWrapper(IEnumerator enumerator) : base(enumerator) { }
		//.........................................................................
		#endregion


		#region IEnumerator<T> Members
		//.........................................................................
		public virtual T Current {
			get { return (T)Wrapped.Current; }
		}
		//.........................................................................
		#endregion


		#region IDisposable Members
		//.........................................................................
		public virtual void Dispose() { }
		//.........................................................................
		#endregion


		#region IEnumerator Members
		//.........................................................................
		object IEnumerator.Current {
			get { return this.Current; }
		}

		public virtual bool MoveNext() {
			return Wrapped.MoveNext();
		}

		public virtual void Reset() {
			Wrapped.Reset();
		}
		//.........................................................................
		#endregion

	}
}
