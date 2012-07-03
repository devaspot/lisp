// $Id: Cache.cs 2368 2006-09-14 12:32:24Z kostya $

//#define PUBLISH_PERFORMANCE_COUNTERS

using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;

using Front.Diagnostics;

namespace Front {

	/// <summary>Объкты, содержащие в себе <see cref="Cache"/> должны поддерживать этот интерфейс.</summary>
	public interface ICacheContainer {
		/// <summary>Очистить внутренний кеш объекта, реализующего <see cref="ICacheContainer"/>.</summary>
		void ClearCache();
		// TODO DF0010: может еще что-то про кеш и политику кеширования спросить?
		// TODO DF0078: дописать события до и после!
	}

	
	
	/// <summary>
	/// Класс <c>Cache</c> предназначен для временного хранения данных, повторное получение
	/// которых связанно с затратами, заведомо превышающими стоимость кеширования.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <c>Cache</c> не стоит использовать для хранения данных, которые нельзя получить повторно,
	/// так как в любой момент времени <c>Cache</c> может очистить свое содержимое полностью или
	/// частично.
	/// </para><para>
	/// <c>Cache</c> поддерживает несколько алгоритмов очистки данных: устаревание данных и очистка
	/// при удалении сборщиком мусора. Следует помнить, что все ссылки, которые <c>Cache</c> хранит
	/// для своих данных, это <see cref="WeakReference"/>, а потому кеширование данных не мешает их удалению
	/// сборщиком мусора.
	/// </para>
	/// </remarks>
	/// <threadsafety static="true" instance="true"/>
	/// <seealso cref="IEnvironment"/>
	// TODO: DF0011: сделать его ICacheContainer (done.)
	public class Cache : MarshalByRefObject, IEnumerable, IDisposable, ICacheContainer {
		///<summary> Константа, указание которой параметром в методе <see cref="Add" /> означает,
		/// что на добавляемое значение не действует механизм абсолютного устаревания.</summary>
		public static readonly DateTime NoAbsoluteExpiration = DateTime.MaxValue;
		///<summary> Константа, указание которой параметром в методе <see cref="Add" /> означает,
		/// что на добавляемое значение не действует механизм устаревания по-обращению.</summary>
		public static readonly TimeSpan NoSlidingExpiration = TimeSpan.Zero;

		public static readonly Front.Diagnostics.Log Log 
			= new Front.Diagnostics.Log(new TraceSwitch("Front.Cache", "Front.Cache", "Error"));
		
		/* -------------------------------------------------------------------------------- */
		
		static int			instanceNumber = 0;
		TimeSpan			defaultAbsolutePeriod = TimeSpan.FromMinutes(60);
		TimeSpan			defaultSlidingPeriod = TimeSpan.FromMinutes(15);

		ReaderWriterLock	rwlock = new ReaderWriterLock();
		object				cacheName;	// object to be compatible with Interlocked.Exchange(..)
		int					cleanPeriod;
		int					maxEntries;
		Hashtable			items = new Hashtable();
		Timer				timer;
		int					inOptimization = 0;
		
#if PUBLISH_PERFORMANCE_COUNTERS
		static Cache() {
			if (!PerformanceCounterCategory.Exists("Front Cache")) {
				CounterCreationData hits = new CounterCreationData();
				hits.CounterName = "Hit Count";
				hits.CounterType = PerformanceCounterType.NumberOfItems32;
				CounterCreationData cacheSize = new CounterCreationData();
				cacheSize.CounterName = "Cache Size";
				cacheSize.CounterType = PerformanceCounterType.NumberOfItems32;
				CounterCreationDataCollection ccds = new CounterCreationDataCollection();
				ccds.Add(hits);
				ccds.Add(cacheSize);
				PerformanceCounterCategory.Create("Front Cache", "Front Cache counters", ccds); 
			}
		}
#endif

		/// <summary>Создает экземпляр <see cref="Cache"/> с периодом оптимизации 60 секунд и
		/// без ограничения максимального количества элементов. </summary>
		public Cache():this(60) {}
		
		/// <summary>Создает экземпляр <see cref="Cache"/> с указанным периодом оптимизации и
		/// без ограничения максимального количества элементов. </summary>
		/// <remarks>Если указанное значение
		/// меньше 5 секунд, период оптимизации принимается равным 5 секундам.</remarks>
		/// <param>Период оптимизации <c>Cache</c>. Указывается в секундах.</param>
		public Cache(int cleanPeriod):this(cleanPeriod, 0) { }

		/// <summary>Создает экземпляр <see cref="Cache"/> с указанным периодом оптимизации и
		/// максимальным количеством элементов. </summary>
		/// <remarks>Если указанное значение
		/// меньше 5 секунд, период оптимизации принимается равным 5 секундам.</remarks>
		/// <param name="cleanPeriod">Период оптимизации <c>Cache</c>. Указывается в секундах.</param>
		/// <param name="maxEntries">Максимальное количество элементов. При значении 0, ограничения нет.</param>
		public Cache(int cleanPeriod, int maxEntries):this(null, cleanPeriod, maxEntries) { }

		/// <summary>Создает именованный экземпляр <see cref="Cache"/> с указанным периодом оптимизации и
		/// максимальным количеством элементов. </summary>
		/// <remarks>Если указанное значение
		/// меньше 5 секунд, период оптимизации принимается равным 5 секундам.</remarks>
		/// <param name="name">Имя для создаваемого экземпляра <c>Cache</c>.</param>
		/// <param name="cleanPeriod">Период оптимизации <c>Cache</c>. Указывается в секундах.</param>
		/// <param name="maxEntries">Максимальное количество элементов. При значении 0, ограничения нет.</param>
		public Cache(string name, int cleanPeriod, int maxEntries) {
			this.cleanPeriod = (cleanPeriod < 5) ? 5 : cleanPeriod;
			this.maxEntries = maxEntries;
			try {
				this.Name = (name != null && name.Length > 0)
					? name
					: Process.GetCurrentProcess().ProcessName + Interlocked.Increment(ref instanceNumber).ToString();
			} catch (InvalidOperationException ex) {
				// catch this case: http://msdn.microsoft.com/netframework/programming/bcl/faq/SystemDiagnosticsProcessFAQ.aspx#Question2
				this.Name = "buggedProcess" + Interlocked.Increment(ref instanceNumber).ToString();
			}
			Log.Info(RM.GetString("LogCacheCreated"), Name, cleanPeriod, maxEntries);
		}

		/// <summary>Освобождает неуправляемые ресурсы, используемые <see cref="Cache"/>.</summary>
		~Cache() {
			Dispose(false);
		}

		/// <summary>Освобождает управляемые и неуправляемые ресурсы, используемые <see cref="Cache"/>.</summary>
		public void Dispose() { Dispose(true); }

		/// <summary>Освобождает управляемые и неуправляемые ресурсы, используемые <see cref="Cache"/>.</summary>
		/// <remarks>Этот метод вызывается из <see cref="IDisposable.Dispose"/> и из метода
		/// <see cref="Finalize"/> с параметром <c>disposing</c> установненным в <c>true</c> и <c>false</c>
		/// соответственно.</remarks>
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				rwlock.AcquireWriterLock(Timeout.Infinite);
				try {
					if (items != null) {
						GC.SuppressFinalize(this);
						items = null;
#if PUBLISH_PERFORMANCE_COUNTERS
						if (hitCounter != null) {
							hitCounter.RemoveInstance();
							cacheSize.RemoveInstance();
							hitCounter.Close();
							cacheSize.Close();
						}
#endif
					}
				} finally {
					rwlock.ReleaseWriterLock();
				}
			}
		}

		/// <summary>Проверяет есть ли в <see cref="Cache"/> элемент с указанным именим.</summary>
		/// <remarks>Следует помнить, что положительный ответ этого метода не гарантирует, что элемент
		/// просуществует в <c>Cache</c> еще какое-то время. Он может быть удален тут же.</remarks>
		/// <param name="code"> Имя искомого элемента.</param>
		/// <exception cref="ArgumentNullException">Параметр <c>code</c> равен <c>null</c>.</exception>
		/// <exception cref="ObjectDisposedException">Для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
		public bool Contains(string code) {
			if (code == null) throw new ArgumentNullException("code");
			rwlock.AcquireReaderLock(Timeout.Infinite);
			try {
#if PUBLISH_PERFORMANCE_COUNTERS
				hitCounter.Increment();
#endif
				if (items == null) throw new ObjectDisposedException(this.GetType().Name);
				return items.Contains(code);
			} finally {
				rwlock.ReleaseReaderLock();
			}
		}

		/// <summary>Добавляет в <see cref="Cache"/> элемент <c>value</c> с именем <c>code</c>.</summary>
		/// <remarks><para>Если элемент с таким именем уже существует он будет заменен.</para>
		/// <para>Время абсолютного устаревания указывает максимальный срок жизни элемента. При первой же
		/// оптимизации, произошедшей после этого срока объект будет удален.</para>
		/// <para>Время относительного устаревания указывает срок жизни элемента после последнего к нему обращения.
		/// Если срок после обращения к элементу больше <c>slidingExpiration</c>, элемент удаляется даже
		/// если время абсолютного устаревания еще не достигнуто.</para>
		/// <para>Установка параметра <c>absoluteExpiration</c> в значение <see cref="NoAbsoluteExpiration"/>,
		/// отключает механизм абсолютного устаревания для данного элемента.</para>
		/// <para>Установка параметра <c>slidingExpiration</c> в значение <see cref="NoSlidingExpiration"/>,
		/// отключает мезанизм относительного устаревания для данного элемента.</para>
		///</remarks>
		/// <param name="code">Имя нового элемента.</param>
		/// <param name="value">Значение нового элемента.</param>
		/// <param name="absoluteExpiration">Время абсолютного устаревания нового элемента.</param>
		/// <param name="slidingExpiration">Период относительного устаревания</param>
		/// <exception cref="ArgumentNullException">Параметр <c>code</c> равен <c>null</c>.</exception>
		/// <exception cref="ObjectDisposedException">Для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
		/// <seealso cref="this"/>
		/// <seealso cref="Get"/>
		/// <seealso cref="Remove"/>
		public virtual object Add(string code, object value,
				DateTime absoluteExpiration, TimeSpan slidingExpiration) {
			if (code == null) throw new ArgumentNullException("code");
			rwlock.AcquireWriterLock(Timeout.Infinite);
			try {
				if (items == null) throw new ObjectDisposedException(this.GetType().Name);
				if (maxEntries > 0 && items.Count >= maxEntries)
					RemoveOneEntry();
				items[code] = new CacheEntry(value, absoluteExpiration, slidingExpiration);
				Log.Info(RM.GetString("LogCacheAdded"), this.Name, code, value);
				if (timer == null)
					timer = new Timer(new TimerCallback(Optimize), null, cleanPeriod * 1000, cleanPeriod * 1000);
#if PUBLISH_PERFORMANCE_COUNTERS
				cacheSize.RawValue = this.Count;
#endif
			} finally {
				rwlock.ReleaseWriterLock();
			}
			return value;
		}

		/// <summary>Добавляет в <see cref="Cache"/> элемент <c>value</c> с именем <c>code</c>.</summary>
		/// <remarks><para>Если элемент с таким именем уже существует он будет заменен.</para>
		/// <para>Время абсолютного устаревания указывает максимальный срок жизни элемента. При первой же
		/// оптимизации, произошедшей после этого срока объект будет удален.</para>
		/// <para>Время относительного устаревания указывает срок жизни элемента после последнего к нему обращения.
		/// Если срок после обращения к элементу больше <c>slidingExpiration</c>, элемент удаляется даже
		/// если время абсолютного устаревания еще не достигнуто.</para>
		/// <para>Время абсолютного устаревания и относительно вычисляется с использованием свойств
		/// <see cref="DefaultAbsolutePeriod"/> и <see cref="DefaultSlidingPeriod"/> соответственно.</para>
		///</remarks>
		/// <param name="code">Имя нового элемента.</param>
		/// <param name="value">Значение нового элемента.</param>
		/// <exception cref="ArgumentNullException">Параметр <c>code</c> равен <c>null</c>.</exception>
		/// <exception cref="ObjectDisposedException">Для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
		/// <seealso cref="this"/>
		/// <seealso cref="Get"/>
		/// <seealso cref="Remove"/>
		public object Add(string code, object value) {
			return this.Add(code, value, DateTime.Now + defaultAbsolutePeriod, defaultSlidingPeriod);
		}

		void RemoveOneEntry() {
			Log.Verb(RM.GetString("LogCacheMaxExceed"), this.Name);
			rwlock.AcquireWriterLock(Timeout.Infinite);
			try {
				if (items == null) throw new ObjectDisposedException(this.GetType().Name);
				DateTime expireTime = DateTime.MaxValue;
				string next2Expire = null;
				foreach (string key in items.Keys) {
					CacheEntry entry = (CacheEntry)items[key];
					DateTime et = entry.ExpirationTime;
					if (et < expireTime) {
						expireTime = et;
						next2Expire = key;
					}
				}
				if (next2Expire != null) InternalRemove(next2Expire);
			} finally {
				rwlock.ReleaseWriterLock();
			}
		}

		/// <summary>Удаляет элемент с именем <c>code</c> из <see cref="Cache"/>.</summary>
		/// <remarks>Если элемент с таким именем не найден, удаление игнорируется.</remarks>
		/// <param name="code">Имя элемента.</param>
		/// <exception cref="ArgumentNullException">Параметр <c>code</c> равен <c>null</c>.</exception>
		/// <exception cref="ObjectDisposedException">Для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
		public virtual object Remove(string code) {
			if (code == null) throw new ArgumentNullException("code");
			rwlock.AcquireReaderLock(Timeout.Infinite);
			try {
				if (items == null) throw new ObjectDisposedException(this.GetType().Name);
				object o = items[code];
				if (o != null) {
					rwlock.UpgradeToWriterLock(Timeout.Infinite);
					InternalRemove(code);
					items.Remove(code);
#if PUBLISH_PERFORMANCE_COUNTERS
					cacheSize.RawValue = this.Count;
#endif
					return ((CacheEntry)o).Value;
				} else
					return null;
			} finally {
				rwlock.ReleaseReaderLock();
			}
		}

		void InternalRemove(string code) {
			Log.Info(RM.GetString("LogCacheRemove"), this.Name, code);
			items.Remove(code);
		}

		/// <summary>Читает или устанавлевает элемент с именем <c>code</c>.</summary>
		/// <remarks>Если искомый элемент не найден, возвращается значение <c>null</c>.
		/// При добавлении элемента используются значения <see cref="DefaultAbsolutePeriod"/>
		/// и <see cref="DefaultSlidingPeriod"/>.</remarks>
		/// <param name="code">Имя элемента.</param>
		/// <exception cref="ArgumentNullException">Параметр <c>code</c> равен <c>null</c>.</exception>
		/// <exception cref="ObjectDisposedException">Для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
		/// <seealso cref="Add"/>
		/// <seealso cref="Get"/>
		/// <seealso cref="Remove"/>
		public object this[string code] {
			get { return Get(code); }
			set { Add(code, value); }
		}
		
		/// <summary>Читает элемент с именем <c>code</c>.</summary>
		/// <remarks>Если искомый элемент не найден, возвращается значение <c>null</c>.</remarks>
		/// <param name="code">Имя элемента.</param>
		/// <exception cref="ArgumentNullException">Параметр <c>code</c> равен <c>null</c>.</exception>
		/// <exception cref="ObjectDisposedException">Для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
		/// <seealso cref="Add"/>
		/// <seealso cref="this"/>
		/// <seealso cref="Remove"/>
		public virtual object Get(string code) {
			if (code == null) throw new ArgumentNullException("code");
			object o;
			rwlock.AcquireReaderLock(Timeout.Infinite);
			try {
				if (items == null) throw new ObjectDisposedException(this.GetType().Name);
#if PUBLISH_PERFORMANCE_COUNTERS
				hitCounter.Increment();
#endif
				o = items[code];
			} finally {
				rwlock.ReleaseReaderLock();
			}
			return (o == null) ? null : ((CacheEntry)o).Value;
		}

		/// <summary>Очищает <see cref="Cache"/>.</summary>
		/// <remarks>Все элементы удаляются.</remarks>
		/// <exception cref="ObjectDisposedException">Для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
		/// <seealso cref="Remove"/>
		public virtual void Clear() {
			Log.Info(RM.GetString("LogCacheMaxExceed"), this.Name);
			rwlock.AcquireWriterLock(Timeout.Infinite);
			try {
				if (items == null) throw new ObjectDisposedException(this.GetType().Name);
				if (timer != null) {
					timer.Dispose();
					timer = null;
				}
				items.Clear();
#if PUBLISH_PERFORMANCE_COUNTERS
				cacheSize.RawValue = this.Count;
#endif
			} finally {
				rwlock.ReleaseWriterLock();
			}
		}

		public virtual void ClearCache() { this.Clear(); }
		
		/// <summary>Оптимизирует <see cref="Cache"/>.</summary>
		/// <remarks>Этот метод обычно вызывается самим <c>Cache</c> с периодичностью заданной
		/// в <see cref="CleanPeriod"/>. Он находит устаревшие элементы и удаляет их.</remarks>
		/// <exception cref="ObjectDisposedException">Для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
		/// <seealso cref="Remove"/>
		/// <seealso cref="Clear"/>
		protected virtual void Optimize(object state) {
			int optimizeActive = Interlocked.CompareExchange(ref inOptimization, 1, 0);
			if (optimizeActive == 0) try {
				Log.Info(RM.GetString("LogCacheOptimize"), this.Name);
				DateTime n = (state == null) ? DateTime.Now : (DateTime)state;
				string[] keys;
				rwlock.AcquireReaderLock(Timeout.Infinite);
				try {
					if (items == null) return;
					keys = new string[items.Keys.Count];
					items.Keys.CopyTo(keys, 0);
					foreach (string key in keys) {
						CacheEntry ce = (CacheEntry)items[key];
						if (ce != null && ce.ExpirationTime < n) {
							if (!rwlock.IsWriterLockHeld) rwlock.UpgradeToWriterLock(Timeout.Infinite);
							this.InternalRemove(key);
						}
					}
#if PUBLISH_PERFORMANCE_COUNTERS
					cacheSize.RawValue = this.Count;
#endif
				} finally {
					rwlock.ReleaseReaderLock();
				}
			} finally {
				Interlocked.Decrement(ref inOptimization);
				Log.Info(RM.GetString("LogCacheOptimized"), this.Name);
			}
		}

		/// <summary>Количество элементов в <see cref="Cache"/>.</summary>
		/// <exception cref="ObjectDisposedException">Для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
		/// <seealso cref="Add"/>
		/// <seealso cref="Remove"/>
		/// <seealso cref="this"/>
		public int Count {
			get {
				rwlock.AcquireReaderLock(Timeout.Infinite);
				try {
					if (items == null) throw new ObjectDisposedException(this.GetType().Name);
					return items.Count;
				} finally {
					rwlock.ReleaseReaderLock();
				}
			}
		}

		/// <summary>Создает <see cref="IEnumerator"/> для перебора элементов <see cref="Cache"/>.</summary>
		/// <remarks>Перебор элементов кеша - опасная операция. Во время перебора элементы кеша могут
		/// быть удалены. В данный момент, <c>IEnumerator</c>, создаваемый этим методом, не делает ничего
		/// для того, чтобы скрыть изменения кеша и избежать ошибок. Но в будущем, это скорее всего изменется.
		/// <para>Чтобы гарантировать неизменность кеша используйте методы <see cref="Lock"/> и <see cref="Unlock"/>.
		/// </para></remarks>
		/// <exception cref="ObjectDisposedException">Для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
		/// <seealso cref="this"/>
		public IEnumerator GetEnumerator() {
			if (items == null) throw new ObjectDisposedException(this.GetType().Name);
			return new Enumerator(items.GetEnumerator());
		}

		/// <summary>Блокирует запись в <see cref="Cache"/>.</summary>
		/// <remarks>Поток вызвавший этот метод блокирует запись в кеш таким образом, что только
		/// сам может в него писать. Это означает, что если поток сам не меняет кеш, то никто другой его
		/// тоже не меняет. Следует избегать блокирования <see cref="Cache"/>
		/// на длительний период, потому что пока кеш заблокирован другие потоки не могут в него писать, в том числе и
		/// поток, выполняющий оптимизацию кеша. </remarks>
		/// <seealso cref="Unlock"/>
		/// <seealso cref="GetEnumerator"/>
		public void Lock() {
			rwlock.AcquireReaderLock(Timeout.Infinite);
		}

		/// <summary>Снимает блокировку записи в <see cref="Cache"/>.</summary>
		/// <seealso cref="Lock"/>
		/// <seealso cref="GetEnumerator"/>
		public void Unlock() {
			rwlock.ReleaseReaderLock();
		}

		/// <summary>Gets the current sequence number.</summary>
		/// <value>The current sequence number.</value>
		/// <remarks><para>The sequence number increases whenever a thread acquires the writer lock.
		/// You can save the sequence number and pass it to AnyWritersSince at a later time, if
		/// you want to determine whether other threads have acquired the writer lock in the meantime.</para>
		/// <para>You can use WriterSeqNum to improve application performance. For example, a thread might
		/// cache the information it obtains while holding a reader lock. After releasing and later reacquiring
		/// the lock, the thread can determine whether other threads have written to the resource by calling
		/// <see cref="AnyWritersSince"/>; if not, the cached information can be used. This technique is useful
		/// when reading the information protected by the lock is expensive; for example,
		/// running a database query.</para>
		/// <para>The caller must be holding a reader lock or a writer lock in order for the sequence
		/// number to be useful.</para>
		///</remarks>
		/// <seealso cref="Lock"/>
		/// <seealso cref="Unlock"/>
		/// <seealso cref="AnyWritersSince"/>
		public int WriterSeqNum {
			get {
				return rwlock.WriterSeqNum;
			}
		}

		/// <summary>Indicates whether the writer lock has been granted to any thread since the sequence
		/// number was obtained.</summary>
		/// <returns>true if the writer lock has been granted to any thread since the sequence number
		/// was obtained; otherwise, false.</returns>
		/// <remarks><para>You can use WriterSeqNum and AnyWritersSince to improve application
		/// performance. For example, a thread might cache the information it obtains while holding
		/// a reader lock. After releasing and later reacquiring the lock, the thread can use
		/// AnyWritersSince to determine whether other threads have written to the resource in the
		/// interim; if not, the cached information can be used. This technique is useful where
		/// reading the information protected by the lock is expensive; for example, running a database query.</para>
		/// <para>The caller must be holding a reader lock or a writer lock in order for the sequence
		/// number to be useful.</para>
		///</remarks>
		/// <seealso cref="Lock"/>
		/// <seealso cref="Unlock"/>
		/// <seealso cref="WriterSeqNum"/>
		public bool AnyWritersSince(int seqNum) {
			return rwlock.AnyWritersSince(seqNum);
		}

		/// <summary>Прочитать или установить периодичность выполнения оптимизации <see cref="Cache"/>.</summary>
		/// <value>Периодичность выполнения оптимизации в секундах. </value>
		/// <remarks>Если указанное значение
		/// меньше 5 секунд, период оптимизации принимается равным 5 секундам.</remarks>
		/// <exception cref="ObjectDisposedException">Вызывается set, тогда когда для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
		/// <seealso cref="InOptimization"/>
		public int CleanPeriod {
			get { return cleanPeriod; }
			set {
				rwlock.AcquireReaderLock(Timeout.Infinite);
				try {
					if (items == null) throw new ObjectDisposedException(this.GetType().Name);
					cleanPeriod = (value < 5) ? 5 : value;
					if (timer != null) {
						rwlock.UpgradeToWriterLock(Timeout.Infinite);
						timer.Change(cleanPeriod * 1000, cleanPeriod * 1000);
					}
				} finally {
					rwlock.ReleaseReaderLock();
				}
			}
		}

		/// <summary>Определить активность оптимизации для этого экземпляра <see cref="Cache"/>.</summary>
		/// <value>true если оптимизация активна и false, если нет. </value>
		/// <seealso cref="CleanPeriod"/>
		public bool InOptimization {
			get {
				return inOptimization > 0;
			}
		}

		/// <summary>Прочитать или установить имя для именованого экземпляра <see cref="Cache"/>.</summary>
		/// <value>Имя экземпляра.</value>
#if PUBLISH_PERFORMANCE_COUNTERS
		/// <exception cref="ObjectDisposedException">Вызван set и для данного экземпляра уже
		/// вызван метод <see cref="Dispose"/>.</exception>
#endif
		public string Name {
			get { return (string)cacheName; }
			set {
#if PUBLISH_PERFORMANCE_COUNTERS
				rwlock.AcquireWriterLock(Timeout.Infinite);
				try {
					if (items == null) throw new ObjectDisposedException(this.GetType().Name);
					Interlocked.Exchange(ref cacheName, value);
					if (hitCounter != null) {
						hitCounter.RemoveInstance();
						cacheSize.RemoveInstance();
						hitCounter.InstanceName = cacheSize.InstanceName = value;
					} else {
						hitCounter = new PerformanceCounter("Front Cache", "Hit Count", value, false);
						cacheSize = new PerformanceCounter("Front Cache", "Cache Size", value, false);
						hitCounter.RawValue = cacheSize.RawValue = 0;
					}
				} finally {
					rwlock.ReleaseWriterLock();
				}
#else
				Interlocked.Exchange(ref cacheName, value);
#endif
			}
		}

#if PUBLISH_PERFORMANCE_COUNTERS
		PerformanceCounter hitCounter;
		PerformanceCounter cacheSize;
#endif

		/// <summary>Прочитать или установить период абсолютного устаревания по-умолчанию.</summary>
		/// <value>Период абсолютного устаревания по-умолчанию.</value>
		/// <remarks>По-умолчанию, период абсолютного устаревания равен 60 минут.</remarks>
		public TimeSpan DefaultAbsolutePeriod {
			get { return defaultAbsolutePeriod; }
			set { defaultAbsolutePeriod = value; }
		}

		/// <summary>Прочитать или установить период относительного устаревания по-умолчанию.</summary>
		/// <value>Период относительного устаревания по-умолчанию.</value>
		/// <remarks>По-умолчанию, период относительного устаревания равен 15 минутам.</remarks>
		public TimeSpan DefaultSlidingPeriod {
			get { return defaultSlidingPeriod; }
			set { defaultSlidingPeriod = value; }
		}

		///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
		protected class CacheEntry {
			DateTime		expire;
			//WeakReference	value;
			object		value;

			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public CacheEntry(object value, DateTime absoluteExpiration, TimeSpan slidingExpiration) {
				this.value = value; //new WeakReference(value);
				AbsoluteExpiration = absoluteExpiration;
				SlidingExpiration = slidingExpiration;
				expire = DateTime.Now + slidingExpiration;
				if (expire > absoluteExpiration) expire = absoluteExpiration;
			}
			
			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public object Value { get {
				//if (value.IsAlive) {
				if (value != null) {
					DateTime newExpire = DateTime.Now + SlidingExpiration;
					expire = (newExpire < AbsoluteExpiration) ? newExpire : AbsoluteExpiration;
					return value; //value.Target;
				} else {
					expire = DateTime.MinValue;
					value = null; // это вместо weakreference
					return null;
				}
			} }

			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public DateTime ExpirationTime { get {
				return expire;
			} }

			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public readonly DateTime AbsoluteExpiration;
			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public readonly TimeSpan SlidingExpiration;
		}

		///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
		public class Enumerator : IDictionaryEnumerator {
			private IDictionaryEnumerator _e;
			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public Enumerator (IDictionaryEnumerator e) { _e = e; }
			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public void Reset() { _e.Reset(); }
			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public bool MoveNext() { return _e.MoveNext(); }
			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public object Current { get { return this.Entry;	} }
			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public DictionaryEntry Entry {get {
				DictionaryEntry de = _e.Entry;
				de.Value = ((CacheEntry)de.Value).Value;
				return de;
			} }
			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public object Key {get { return this.Entry.Key; } }
			///<summary>Этот класс предназначен для внутреннего использования классом <see cref="Cache"/></summary>
			public object Value {get { return this.Entry.Value; } }
		}
		
	}
}

