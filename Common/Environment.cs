// $Id: Environment.cs 129 2006-04-06 12:00:46Z pilya $

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Security;

namespace Front {

	///<summary>Предоставляет информацию о текущем окружении.</summary>
	public interface IEnvironment : IDictionary, ICloneable {
		/// <summary>Создает новый экземпляр <see cref="IEnvironment"/>.</summary>
		/// <remarks>Новый экземпляр имеет тот же тип, что и текущий или совместимый с ним
		/// и содержит ссылку на текущий объект, как на родительский.</remarks>
		/// <seealso cref="Clone"/>
		IEnvironment Fork();
		/// <summary>Создает новый экземпляр <see cref="IEnvironment"/>.</summary>
		/// <remarks>Новый экземпляр имеет тот же тип, что и текущий или совместимый с ним
		/// и не содержит ссылок на родительский экземпляр. Все значения копируются в новый
		/// <see cref="IEnvironment"/>. Для ссылочных типов копируются только ссылки.</remarks>
		/// <seealso cref="Fork"/>
		IEnvironment Clone(params string[] names);
	}

	///<summary>Перечисляет все переменные окружения в <see cref="IEnvironment"/>.</summary>
	public class EnvironmentEnumerator : IDictionaryEnumerator {
		IEnvironment env;
		IEnumerator keysEnumerator;

		/// <summary>Initializes a new instance of the <see cref="EnvironmentEnumerator"/> class.</summary>
		/// <param name="e"><see cref="IEnumerator"/>, переменные которого надо перечислить.</param>
		public EnvironmentEnumerator(IEnvironment e) {
			if (e == null) throw new ArgumentNullException("e");
			env = e;
			Reset();
		}

		/// <summary>Получить текущий элемент перечисления.</summary>
		/// <value>Структура <see cref="DictionaryEntry"/>, содержит имя текущий переменной и ее значение.</value>
		public DictionaryEntry Entry {
			get {
				return new DictionaryEntry(this.Key, this.Value);
			}
		}

		object IEnumerator.Current {
			get { return this.Entry; }
		}

		/// <summary>Получить текущую переменную в перечислении.</summary>
		/// <value>Имя текущей переменной.</value>
		public string Key { get { return (string)keysEnumerator.Current; } }
		object IDictionaryEnumerator.Key { get { return this.Key; } }

		/// <summary>Получить значение текущей переменной в перечислении.</summary>
		/// <value>Значение текущей переменной.</value>
		public object Value { get { return env[this.Key]; } }

		/// <summary>Advances the enumerator to the next element of the collection.</summary>
		/// <returns>true if the enumerator was successfully advanced to the next element;
		/// false if the enumerator has passed the end of the collection.</returns>
		/// <remarks><para>After an enumerator is created or after a call to Reset, an enumerator
		/// is positioned before the first element of the collection, and the first call to
		/// MoveNext moves the enumerator over the first element of the collection.</para>
		/// <para>After the end of the collection is passed, subsequent calls to MoveNext return
		/// false until Reset is called.</para>
		/// <para>An enumerator remains valid as long as the collection remains unchanged. If
		/// changes are made to the collection, such as adding, modifying or deleting elements,
		/// the enumerator is irrecoverably invalidated and the next call to MoveNext or Reset throws
		/// an <see cref="InvalidOperationException"/>.</para>
		/// </remarks>
		/// <exception cref="InvalidOperationException">The collection was modified after the
		/// enumerator was created.</exception>
		public bool MoveNext() {
			return (keysEnumerator != null || keysEnumerator.MoveNext());
		}

		/// <summary>Sets the enumerator to its initial position, which is before the first
		/// element in the collection.</summary>
		/// <exception cref="InvalidOperationException">The collection was modified after the
		/// enumerator was created.</exception>
		/// <remarks><para>An enumerator remains valid as long as the collection remains unchanged.
		/// If changes are made to the collection, such as adding, modifying or deleting elements,
		/// the enumerator is irrecoverably invalidated and the next call to MoveNext or Reset throws
		/// an <see cref="InvalidOperationException"/>.</para>
		/// <b>Notes to Implementers:</b>
		/// <para>All calls to <c>Reset</c> must result in the same state for the enumerator. The preferred
		/// implementation is to move the enumerator to the beginning of the collection, before the
		/// first element. This invalidates the enumerator if the collection has been modified since
		/// the enumerator was created, which is consistent with <see cref="MoveNext"/>
		/// and <see cref="Entry"/>.</para></remarks>
		public void Reset() {
			keysEnumerator = env.Keys.GetEnumerator();
		}
	}

	/// <summary>Базовая реализация <see cref="IEnvironment"/>.</summary>
	public abstract class EnvironmentBase : MarshalByRefObject, IEnvironment {
		IEnvironment parent;

		/// <summary>Initialize new EnvironmentBase instance.</summary>
		protected EnvironmentBase() {
			parent = null;
		}

		/// <summary>Initialize new EnvironmentBase instance.</summary>
		/// <param name="parent">Базовый IEnvironment.</param>
		/// <remarks>Если значения нет в самом текущем экземпляре класса, то поиск продолжится в базовом.</remarks>
		protected EnvironmentBase(IEnvironment parent) {
			this.parent = parent;
		}
		/// <summary>Gets a value indicating whether the <see cref="IEnvironment"/> has a fixed size.</summary>
		/// <value><c>true</c> if the <see cref="IEnvironment"/> has a fixed size; otherwise, <c>false</c>.</value>
		/// <remarks>A collection with a fixed size does not allow the addition or removal of elements
		/// after the collection is created, but it allows the modification of existing elements.
		/// <para> A collection with a fixed size is simply a collection with a wrapper that prevents
		/// adding and removing elements; therefore, if changes are made to the underlying collection,
		/// including the addition or removal of elements, the fixed-size collection reflects those changes.
		/// </para></remarks>
		public virtual bool IsFixedSize { get { return false; } }

		/// <summary>Gets a value indicating whether the <see cref="IEnvironment"/> is read-only.</summary>
		/// <value><c>false</c>.</value>
		/// <remarks>EnvironmentBase does not support read-only mode.</remarks>
		public virtual bool IsReadOnly { get { return false; } }

		/// <summary>Gets or sets the element with the specified key.</summary>
		/// <param name="key">The name of the environment variable to get or set.</param>
		/// <value>The value of the variable with the specified key.</value>
		/// <exception cref="ArgumentNullException">key is a null reference (<c>Nothing</c> in Visual Basic).</exception>
		/// <exception cref="NotSupportedException">The property is set and the IDictionary is read-only.
		/// <para>-or-</para>
		/// <para>The property is set, key does not exist in the collection, and the
		/// <see cref="IEnvironment"/> has a fixed size.</para>
		/// </exception>
		public object this[string key] {
			get {
				object v = GetOwnedValue(key);
				return (v == null && parent != null) ? parent[key] : v;
			}
			set {
				if (this.IsReadOnly) throw new NotSupportedException();
				SetOwnedValue(key, value);
			}
		}

		/// <summary>Gets or sets the element with the specified key.</summary>
		/// <value>The value of the variable with the specified key.</value>
		/// <remarks>Please, use overloaded version with <b>string</b> parameter.</remarks>
		/// <exception cref="ArgumentNullException">key is a null reference (<c>Nothing</c> in Visual Basic).</exception>
		/// <exception cref="NotSupportedException">The property is set and the IDictionary is read-only.
		/// <para>-or-</para>
		/// <para>The property is set, key does not exist in the collection, and the
		/// <see cref="IDictionary"/> has a fixed size.</para>
		/// </exception>
		object IDictionary.this[object key] {
			get {
				return this[(string)key];
			}
			set {
				this[(string)key] = value;
			}
		}
		
		/// <summary>Gets an <see cref="ICollection"/> containing the keys of the <see cref="IEnvironment"/>.</summary>
		/// <value>An <see cref="ICollection"/> containing the keys of the <see cref="IEnvironment"/>.</value>
		public ICollection Keys {
			get {
				StringCollection keyList = new StringCollection();
				// read parent keys
				if (parent != null) 
					foreach (string key in parent.Keys)
						keyList.Add(key);
				// add owned keys
				IDictionary owned = GetOwnedDictionary();
				foreach (string key in owned.Keys)
					if (!keyList.Contains(key))
						keyList.Add(key);
				return keyList;
			}
		}

		/// <summary>Gets an <see cref="ICollection"/> containing the values in the <see cref="IEnvironment"/>.</summary>
		/// <value>An <see cref="ICollection"/> containing the values of the <see cref="IEnvironment"/>.</value>
		public ICollection Values {
			get {
				ArrayList valueList = new ArrayList();
				foreach (string key in this.Keys)
					valueList.Add(this[key]);
				return valueList;
			}
		}

		/// <summary>Adds an element with the provided name and value to the <see cref="IEnvironment"/>.</summary>
		/// <param name="name">Environment variable name.</param>
		/// <param name="value">Environment variable value.</param>
		/// <exception cref="ArgumentNullException"><c>name</c> is a null reference (<b>Nothing</b> in Visual Basic).</exception>
		/// <exception cref="ArgumentException">An variable with the same name already exists in the <see cref="IEnvironment"/>.</exception>
		/// <exception cref="NotSupportedException">The IDictionary is read-only. 
		/// <para>-or-</para>
		/// <para>The IDictionary has a fixed size.</para></exception>
		void IDictionary.Add(object name, object value) {
			this.Add((string)name, value);
		}

		/// <summary>Adds an element with the provided name and value to the <see cref="IEnvironment"/>.</summary>
		/// <param name="name">Environment variable name.</param>
		/// <param name="value">Environment variable value.</param>
		/// <exception cref="ArgumentNullException"><c>name</c> is a null reference (<b>Nothing</b> in Visual Basic).</exception>
		/// <exception cref="ArgumentException">An variable with the same name already exists in the <see cref="IEnvironment"/>.</exception>
		/// <exception cref="NotSupportedException">The IDictionary is read-only. 
		/// <para>-or-</para>
		/// <para>The IDictionary has a fixed size.</para></exception>
		public virtual void Add(string name, object value) {
			if (this.IsReadOnly || this.IsFixedSize)
				throw new ReadOnlyException();
			IDictionary owned = GetOwnedDictionary();
			owned.Add(name, value);
		}

		/// <summary>Removes all owned variables from the <see cref="IEnvironment"/>.</summary>
		/// <exception cref="NotSupportedException">The IDictionary is read-only.</exception>
		public virtual void Clear() {
			if (this.IsReadOnly) throw new NotSupportedException();
			IDictionary owned = GetOwnedDictionary();
			owned.Clear();
		}
		
		/// <summary>Determines whether the <see cref="IEnvironment"/> contains an variable with the specified name.</summary>
		/// <value>true if the <see cref="IEnvironment"/> or his parent contains an element with the name; otherwise, false.</value>
		/// <param name="name">The name of variable to locate in the <see cref="IEnvironment"/>.</param>
		/// <exception cref="ArgumentNullException">name is a null reference (Nothing in Visual Basic).</exception>
		public virtual bool Contains(string name) {
			IDictionary owned = GetOwnedDictionary();
			bool found = owned.Contains(name);
			return (!found && parent != null) ? parent.Contains(name) : found;
		}

		/// <summary>Determines whether the <see cref="IEnvironment"/> contains an variable with the specified name.</summary>
		/// <value>true if the <see cref="IEnvironment"/> or his parent contains an element with the name; otherwise, false.</value>
		/// <param name="name">The name of variable to locate in the <see cref="IEnvironment"/>.</param>
		/// <exception cref="ArgumentNullException">name is a null reference (Nothing in Visual Basic).</exception>
		/// <summary><see cref="IDictionary"/> implementation</summary>
		bool IDictionary.Contains(object name) {
			return Contains((string)name);
		}

		/// <summary>Returns an <see cref="IDictionaryEnumerator"/> for the <see cref="IEnvironment"/>.</summary>
		/// <value>An <see cref="IDictionaryEnumerator"/> for the <see cref="IEnvironment"/>.</value>
		/// <seealso cref="IDictionary.GetEnumerator"/>
		public virtual IDictionaryEnumerator GetEnumerator() {
			return new EnvironmentEnumerator(this);
		}

		/// <summary>Returns an <see cref="IDictionaryEnumerator"/> for the <see cref="IEnvironment"/>.</summary>
		/// <value>An <see cref="IDictionaryEnumerator"/> for the <see cref="IEnvironment"/>.</value>
		/// <seealso cref="IDictionary.GetEnumerator"/>
		/// <summary><see cref="IDictionary"/> implementation</summary>
		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		/// <summary>Removes the variable with the specified key from the <see cref="IEnvironment"/>.</summary>
		/// <param name="name">The name of the variable to remove.</param>
		/// <exception cref="ArgumentNullException">key is a null reference (Nothing in Visual Basic).</exception>
		/// <exception cref="NotSupportedException">The IDictionary is read-only. 
		/// <para>-or-</para>
		/// <para>The IDictionary has a fixed size.</para></exception>
		public virtual void Remove(string name) {
			if (this.IsReadOnly || this.IsFixedSize)
				throw new ReadOnlyException();
			IDictionary owned = GetOwnedDictionary();
			owned.Remove(name);
		}

		/// <summary>Removes the variable with the specified key from the <see cref="IEnvironment"/>.</summary>
		/// <param name="name">The name of the variable to remove.</param>
		/// <exception cref="ArgumentNullException">key is a null reference (Nothing in Visual Basic).</exception>
		/// <exception cref="NotSupportedException">The IDictionary is read-only. 
		/// <para>-or-</para>
		/// <para>The IDictionary has a fixed size.</para></exception>
		/// <summary><see cref="IDictionary"/> implementation</summary>
		void IDictionary.Remove(object name) {
			this.Remove((string)name);
		}

		/// <summary>Получить значение, которое определено непосредственно в этом классе.</summary>
		/// <remarks>Поиск в базовом окружении не выполняется.</remarks>
		protected virtual object GetOwnedValue(string key) {
			IDictionary owned = GetOwnedDictionary();
			return owned[key];
		}
		
		/// <summary>Установить значение.</summary>
		protected virtual void SetOwnedValue(string key, object value) {
			IDictionary owned = GetOwnedDictionary();
			if (!owned.Contains(key) && this.IsFixedSize)
				throw new NotSupportedException();
				owned[key] = value;
		}

		/// <summary>Получить <see cref="IDictionary"/>, в котором хранятся значения, определенные
		/// в этом окружении.</summary>
		protected abstract IDictionary GetOwnedDictionary();

		/// <summary>Gets the number of elements contained in the <see cref="IEnvironment"/>.</summary>
		/// <value>The number of elements contained in the <see cref="IEnvironment"/>.</value>
		public int Count { get { return this.Keys.Count; } }

		/// <summary>Gets a value indicating whether access to the ICollection is synchronized (thread-safe).</summary>
		/// <value>true if access to the ICollection is synchronized (thread-safe); otherwise, false.</value>
		/// <seealso cref="SyncRoot"/>
		public virtual bool IsSynchronized { get { return false; } }

		/// <summary>Gets an object that can be used to synchronize access to the <see cref="IEnvironment"/>.</summary>
		/// <remarks>Only access to owned environment variables will be synchronized.</remarks>
		public virtual object SyncRoot { get { return GetOwnedDictionary().SyncRoot; } }

		/// <summary>Copies the elements of the <see cref="IEnvironment"/> to an Array,
		/// starting at a particular Array index.</summary>
		/// <param name="array">The one-dimensional Array that is the destination of the
		/// elements copied from ICollection. The Array must have zero-based indexing.</param>
		/// <param name="index">The zero-based index in array at which copying begins.</param>
		/// <exception cref="ArgumentNullException">array is a null reference (Nothing in Visual Basic).</exception>
		/// <exception cref="ArgumentOutOfRangeException">index is less than zero.</exception>
		/// <exception cref="ArgumentException">array is multidimensional. 
		/// <para>-or-</para>
		/// <para>index is equal to or greater than the length of array.</para>
		/// <para>-or-</para>
		/// <para>The number of elements in the source ICollection is greater than the available
		/// space from index to the end of the destination array.</para></exception>
		/// <exception cref="InvalidCastException">The type of the source ICollection cannot be cast
		/// automatically to the type of the destination array.</exception>
		public virtual void CopyTo(Array array, int index) {
			if (array == null) throw new ArgumentNullException("array");
			if (index < 0) throw new ArgumentOutOfRangeException("index");
			ICollection keys;
			if (array.Rank > 1 || index > array.Length
					|| (keys = this.Keys).Count + index > array.Length) throw new ArgumentException("array");
			foreach (DictionaryEntry entry in this)
				array.SetValue(entry, index++);
		}

		/// <summary>Создает новый экземпляр <see cref="IEnvironment"/>.</summary>
		/// <remarks>Новый экземпляр имеет тот же тип, что и текущий или совместимый с ним
		/// и содержит ссылку на текущий объект, как на родительский.</remarks>
		/// <seealso cref="Clone"/>
		public virtual IEnvironment Fork() {
				return (IEnvironment)new Environment(this);
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		/// <value>A new object that is a copy of this instance.</value>
		object ICloneable.Clone() {
			return Clone();			
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		/// <value>A new object that is a copy of this instance.</value>
		public virtual IEnvironment Clone(params string[] names) {
			Environment env = new Environment();
			foreach (string key in names) {
				object value = this[key];
				if (value != null)
					env[key] = value;
			}
			return env;
		}
	}

	/// <summary> Предоставляет информацию о текущем окружении. Представляет собой коллекцию
	/// пар ключ-значение.</summary>
	/// <remarks>Окружение имеет стековую структуру. При создании нового
	/// окружения методом <see cref="IEnvironment.Fork"/> текущий экземпляр <see cref="IEnvironment"/> передается
	/// ему в качестве родительского. При поиске переменной в окружении, сначала выполняется поиск в
	/// собственных переменных, а потом в переменных родительского окружения. То есть, дочернее окружение
	/// имеет возможность переопределить значение переменной, определенной в родительском.</remarks>
	public class Environment : EnvironmentBase {
		const string defaultEnvironmentName = "Front.Environment.DefaultInstance";
		IDictionary owned;

		/// <summary> Инициализирует новый экземпляр <see cref="Front.Environment"/>, не содержащий переменных
		/// и без родительского окружения.</summary>
		/// <remarks>Не рекоммендуется создавать новый (корневой) <see cref="IEnvironment"/> с использованием
		/// конструктора. Рассмотрите вариант использования статического свойства
		/// <see cref="Environment.DefaultEnvironment"/> класса <see cref="Environment"/>.</remarks>
		/// <seealso cref="IEnvironment.Fork"/>
		public Environment():base() {
			owned = new HybridDictionary();
		}

		/// <summary> Инициализирует новый экземпляр <see cref="Front.Environment"/>, не содержащий переменных.</summary>
		/// <param name="parent">Родительское окружение.</param>
		/// <remarks>Не рекоммендуется использование этого конструктора напрямую.
		/// Рассмотрите вариант использования метода <see cref="IEnvironment.Fork"/>.</remarks>
		/// <seealso cref="IEnvironment.Fork"/>
		public Environment(IEnvironment parent):base(parent) {}

		/// <summary> Инициализирует новый экземпляр <see cref="Front.Environment"/>.</summary>
		/// <param name="env">Набор пар ключ-значение, которые будут автоматически добавлены в новое окружение.</param>
		/// <remarks><see cref="Front.Environment"/> хранит внутри себя набор пар ключ-значение, используя
		/// одну из реализация интерфейса <see cref="IDictionary"/>. В данном случае, конструктор не копирует
		/// переменные из переданного списка в свой собственный. Переданный список используется окружением для
		/// хранения всех переменных в дальшейшем и параллельное изменение этого списка влияет на содержание окружения.</remarks>
		/// <seealso cref="IEnvironment.Fork"/>
		public Environment(IDictionary env):base() { this.owned = env; }

		/// <summary> Инициализирует новый экземпляр <see cref="Front.Environment"/>.</summary>
		/// <param name="items">Массив, в котором все четные элементы являются ключами, а следующие за нами нечетные
		/// - значениями. Этот набор пар ключ-значение, будут автоматически добавлены в новое окружение.</param>
		/// <seealso cref="IEnvironment.Fork"/>
		/// <seealso cref="MakeDictionary"/>
		public Environment(params object[] items) : this( MakeDictionary(items) ) {}

		/// <summary> Получить <see cref="IDictionary"/>, в котором хранятся значения, определенные
		/// в этом окружении.</summary>
		protected override IDictionary GetOwnedDictionary() {
			return owned;
		}

		/// <summary>Возвращает <see cref="IEnvironment"/> по-умолчанию.</summary>
		/// <remarks>При обращении к этому свойству впервые, создается экземпляр класса <see cref="Front.Environment"/> и
		/// помещается в статическую переменную. Это приводит к тому, что все последующие вызовы будут возвращать
		/// тот же экземпляр <see cref="IEnvironment"/>. Следует учитывать, что статические переменные в разных
		/// <see cref="System.AppDomain"/> могут иметь разное значение. Соответственно, в разных <see cref="System.AppDomain"/>
		/// окружение по-умолчанию будет разным.
		/// <para>Предполагается использование иерархии окружений таким образом, что корневое окружение создается
		/// вызовом метода <see cref="Environment.DefaultEnvironment"/>, а все остальные путем вызова <see cref="IEnvironment.Fork"/> у
		/// умолчательного окружения, или у окружения принадлежащего дереву "детей" умолчательного окружения.</para></remarks>
		/// <value>Умолчательний <see cref="IEnvironment"/>.</value>
		public static IEnvironment DefaultEnvironment {
			get {
				IEnvironment e;
				e = AppDomain.CurrentDomain.GetData(defaultEnvironmentName) as IEnvironment;
				if (e == null) {
					e = new Environment();
					try { 
						SetDefault(e);
					} catch (SecurityException) {
						// catch SecurityException for partially-trusted code, ignore it
					}
				}
				
				// repeat reading for thread-safe
				e = AppDomain.CurrentDomain.GetData(defaultEnvironmentName) as IEnvironment;
				return e;
			}
		}

		/// <summary>Переустановить умолчательное окружение.</summary>
		/// <param name="e">Окружение, которое будет умолчательным.</param>
		/// <remarks>Обычно, приложению нет необходимости вызывать метод <see cref="SetDefault"/>.</remarks>
		public static void SetDefault(IEnvironment e) {
			Front.Environment.SetDefault(AppDomain.CurrentDomain, e);
		}

		/// <summary>Переустановить умолчательное окружение для другого <see cref="System.AppDomain"/>.</summary>
		/// <param name="e">Окружение, которое будет умолчательным.</param>
		/// <param name="domain"><see cref="AppDomain"/> для которого надо изменить окружение.</param>
		/// <remarks>Обычно, приложению нет необходимости вызывать метод <see cref="SetDefault"/>.</remarks>
		public static void SetDefault(AppDomain domain, IEnvironment e) {
			lock (typeof(Environment)) {
				domain.SetData(defaultEnvironmentName, e);
			}
		}

		/// <summary> Получить <see cref="IDictionary"/>, содержащий элементы из массива <c>items</c>.</summary>
		/// <param name="items">Массив, в котором все четные элементы являются ключами, а следующие за нами нечетные
		/// - значениями. Этот набор пар ключ-значение, будут автоматически добавлены в новое окружение.</param>
		/// <value><see cref="IDictionary"/>, содержащий элементы из массива <c>items</c>.</value>
		public static IDictionary MakeDictionary(params object[] items) {
			if (items == null || items.Length <2) return null;
			ListDictionary res = new ListDictionary();
			for(int i =0; i+1 < items.Length; i+=2) {
				string key = items[i] as string;
				if (key == null)
					res[key] = items[i+1];
			}
			return res;
		}
	}
}

