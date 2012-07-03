using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Front.Collections.Generic {

	public interface IGenericCollection<T> : ICollection<T>, IList<T> {
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

		IGenericCollection<T> Filter(Predicate<T> filter);
		void Apply(Action<T> apply);
		void Apply(Action<T> apply, Predicate<T> filter);
	}

	public class Collection<T> : CollectionBase, IGenericCollection<T> {
        #region Constructors
        public Collection() : base() { }
        public Collection(int capacity) : base(capacity) { }

        public Collection(IEnumerable<T> obj) {
			if (obj != null) 
				AddRange(obj);
        }
        #endregion

        #region Public Properties
        public virtual T this[int index] {
            get { return (T)List[index]; }
			set { InternalSet(index, value); }
        }

        public virtual bool IsReadOnly { get { return List.IsReadOnly; } }
        #endregion

		#region Events
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
		#endregion

		#region Public Methods
		public virtual void Add(T value) {
			AddItemEventArgs<T> args = new AddItemEventArgs<T>(value);
			OnBeforeAdd(args);
			List.Add(args.Item);
			OnAfterAdd(args);
        }

        public virtual int IndexOf(T value) {
            return List.IndexOf(value);
        }

        public virtual void Insert(int index, T value) {
			InsertItemEventArgs<T> args = new InsertItemEventArgs<T>(value, index);
			OnBeforeInsert(args);
            List.Insert(args.Index, args.Item);
			OnAfterInsert(args);
        }

        public virtual bool Remove(T value) {
			if (List.Contains(value)) {
				RemoveItemEventArgs<T> args = new RemoveItemEventArgs<T>(value);
				OnBeforeRemove(args);
				List.Remove(args.Item);
				OnAfterRemove(args);
				return true;
			} else
				return false;
        }

        public virtual bool Contains(T value) {
            return List.Contains(value);
        }

        public virtual void CopyTo(T[] array, int arrayIndex) {
            List.CopyTo(array, arrayIndex);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return new CollectionBaseEnumerator(this);
        }

        public virtual void AddRange(IEnumerable<T> collection) {
            IEnumerator<T> e = collection.GetEnumerator();
            e.Reset();
            while (e.MoveNext())
                Add(e.Current);
        }

        public override bool Equals(object obj) {
            if (obj == null) return false;
            Collection<T> cb = obj as Collection<T>;
            if (cb == null) return false;

            return this.GetHashCode() == cb.GetHashCode();
        }

        public override int GetHashCode() {
            int hash = 0;
            foreach (T item in List) {
                hash += item.GetHashCode();
            }

            return hash;
        }

		public virtual IGenericCollection<T> Filter(Predicate<T> filter) {
			Collection<T> list = new Collection<T>();
			if (filter != null) {
				foreach (T item in this) {
					if (filter(item))
						list.Add(item);
				}
			}

			return list;
		}

		public virtual void Apply(Action<T> apply) {
			Apply(apply, this);
		}

		public virtual void Apply(Action<T> apply, Predicate<T> filter) {
			Apply(apply, Filter(filter));
		}

		public static void Apply(Action<T> apply, IEnumerable<T> list) {
			if (apply != null) {
				if (list != null) {
					foreach (T item in list)
						apply(item);
				}
			}
		}
        #endregion

		#region Protected Methods
		protected override void OnClear() {
			base.OnClear();
			EventHandler handler = BeforeClear;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected override void OnClearComplete() {
			base.OnClearComplete();
			EventHandler handler = AfterClear;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected override void OnRemove(int index, object value) {
			base.OnRemove(index, value);
			EventHandler<RemoveAtEventArgs> handler = BeforeRemoveAt;
			if (handler != null)
				handler(this, new RemoveAtEventArgs(index));
		}

		protected override void OnRemoveComplete(int index, object value) {
			base.OnRemoveComplete(index, value);
			EventHandler<RemoveAtEventArgs> handler = AfterRemoveAt;
			if (handler != null)
				handler(this, new RemoveAtEventArgs(index));
		}

		protected virtual void InternalSet(int index, T value) {
			SetItemEventArgs<T> args = new SetItemEventArgs<T>(value, index);
			OnBeforeSet(args);
			List[args.Index] = args.Item;
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
		#endregion

		#region Nested Types
		public class CollectionBaseEnumerator : IEnumerator<T> {
            protected IEnumerator InnerEnumerator;

            internal CollectionBaseEnumerator(Collection<T> enumerable) {
                InnerEnumerator = enumerable.InnerList.GetEnumerator();
                Reset();
            }

            public void Reset() {
                InnerEnumerator.Reset();
            }

            public bool MoveNext() {
                return InnerEnumerator.MoveNext();
            }

            object IEnumerator.Current {
                get { return InnerEnumerator.Current; }
            }

            public T Current {
                get { return (T)InnerEnumerator.Current; }
            }

            public void Dispose() {
            }
		}
		#endregion
	}

	// XXX Довести до ума!!!
	// TODO Написать тесты
	public class FilteredCollection<T> : Wrapper<IGenericCollection<T>>, IGenericCollection<T> {
		#region Protected Fields
		protected IGenericCollection<T> InnerItems;
		#endregion

		#region Constructors
		public FilteredCollection(IGenericCollection<T> wrapped) : base(wrapped) {
			wrapped.AfterAdd += WrappedAfterAdd;
			wrapped.AfterInsert += WrappedAfterInsert;
			wrapped.AfterRemove += WrappedAfterRemove;
			wrapped.AfterSet += WrappedAfterSet;
			wrapped.AfterClear += WrappedAfterClear;
			wrapped.BeforeRemoveAt += WrappedBeforeRemoveAt;

			InnerItems = wrapped.Filter(FilterHandler);
		}
		#endregion

		public Predicate<T> FilterPredicate;

		#region Protected Methods
		protected virtual bool FilterHandler(T item) {
			Predicate<T> handler = FilterPredicate;
			if (handler != null)
				return handler(item);

			return false;
		}

		protected virtual void WrappedAfterAdd(object sender, AddItemEventArgs<T> args) {
			InnerItems.Add(args.Item);
		}

		protected virtual void WrappedAfterInsert(object sender, InsertItemEventArgs<T> args) {
			InnerItems.Insert(args.Index, args.Item);
		}

		protected virtual void WrappedAfterRemove(object sender, RemoveItemEventArgs<T> args) {
			InnerItems.Remove(args.Item);
		}

		protected virtual void WrappedAfterSet(object sender, SetItemEventArgs<T> args) {
			InnerItems[args.Index] = args.Item;
		}

		protected virtual void WrappedAfterClear(object sender, EventArgs args) {
			InnerItems.Clear();
		}

		protected virtual void WrappedBeforeRemoveAt(object sender, RemoveAtEventArgs args) {
			InnerItems.Remove(Wrapped[args.Index]);
		}
		#endregion

		#region IGenericCollection<T> Members
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

		public virtual IGenericCollection<T> Filter(Predicate<T> filter) {
			return InnerItems.Filter(filter);
		}

		public virtual void Apply(Action<T> apply) {
			InnerItems.Apply(apply);
		}

		public virtual void Apply(Action<T> apply, Predicate<T> filter) {
			InnerItems.Apply(apply, filter);
		}
		#endregion

		#region ICollection<T> Members
		public virtual void Add(T item) {
			AddItemEventArgs<T> args = new AddItemEventArgs<T>(item);
			OnBeforeAdd(args);
			InnerItems.Add(args.Item);
			Wrapped.Add(args.Item);
			OnAfterAdd(args);
		}
		
		public virtual void Clear() {
			OnBeforeClear();
			InnerItems.Clear();
			Wrapped.Clear();
			OnAfterClear();
		}

		public virtual bool Contains(T item) {
			return Wrapped.Contains(item);
		}

		public virtual void CopyTo(T[] array, int arrayIndex) {
			InnerItems.CopyTo(array, arrayIndex);
		}

		public int Count { get { return InnerItems.Count; } }
		public bool IsReadOnly { get { return false; } }

		public virtual bool Remove(T item) {
			if (InnerItems.Contains(item)) {
				RemoveItemEventArgs<T> args = new RemoveItemEventArgs<T>(item);
				OnBeforeRemove(args);
				InnerItems.Remove(args.Item);
				Wrapped.Remove(args.Item);
				OnAfterRemove(args);

				return true;
			}

			return false;
		}
		#endregion

		#region IEnumerable<T> Members
		public virtual IEnumerator<T> GetEnumerator() {
			return InnerItems.GetEnumerator();
		}
		#endregion

		#region IEnumerable Members
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
		#endregion

		#region IList<T> Members

		public virtual int IndexOf(T item) {
			return InnerItems.IndexOf(item);
		}

		public virtual void Insert(int index, T item) {
			InsertItemEventArgs<T> args = new InsertItemEventArgs<T>(item, index);
			OnBeforeInsert(args);
			int newindex = Wrapped.IndexOf(InnerItems[args.Index]);
			InnerItems.Insert(args.Index, args.Item);
			Wrapped.Insert(newindex, item);
			OnAfterInsert(args);
		}

		public virtual void RemoveAt(int index) {
			RemoveAtEventArgs args = new RemoveAtEventArgs(index);
			OnBeforeRemoveAt(args);
			int newindex = Wrapped.IndexOf(InnerItems[index]);
			InnerItems.RemoveAt(args.Index);
			Wrapped.RemoveAt(newindex);
			OnAfterRemoveAt(args);
		}
		//....................................................................................
		public T this[int index] {
			get { return InnerItems[index]; }
			set { InternalSet(index, value); }
		}
		#endregion

		#region Protected Fields
		protected virtual void InternalSet(int index, T value) {
			SetItemEventArgs<T> args = new SetItemEventArgs<T>(value, index);
			OnBeforeSet(args);
			int newindex = Wrapped.IndexOf(InnerItems[args.Index]);
			InnerItems[args.Index] = args.Item;
			Wrapped[newindex] = value;
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
		#endregion
	}

	public class CollectionItemEventArgs<TItem> : EventArgs {
		protected TItem InnerItem;

		public CollectionItemEventArgs(TItem item) {
			InnerItem = item;
		}

		public TItem Item { get { return InnerItem; } set { InnerItem = value; } }
	}

	public class AddItemEventArgs<TItem> : CollectionItemEventArgs<TItem> {
		public AddItemEventArgs(TItem item) : base(item) { }
	}

	public class AddItemEventArgs : AddItemEventArgs<object> {
		public AddItemEventArgs(object item) : base(item) { }
	}

	public class RemoveItemEventArgs<TItem> : CollectionItemEventArgs<TItem> {
		public RemoveItemEventArgs(TItem item) : base(item) { }
	}

	public class RemoveItemEventArgs : RemoveItemEventArgs<object> {
		public RemoveItemEventArgs(object item) : base(item) { }
	}

	public class SetItemEventArgs<TItem> : CollectionItemEventArgs<TItem> {
		protected int InnerIndex;

		public SetItemEventArgs(TItem item, int index) : base(item) { 
			InnerIndex = index;
		}

		public int Index { get { return InnerIndex; } set  { InnerIndex = value; } }
	}

	public class SetItemEventArgs : SetItemEventArgs<object> {
		public SetItemEventArgs(object item, int index) : base(item, index) { }
	}

	public class InsertItemEventArgs<TItem> : CollectionItemEventArgs<TItem> {
		protected int InnerIndex;

		public InsertItemEventArgs(TItem item, int index)
			: base(item) {
			InnerIndex = index;
		}

		public int Index { get { return InnerIndex; } set { InnerIndex = value; } }
	}

	public class InsertItemEventArgs : InsertItemEventArgs<object> {
		public InsertItemEventArgs(object item, int index) : base(item, index) { }
	}

	public class RemoveAtEventArgs : EventArgs {
		protected int InnerIndex;

		public RemoveAtEventArgs(int index) {
			InnerIndex = index;
		}

		public int Index { get { return InnerIndex; } }
	}
}
