// $Id: Collections.cs 2421 2006-09-19 07:26:55Z kostya $

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Runtime.Remoting.Messaging;
using System.Collections.Generic;

namespace Front.Collections {


	// TODO: нужен какой-то FixedOrderDictionary<T>...
	// TODO: сделать ему правильные Enumerator

	/// <summary>Упорядоченный <see cref="IDictionary"/>.</summary>
	/// <remarks>Свойство <see cref="Keys"/> всегда возвращает элементы в том порядке, в
	/// котором они добавлялись.</remarks>
	[Serializable]
	public class FixedOrderDictionary : Hashtable {
		protected ArrayList indexList = new ArrayList(); // TODO: не лучший вариант для скорости?

		/// <summary>Initialize a new instance of the <see cref="FixedOrderDictionary"/></summary>
		public FixedOrderDictionary() : this(0) { }

		/// <summary>Initialize a new instance of the <see cref="FixedOrderDictionary"/></summary>
		public FixedOrderDictionary(int capacity) : this(capacity, 1.0f) { }

		/// <summary>Initialize a new instance of the <see cref="FixedOrderDictionary"/></summary>
		public FixedOrderDictionary(int capacity, float loadFactor) : this(capacity, loadFactor, null, null) { }

		/// <summary>Initialize a new instance of the <see cref="FixedOrderDictionary"/></summary>
		public FixedOrderDictionary(IDictionary dictionary) : this(dictionary, 1.0f) { }

		/// <summary>Initialize a new instance of the <see cref="FixedOrderDictionary"/></summary>
		public FixedOrderDictionary(IDictionary dictionary, float loadFactor) : this(dictionary, loadFactor, null, null) { }

		/// <summary>Initialize a new instance of the <see cref="FixedOrderDictionary"/></summary>
		public FixedOrderDictionary(IDictionary dictionary, float loadFactor,
				IHashCodeProvider hcp, IComparer comparer)
			: this(0, loadFactor, hcp, comparer) {
			if (dictionary == null) throw new ArgumentNullException("dictionary");
			foreach (DictionaryEntry de in dictionary)
				this.Add(de.Key, de.Value);
		}

		/// <summary>Initialize a new instance of the <see cref="FixedOrderDictionary"/></summary>
		public FixedOrderDictionary(IHashCodeProvider hcp, IComparer comparer) : this(0, hcp, comparer) { }

		/// <summary>Initialize a new instance of the <see cref="FixedOrderDictionary"/></summary>
		public FixedOrderDictionary(int capacity, IHashCodeProvider hcp, IComparer comparer) : this(capacity, 1.0f, hcp, comparer) { }

		/// <summary>Initialize a new instance of the <see cref="FixedOrderDictionary"/></summary>
		public FixedOrderDictionary(int capacity, float loadFactor,
				IHashCodeProvider hcp, IComparer comparer)
			: base(capacity, loadFactor, hcp, comparer) {
			indexList = new ArrayList(capacity);
		}

		/// <summary>Initialize a new instance of the <see cref="FixedOrderDictionary"/></summary>
		protected FixedOrderDictionary(SerializationInfo info, StreamingContext context)
			: base(info, context) {
			indexList = (ArrayList)info.GetValue("index", typeof(ArrayList));
		}

		/// <summary>Implements the <see cref="ISerializable"/> interface and returns the data needed
		/// to serialize the <see cref="FixedOrderDictionary"/>.</summary>
		/// <param name="info">A <see cref="SerializationInfo"/> object containing the information required
		/// to serialize the <see cref="FixedOrderDictionary"/>.</param>
		/// <param name="context">A <see cref="StreamingContext"/> object containing the source and
		/// destination of the serialized stream associated with the <see cref="FixedOrderDictionary"/>.</param>
		public override void GetObjectData(SerializationInfo info, StreamingContext context) {
			base.GetObjectData(info, context);
			info.AddValue("index", indexList);
		}

		/// <summary>Gets an <see cref="ICollection"/> containing the keys
		/// in the <see cref="FixedOrderDictionary"/>.</summary>
		/// <value>An <see cref="ICollection"/> containing the keys in the <see cref="FixedOrderDictionary"/>.</value>
		public override ICollection Keys {
			get { return ArrayList.ReadOnly(indexList); }
		}

		/// <summary>Adds an element with the provided key and value to the IDictionary.</summary>  
		/// <param name="key">The Object to use as the key of the element to add.</param>
		/// <param name="value">The Object to use as the value of the element to add.</param>
		/// <exception cref="ArgumentNullException">key is a null reference (Nothing in Visual Basic).</exception>
		/// <exception cref="ArgumentException">An element with the same key already exists in the IDictionary.</exception>
		/// <exception cref="NotSupportedException">The IDictionary is read-only. 
		/// <para>-or-</para>
		/// <para>The IDictionary has a fixed size.</para></exception>
		/// <remarks>The Item property can also be used to add new elements by setting the value of a key that
		/// does not exist in the dictionary. For example: myCollection["myNonexistentKey"] = myValue. However,
		/// if the specified key already exists in the dictionary, setting the Item property overwrites the old
		/// value. In contrast, the Add method does not modify existing elements.</remarks>
		public override void Add(object key, object value) {
			base.Add(key, value);
			indexList.Add(key);
		}

		/// <summary>Removes all owned variables from the <see cref="FixedOrderDictionary"/>.</summary>
		public override void Clear() {
			base.Clear();
			indexList.Clear();
		}

		/// <summary>Removes the element with the specified key from the <see cref="FixedOrderDictionary"/>.</summary>
		/// <param name="key">The key of the variable to remove.</param>
		/// <exception cref="ArgumentNullException">key is a null reference (Nothing in Visual Basic).</exception>
		/// <exception cref="NotSupportedException">The IDictionary is read-only. 
		/// <para>-or-</para>
		/// <para>The IDictionary has a fixed size.</para></exception>
		public override void Remove(object key) {
			base.Remove(key);
			for (int i = 0; i < indexList.Count; i++)
				if (this.KeyEquals(key, indexList[i])) {
					indexList.RemoveAt(i);
					break;
				}
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		/// <value>A new object that is a copy of this instance.</value>
		public override object Clone() {
			FixedOrderDictionary od = new FixedOrderDictionary(indexList.Count, this.hcp, this.comparer);
			foreach (object key in indexList)
				od.Add(key, this[key]);
			return od;
		}

		public virtual object this[int index] {
			get {
				if (index <0 || index >= Count)
					return null;
				else
					return base[indexList[index]];
			}
			set {
				if (index >= 0 && index < Count)
					base[indexList[index]] = value;
			}
		}

		public override object this[object key] {
			get { return base[key]; }
			set {
				// TODO: продумать это по-лучше!
				// Hashtable работает без блокировки...
				base[key] = value;
				if (!indexList.Contains(key))
					indexList.Add(key);
			}
		}

		public virtual int GetKeyIndex(object key) {
			for (int i = 0; i < indexList.Count; i++)
				if (KeyEquals(key, indexList[i]))
					return i;
			return -1;
		}

		public virtual void InsertAt(int index, object key, object value) {
			lock(this) {
				base[key] = value;
				
				int ix = GetKeyIndex(key);
				// повторная вставка InseraAt(-1, key, value) выбрасывает объект в конец списка

				if (index >= indexList.Count || index < 0) {
					index = indexList.Count;
					indexList.Add(key);
				} else
					indexList.Insert(index, key);

				if (ix >= 0 && ix != index) {
					if (ix < index)
						indexList.RemoveAt(ix+1);
					else
						indexList.RemoveAt(ix);
				}
			}
		}

		public static FixedOrderDictionary Prepare(params object[] args) {
			FixedOrderDictionary res = new FixedOrderDictionary();
			if (args != null)
				for (int i = 0; i < args.Length; i++)
					res.Add(args[i], (i + 1 < args.Length) ? args[++i] : null);
			return res;
		}

	}



	/// <summary>Обертка для <see cref="IDictionary"/>, которая защищает его от изменений.</summary>
	/// <remarks>Класс используется тогда, когда нужно запретить редактирование словаря, передаваемого в чужой код.</remarks>
	/// <threadsafety static="true" instance="true"/>
	[Serializable]
	public class ReadOnlyDictionary : IDictionary, ISerializable {
		static ReadOnlyDictionary emptyDictionary;
		IDictionary source;

		/// <summary>Создать новый <see cref="ReadOnlyDictionary"/> и обернуть его вокруг указанного словаря.</summary>
		/// <remarks>Изменение исходного <see cref="IDictionary"/> будут видны через <see cref="ReadOnlyDictionary"/>.
		/// </remarks>
		/// <param name="source">Исходный словарь.</param>
		public ReadOnlyDictionary(IDictionary source)
			: this(source, false) {
		}

		/// <summary>Создать новый <see cref="ReadOnlyDictionary"/> в процессе десериализации.</summary>
		/// <param name="info">A <see cref="SerializationInfo"/> object containing the information
		/// required to serialize the <see cref="ReadOnlyDictionary"/>.</param>
		/// <param name="context">A <see cref="StreamingContext"/> object containing the source and
		/// destination of the serialized stream associated with the <see cref="ReadOnlyDictionary"/>. </param>
		protected ReadOnlyDictionary(SerializationInfo info, StreamingContext context) {
			if (info == null) throw new ArgumentNullException("info");
			source = (IDictionary)info.GetValue("source", typeof(IDictionary));
		}

		/// <summary>Implements the <see cref="ISerializable"/> interface and returns the data needed
		/// to serialize the <see cref="ReadOnlyDictionary"/>.</summary>
		/// <param name="info">A <see cref="SerializationInfo"/> object containing the information
		/// required to serialize the <see cref="ReadOnlyDictionary"/>.</param>
		/// <param name="context">A <see cref="StreamingContext"/> object containing the source and
		/// destination of the serialized stream associated with the <see cref="ReadOnlyDictionary"/>. </param>
		/// <exception cref="ArgumentNullException">info is a null reference (Nothing in Visual Basic).</exception>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
			info.AddValue("source", source);
		}

		/// <summary>Создать новый <see cref="ReadOnlyDictionary"/> и обернуть его вокруг указанного словаря или
		/// скопировать все его содержимое.</summary>
		/// <remarks>Изменение исходного <see cref="IDictionary"/> будут видны через <see cref="ReadOnlyDictionary"/>, в
		/// случае если <paramref name="copy"/> равно <c>false</c> и не будут влиять на <see cref="ReadOnlyDictionary"/> в
		/// обратном случае.
		/// </remarks>
		/// <param name="source">Исходный словарь.</param>
		/// <param name="copy">Указание следует ли копировать значения в собственный словарь или можно ссылатся
		/// на <paramref name="source"/>.</param>
		public ReadOnlyDictionary(IDictionary source, bool copy) {
			if (source == null)
				this.source = new ListDictionary();
			else if (copy) {
				this.source = new HybridDictionary(source.Count);
				foreach (DictionaryEntry entry in source)
					this.source.Add(entry.Key, entry.Value);
			}
			else
				this.source = source;
		}

		/// <summary>Gets a value indicating whether the <see cref="ReadOnlyDictionary"/> has a fixed size.</summary>
		/// <value>Возвращает <c>true</c>.</value>
		/// <remarks><see cref="ReadOnlyDictionary"/> это неизменяемый словарь, поэтому значение этого свойства всегда равно <c>true</c>.</remarks>
		public bool IsFixedSize { get { return true; } }
		/// <summary>Gets a value indicating whether the <see cref="ReadOnlyDictionary"/> is read-only.</summary>
		/// <value>Возвращает <c>true</c>.</value>
		/// <remarks><see cref="ReadOnlyDictionary"/> это неизменяемый словарь, поэтому значение этого свойства всегда равно <c>true</c>.</remarks>
		public bool IsReadOnly { get { return true; } }

		/// <summary>Gets the value associated with the specified key.</summary>
		/// <remarks><see cref="ReadOnlyDictionary"/> это неизменяемый словарь, поэтому
		/// попытка изменения словаря приведет к исключению.</remarks>
		/// <exception cref="NotSupportedException">The property is set.</exception>
		/// <value>The value associated with the specified key. If the specified key is not found,
		/// attempting to get it returns a null reference (Nothing in Visual Basic).</value>
		/// <param name="key">The key whose value to get.</param>
		public virtual object this[object key] {
			get { return source[key]; }
			set { throw new NotSupportedException(); }
		}

		/// <summary>Gets an ICollection containing the keys in the <see cref="ReadOnlyDictionary"/>.</summary>
		/// <remarks>The order of the keys in the ICollection is unspecified, but it is the same order as
		/// the associated values in the ICollection returned by the <see cref="Values"/> method.</remarks>
		public ICollection Keys { get { return source.Keys; } }

		/// <summary>Gets an ICollection containing the values in the <see cref="ReadOnlyDictionary"/>.</summary>
		/// <remarks>The order of the values in the ICollection is unspecified, but it is the same order as
		/// the associated keys in the ICollection returned by the <see cref="Keys"/> method.</remarks>
		public ICollection Values { get { return source.Values; } }

		/// <summary>Always throws <see cref="NotSupportedException"/>.</summary>
		/// <remarks><see cref="ReadOnlyDictionary"/> это неизменяемый словарь, поэтому
		/// попытка изменения словаря приведет к исключению.</remarks>
		/// <exception cref="NotSupportedException">Always.</exception>
		/// <param name="key">The key of the element to add.</param>
		/// <param name="value">The value of the element to add. The value can be a null
		/// reference (Nothing in Visual Basic).</param>
		void IDictionary.Add(object key, object value) {
			throw new NotSupportedException();
		}

		/// <summary>Always throws <see cref="NotSupportedException"/>.</summary>
		/// <remarks><see cref="ReadOnlyDictionary"/> это неизменяемый словарь, поэтому
		/// попытка изменения словаря приведет к исключению.</remarks>
		/// <exception cref="NotSupportedException">Always.</exception>
		void IDictionary.Clear() {
			throw new NotSupportedException();
		}

		/// <summary>Determines whether the <see cref="ReadOnlyDictionary"/> contains a specific key.</summary>
		/// <param name="key">The key to locate in the <see cref="ReadOnlyDictionary"/>.</param>
		/// <value>true if the <see cref="ReadOnlyDictionary"/> contains an element with the specified key; otherwise, false.</value>
		/// <remarks>Contains implements <see cref="IDictionary.Contains"/>.</remarks>
		public bool Contains(object key) {
			return source.Contains(key);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		/// <summary>Returns an IDictionaryEnumerator that can iterate through the <see cref="ReadOnlyDictionary"/>.</summary>
		/// <value>An IDictionaryEnumerator for the <see cref="ReadOnlyDictionary"/>.</value>
		/// <remarks>Enumerators only allow reading the data in the collection. Enumerators cannot be used to modify the underlying collection.</remarks>
		public IDictionaryEnumerator GetEnumerator() {
			return source.GetEnumerator();
		}

		/// <summary>Always throws <see cref="NotSupportedException"/>.</summary>
		/// <remarks><see cref="ReadOnlyDictionary"/> это неизменяемый словарь, поэтому
		/// попытка изменения словаря приведет к исключению.</remarks>
		/// <exception cref="NotSupportedException">Always.</exception>
		/// <param name="key">The key of the element to remove.</param>
		void IDictionary.Remove(object key) {
			throw new NotSupportedException();
		}

		/// <summary>Gets the number of key-and-value pairs contained in the <see cref="ReadOnlyDictionary"/>.</summary>
		/// <value>The number of key-and-value pairs contained in the <see cref="ReadOnlyDictionary"/>.</value>
		public int Count { get { return source.Count; } }

		/// <summary>Gets a value indicating whether access to the <see cref="ReadOnlyDictionary"/> is synchronized (thread-safe).</summary>
		/// <value>Always true.</value>
		/// <remarks>Так как <see cref="ReadOnlyDictionary"/> не позволяет модифицировать словарь, то можно считать
		/// работу с этим классом потокобезопасной. Следует помнить, что словарь может изменятся путем модификации исходного
		/// словаря, если <see cref="ReadOnlyDictionary"/> моздавался без копирования данных. В этом случае, модификация
		/// исходного словаря может повлечь за собой ошибки при перечислении (проходу итератором). Используйте
		/// <see cref="SyncRoot"/> для защиты от изменений во время перечисления.</remarks>
		public bool IsSynchronized { get { return true; } }

		/// <summary>Gets an object that can be used to synchronize access to the <see cref="ReadOnlyDictionary"/>.</summary>
		/// <remarks>Так как <see cref="ReadOnlyDictionary"/> не позволяет модифицировать словарь, то можно считать
		/// работу с этим классом потокобезопасной. Следует помнить, что словарь может изменятся путем модификации исходного
		/// словаря, если <see cref="ReadOnlyDictionary"/> моздавался без копирования данных. В этом случае, модификация
		/// исходного словаря может повлечь за собой ошибки при перечислении (проходу итератором). Используйте
		/// <see cref="SyncRoot"/> для защиты от изменений во время перечисления.</remarks>
		public object SyncRoot { get { return source.SyncRoot; } }

		/// <summary>Copies the <see cref="ReadOnlyDictionary"/> elements to a one-dimensional
		/// <see cref="Array"/> instance at the specified index.</summary>
		/// <exception cref="ArgumentNullException">array is a null reference (Nothing in Visual Basic).</exception>
		/// <exception cref="ArgumentOutOfRangeException">arrayIndex is less than zero.</exception>
		/// <exception cref="ArgumentException">array is multidimensional.
		/// <para>-or-</para>
		/// <para>arrayIndex is equal to or greater than the length of array.</para>
		/// <para>-or-</para>
		/// <para>The number of elements in the source Hashtable is greater than the available space
		/// from arrayIndex to the end of the destination array.</para></exception>
		/// <exception cref="InvalidCastException">The type of the source <see cref="ReadOnlyDictionary"/>
		/// cannot be cast automatically to the type of the destination array.</exception>
		/// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements
		/// copied from <see cref="ReadOnlyDictionary"/>. The Array must have zero-based indexing.</param>
		/// <param name="index">The zero-based index in array at which copying begins.</param>
		public void CopyTo(Array array, int index) {
			source.CopyTo(array, index);
		}

		/// <summary>Получить экземпляр <see cref="ReadOnlyDictionary"/>, который не содержит в себе элементов.</summary>
		/// <remarks>Класс <see cref="ReadOnlyDictionary"/> кеширует пустой экземпляр, поэтому использование данного
		/// свойства предпочтительнее чем явное создание <see cref="ReadOnlyDictionary"/> с пустым словарем в качестве
		/// исходного.</remarks>
		/// <value><see cref="ReadOnlyDictionary"/>, который не содержит элементов.</value>
		public static ReadOnlyDictionary EmptyDictionary {
			get {
				if (emptyDictionary == null)
					emptyDictionary = new ReadOnlyDictionary(null);
				return emptyDictionary;
			}
		}
	}


	///<summary>Коллекция, образуемая как объединение нескольких коллекций (при этом сами коллекции остаются 
	/// неизменными.</summary>
	/// <remarks><see cref="JointCollection"/> хранит в себе ссылки на все объединяемые коллекции, так что их изменения 
	/// тут же отражаются на изменении объединения.</remarks>
	[Serializable]
	public class JointCollection<T> : IList<T> {
		protected IList<T>[] InnerLists;

		public JointCollection(params IList<T>[] clist) {
			if (clist == null) throw new ArgumentNullException("clist");
			InnerLists = clist;
		}

		#region ICollection<T> Members

		[Obsolete("JointCollection does not allow to add items", true)]
		public virtual void Add(T item) {
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual void Clear() {
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual bool Contains(T item) {
			throw new Exception("The method or operation is not implemented.");
		}

		public void CopyTo(T[] array, int index) {
			if (array == null) throw new ArgumentNullException("array");
			if (index < 0) throw new ArgumentOutOfRangeException("index");
			if (array.Rank > 1 || index + this.Count > array.Length) throw new ArgumentException();
			foreach (object item in this)
				array.SetValue(item, index++);
		}

		public int Count {
			get {
				int cnt = 0;
				foreach (ICollection<T> c in InnerLists)
					if (c != null) cnt += c.Count;
				return cnt;
			}
		}

		public bool IsReadOnly {
			get { return true; }
		}

		public virtual bool Remove(T item) {
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator() {
			return new MultiCollectionEnumerator<T>(InnerLists);
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		#region IList<T> Members

		public int IndexOf(T item) {
			throw new Exception("The method or operation is not implemented.");
		}

		public void Insert(int index, T item) {
			throw new Exception("The method or operation is not implemented.");
		}

		public void RemoveAt(int index) {
			throw new Exception("The method or operation is not implemented.");
		}

		public T this[int index] {
			get {
				int count = 0;
				for (int i = 0; i < InnerLists.Length; i++) {
					IList<T> col = InnerLists[i];
					if (col != null) {
						count += col.Count;
						if (index < count)
							return col[index - (count - col.Count)];
					}
				}

				return default(T);
			}
			set {
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion
	}

	///<summary>Коллекция, образуемая как объединение нескольких коллекций (при этом сами коллекции остаются 
	/// неизменными.</summary>
	/// <remarks><see cref="JointCollection"/> хранит в себе ссылки на все объединяемые коллекции, так что их изменения 
	/// тут же отражаются на изменении бъединения.</remarks>
	[Serializable]
	public class JointCollection : IList {
		ICollection[] m_lists;

		public JointCollection(params ICollection[] clist) {
			if (clist == null) throw new ArgumentNullException("clist");
			m_lists = clist;
		}

		#region ICollection Members

		[Obsolete("JointCollection does not allow to add items", true)]
		public void Add(ICollection item) {
			throw new Exception("The method or operation is not implemented.");
		}

		public void Clear() {
			throw new Exception("The method or operation is not implemented.");
		}

		public bool Contains(ICollection item) {
			throw new Exception("The method or operation is not implemented.");
		}

		public void CopyTo(Array array, int index) {
			if (array == null) throw new ArgumentNullException("array");
			if (index < 0) throw new ArgumentOutOfRangeException("index");
			if (array.Rank > 1 || index + this.Count > array.Length) throw new ArgumentException();
			foreach (object item in this)
				array.SetValue(item, index++);
		}

		public int Count {
			get {
				int cnt = 0;
				foreach (ICollection c in m_lists)
					if (c != null) cnt += c.Count;
				return cnt;
			}
		}

		public bool IsSynchronized { get { throw new Exception("The method or operation is not implemented.");  } }

		public object SyncRoot { get { throw new Exception("The method or operation is not implemented."); } }

		public bool IsReadOnly {
			get { return true; }
		}

		public bool Remove(ICollection item) {
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion

		#region IEnumerable Members

		public IEnumerator GetEnumerator() {
			return new MultiCollectionEnumerator(m_lists);
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		#region IList Members
		
		public bool IsFixedSize {
			get { throw new Exception("The method or operation is not implemented."); }
		}
		[Obsolete("JointCollection does not allow to add items", true)]
		public int Add(object value) {
			throw new Exception("The method or operation is not implemented.");
		}

		public bool Contains(object value) {
			throw new Exception("The method or operation is not implemented.");
		}

		public int IndexOf(object value) {
			throw new Exception("The method or operation is not implemented.");
		}

		public void Insert(int index, object value) {
			throw new Exception("The method or operation is not implemented.");
		}

		public void Remove(object value) {
			throw new Exception("The method or operation is not implemented.");
		}

		public int IndexOf(ICollection item) {
			throw new Exception("The method or operation is not implemented.");
		}

		public void Insert(int index, ICollection item) {
			throw new Exception("The method or operation is not implemented.");
		}

		public void RemoveAt(int index) {
			throw new Exception("The method or operation is not implemented.");
		}

		public object this[int index] {
			get {
/*				int curColCount = 0;
				for (int i = 0; i < m_lists.Length; i++) {
					ICollection curCollection = m_lists[i];

					curColCount += curCollection.Count;
					if (index > curColCount)
						continue;

					int indexInCurrColl = curCollection.Count - curColCount + index;
					int j = 0;
					foreach (object item in curCollection) {
						if (j == indexInCurrColl)
							return item;
						j++;
					}

					break;*/
					//return curCollection[curCollection.Count - curColCount + index];
				throw new Exception("The method or operation is not implemented.");
				}				
			set {
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion
	}



	public class MultiCollectionEnumerator<T> : IEnumerator<T> {
		ICollection<T>[] clist;
		int currentCollection;
		IEnumerator<T> currentEnumerator;

		public MultiCollectionEnumerator(params ICollection<T>[] clist) {
			if (clist == null) throw new ArgumentNullException("clist");
			this.clist = clist;
			Reset();
		}

		public T Current {
			get { return (currentEnumerator == null) ? default(T) : currentEnumerator.Current; }
		}

		public bool MoveNext() {
			bool result = (currentEnumerator == null) ? Move2NextCollection() : currentEnumerator.MoveNext();
			return result ? result : Move2NextCollection();
		}

		public void Reset() {
			currentCollection = 0;
			currentEnumerator = null;
		}

		protected bool Move2NextCollection() {
			ICollection<T> c = null;
			while (currentCollection < clist.Length && (c == null || c.Count == 0)) {
				c = clist[currentCollection];
				currentCollection++;
			}
			if (c == null) return false;
			currentEnumerator = c.GetEnumerator();
			currentEnumerator.Reset();
			return currentEnumerator.MoveNext();
		}

		#region IDisposable Members

		public void Dispose() {
		}

		#endregion

		#region IEnumerator Members

		object IEnumerator.Current {
			get { return Current; }
		}

		#endregion
	}

	public class MultiCollectionEnumerator : IEnumerator {
		ICollection[] clist;
		int currentCollection;
		IEnumerator currentEnumerator;

		public MultiCollectionEnumerator(params ICollection[] clist) {
			if (clist == null) throw new ArgumentNullException("clist");
			this.clist = clist;
			Reset();
		}

		public object Current {
			get { return (currentEnumerator == null) ? null : currentEnumerator.Current; }
		}

		public bool MoveNext() {
			bool result = (currentEnumerator == null) ? Move2NextCollection() : currentEnumerator.MoveNext();
			return result ? result : Move2NextCollection();
		}

		public void Reset() {
			currentCollection = 0;
			currentEnumerator = null;
		}

		protected bool Move2NextCollection() {
			ICollection c = null;
			while (currentCollection < clist.Length && (c == null || c.Count == 0)) {				
				c = clist[currentCollection];
				currentCollection++;
			}
			if (c == null) return false;
			currentEnumerator = c.GetEnumerator();
			currentEnumerator.Reset();
			return currentEnumerator.MoveNext();
		}

		#region IDisposable Members

		public void Dispose() {
		}

		#endregion

		#region IEnumerator Members

		object IEnumerator.Current {
			get { return Current; }
		}

		#endregion
	}

}

