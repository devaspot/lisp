// $Id: IO.cs 657 2006-05-30 13:31:52Z pilya $

using System;
using System.Collections;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace Front.IO {
	// TODO DF0023: Слишком эпизодическое и внутреннее использование класса. Его можно перенести в FS

	/// <summary>Предоставляет пул потоков, которые могут быть использованы для операций ввода-вывода.</summary>
	/// <remarks>Рекоммедуется использовать <see cref="BufferPool.Allocate"/> вместо явного выделения
	/// памяти путем создания новго массива байтов. Это дает возможность использовать один и тот же
	/// буфер повторно.</remarks>
	public class BufferPool : MarshalByRefObject, IDisposable {
		/// <summary>Внутреннея реализация буфера для класса <see cref="BufferPool"/>.</summary>
		/// <remarks>Обычно, экземпляры этого класса не создаются напрямую приложением, а
		/// приложение получает их у <srr cref="BufferPool"/> вызовом метода <see cref="BufferPool.Allocate"/>,
		/// но явное создание не запрещено.</remarks>
		public class Buffer : IDisposable {
			BufferPool		pool;
			byte[]			buffer;
			int				usedFlag;
			bool			mustClean;

			/// <summary>Создать новый <see cref="Buffer"/>.</summary>
			/// <param name="pool"><see cref="BufferPool"/>, который будет управлять буфером.</param>
			/// <param name="size">Размер нового буфера в байтах.</param>
			/// <remarks><paramref name="pool"/> может принимать значение <b>null</b>, это означает что
			/// новым буфером не будет управлять <see cref="BufferPool"/>.</remarks>
			public Buffer(BufferPool pool, int size) {
				this.pool = pool;
				buffer = new byte[size];
				mustClean = false;
			}

			/// <summary>Перевести буфер в состояние "используется".</summary>
			/// <remarks>Перевод буфера в состояние "используется" препятствует выделению
			/// его методом <see cref="BufferPool.Allocate"/> объекта <see cref="BufferPool"/>,
			/// который управляет данным буфером.
			/// <para>В случае когда <see cref="Buffer"/> создан явно и им не управляет <see cref="BufferPool"/>
			/// вызовы методов <see cref="Allocate"/> и <see cref="Release"/> не имеют никакого влияния.</para>
			/// </remarks>
			/// <returns>true, если буфер до этого был в состоянии "не используется" и удалось перевести его в
			/// состояние "используется"; иначе false</returns>
			public bool Allocate() {
				return Allocate(false);
			}

			/// <summary>Перевести буфер в состояние "используется".</summary>
			/// <param name="cleanOnRelease">Указывает следует ли очищать буфер перед возвратом его в пул.</param>
			/// <remarks>Перевод буфера в состояние "используется" препятствует выделению
			/// его методом <see cref="BufferPool.Allocate"/> объекта <see cref="BufferPool"/>,
			/// который управляет данным буфером.
			/// <para>В случае когда <see cref="Buffer"/> создан явно и им не управляет <see cref="BufferPool"/>
			/// вызовы методов <see cref="Allocate"/> и <see cref="Release"/> не имеют никакого влияния.</para>
			/// </remarks>
			/// <returns>true, если буфер до этого был в состоянии "не используется" и удалось перевести его в
			/// состояние "используется"; иначе false</returns>
			public bool Allocate(bool cleanOnRelease) {
				int flag = Interlocked.CompareExchange(ref usedFlag, 1, 0);
				if (flag == 0) {
					// если предыдущий "пользователь" требовал очистки - чистим
					if (mustClean)
						System.Array.Clear(buffer, 0, buffer.Length);
					// запоминаем что хочет этот пользователь
					mustClean = cleanOnRelease;
					return true;
				} else
					return false;	
			}

			/// <summary>Перевести буфер в состояние "не используется".</summary>
			/// <remarks>После вызова этого метода данный буфер может быть выделен методом
			/// <see cref="BufferPool.Allocate"/> объекта <see cref="BufferPool"/>,
			/// который управляет данным буфером.
			/// <para>В случае когда <see cref="Buffer"/> создан явно и им не управляет <see cref="BufferPool"/>
			/// вызовы методов <see cref="Allocate"/> и <see cref="Release"/> не имеют никакого влияния.</para>
			/// </remarks>
			public void Release() {
				Interlocked.Exchange(ref usedFlag, 0);
				if (pool != null) pool.mrEvent.Set();
			}

			/// <summary>Собственно буфер (память для операций ввода-вывода).</summary>
			/// <value>Массив байтов, который используется <see cref="Buffer"/> в качестве буфера.</value>
			public byte[] Array { get { return buffer; } }

			/// <summary>Получить размер буфера в байтах.</summary>
			/// <value>Размер буфера в байтах.</value>
			public int Size { get { return buffer.Length; } }

			/// <summary>Узнать занят ли сейчас этот буфер.</summary>
			/// <value>true, если этот буфер сейчас используется и false, если он свободен.</value>
			public bool InUse { get { return (usedFlag != 0); } }

			void IDisposable.Dispose() {
				Release();
			}

			/// <summary>Преобразовать <see cref="Buffer"/> в <c>byte[]</c>.</summary>
			/// <remarks>По-сути, это преобразование равнозначно обращению к свойству <see cref="Array"/> и потому
			/// очень дешево. Оно предназначено для упрощения использования <see cref="BufferPool"/> в языке C#.</remarks>
			public static implicit operator byte[](Buffer b) {
				return b.Array;
			}
		}
		
		int					granularity;
		int					maxBufferCount;
		ArrayList			buffers = new ArrayList();
		ReaderWriterLock	rwLock = new ReaderWriterLock();
		ManualResetEvent	mrEvent = new ManualResetEvent(false);

		/// <summary>Initializes a new instance of the <see cref="BufferPool"/> class.</summary>
		/// <remarks>Создать <see cref="BufferPool"/> c неограниченным количеством буферов и гранулярностью равной 1024.</remarks>
		public BufferPool():this(1024) {}

		/// <summary>Initializes a new instance of the <see cref="BufferPool"/> class.</summary>
		/// <remarks>Создать <see cref="BufferPool"/> c неограниченным количеством буферов и заданной гранулярностью</remarks>
		/// <param name="granularity">Гранулярность пула. До значения кратного гранулярности, дополняется размер
		/// всех буферов, создаваемых <see cref="BufferPool"/>.</param>
		public BufferPool(int granularity):this(granularity, 0) {}

		/// <summary>Initializes a new instance of the <see cref="BufferPool"/> class.</summary>
		/// <remarks>Создать <see cref="BufferPool"/> c количеством буферов,
		/// ограниченном <paramref name="maxBufferCount"/>. Если <paramref name="maxBufferCount"/> принимает
		/// значение 0, то количество буферов неограничено.</remarks>
		/// <param name="granularity">Гранулярность пула. До значения кратного гранулярности, дополняется размер
		/// всех буферов, создаваемых <see cref="BufferPool"/>.</param>
		/// <param name="maxBufferCount">Максимальное количество буферов, которые могут существовать одновременно
		/// в данном <see cref="BufferPool"/>.</param>
		public BufferPool(int granularity, int maxBufferCount) {
			this.granularity	= granularity;
			this.maxBufferCount	= maxBufferCount;
		}

		/// <summary>Освободить неуправляемые ресурсы и <see cref="IDisposable"/> объекты.</summary>
		public void Dispose() {
			Dispose(true);
		}

		/// <summary>Освободить неуправляемые ресурсы и <see cref="IDisposable"/> объекты.</summary>
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				GC.SuppressFinalize(this);
				if (mrEvent != null) {
					mrEvent.Close();
					mrEvent = null;
				}
			}
		}

		/// <summary>Выделить буфер.</summary>
		/// <param name="reqSize">Размер буфера, который необходимо создать.</param>
		/// <remarks>Этот метод создает буфер, который не будет очищен при освобождении.
		/// <para>Если свободного буфера нет, то метод будет ждать возможности создать его
		/// неограниченное время.</para>
		/// <para>Следует учитывать, что при создании нового буфера метод <see cref="Allocate"/> гарантирует
		/// что возвращаемый буфер будет иметь размер не меньший чем <paramref name="reqSize"/>. При этом буфер\
		/// может быть больше.</para>
		/// </remarks>
		/// <seealso cref="CollectLostBuffers"/>
		/// <seealso cref="Granularity"/>
		public Buffer Allocate(int reqSize) {
			return Allocate(reqSize, false, Timeout.Infinite);
		}
		
		/// <summary>Выделить буфер.</summary>
		/// <param name="reqSize">Размер буфера, который необходимо создать.</param>
		/// <param name="cleanOnRelease">Требование очищать буфер при возврате его в пул.</param>
		/// <remarks>Если свободного буфера нет, то метод будет ждать возможности создать его
		/// неограниченное время.<para>Следует учитывать, что при создании нового буфера метод <see cref="Allocate"/> гарантирует
		/// что возвращаемый буфер будет иметь размер не меньший чем <paramref name="reqSize"/>. При этом буфер\
		/// может быть больше.</para>
		/// </remarks>
		/// <seealso cref="CollectLostBuffers"/>
		/// <seealso cref="Granularity"/>
		public Buffer Allocate(int reqSize, bool cleanOnRelease) {
			return Allocate(reqSize, cleanOnRelease, Timeout.Infinite);
		}

		/// <summary>Выделить буфер.</summary>
		/// <param name="reqSize">Размер буфера, который необходимо создать.</param>
		/// <param name="timeOut">Время в миллисекундах, которое метод будет ждать, в случае если буфера нет.</param>
		/// <remarks>Этот метод создает буфер, который не будет очищен при освобождении.
		/// <para>Если свободного буфера нет, то метод будет ждать возможности создать его <paramref name="timeOut"/>
		/// миллисекунд.</para>
		/// <para>Следует учитывать, что при создании нового буфера метод <see cref="Allocate"/> гарантирует
		/// что возвращаемый буфер будет иметь размер не меньший чем <paramref name="reqSize"/>. При этом буфер\
		/// может быть больше.</para>
		/// </remarks>
		/// <seealso cref="CollectLostBuffers"/>
		/// <seealso cref="Granularity"/>
		public Buffer Allocate(int reqSize, int timeOut) {
			return Allocate(reqSize, false, timeOut);
		}

		/// <summary>Выделить буфер.</summary>
		/// <param name="reqSize">Размер буфера, который необходимо создать.</param>
		/// <param name="cleanOnRelease">Требование очищать буфер при возврате его в пул.</param>
		/// <param name="timeOut">Время в миллисекундах, которое метод будет ждать, в случае если буфера нет.</param>
		/// <remarks>Этот метод создает буфер, который может быть очищен при освобождении.
		/// <para>Если свободного буфера нет, то метод будет ждать возможности создать его <paramref name="timeOut"/>
		/// миллисекунд.</para>
		/// <para>Следует учитывать, что при создании нового буфера метод <see cref="Allocate"/> гарантирует
		/// что возвращаемый буфер будет иметь размер не меньший чем <paramref name="reqSize"/>. При этом буфер\
		/// может быть больше.</para>
		/// </remarks>
		/// <seealso cref="CollectLostBuffers"/>
		/// <seealso cref="Granularity"/>
		public virtual Buffer Allocate(int reqSize, bool cleanOnRelease, int timeOut) {
			bool deadFound = false;
			if (mrEvent == null)
				throw new ObjectDisposedException(this.GetType().Name);
			Buffer result = null;
			bool wait = false;
			DateTime deadLine = (timeOut == Timeout.Infinite)
				? DateTime.MaxValue
				: DateTime.Now + TimeSpan.FromMilliseconds(timeOut);
			do {
				if (wait) {
					if (mrEvent.WaitOne(
							(int)(deadLine - DateTime.Now).TotalMilliseconds, false ))
						mrEvent.Reset();
					else
						throw new ApplicationException(RM.GetString("ErrTimeout"));
				}
				rwLock.AcquireReaderLock(
						(timeOut > 0)
							? (int)(deadLine - DateTime.Now).TotalMilliseconds
							: timeOut);
				if (!rwLock.IsReaderLockHeld)
					break;  // ApplicationException will be throwed
				try {
					foreach (WeakReference wr in buffers) {
						if (wr.IsAlive) {
							Buffer b = wr.Target as Buffer;
							if (b.Size >= reqSize && b.Allocate(cleanOnRelease)) {
								result = b;
								break;
							}
						} else
							deadFound = true;
					}

					if (result == null && (maxBufferCount == 0 || buffers.Count < maxBufferCount)) {
						rwLock.UpgradeToWriterLock(
								(timeOut > 0)
									? (int)(deadLine - DateTime.Now).TotalMilliseconds
									: timeOut);
						if (!rwLock.IsWriterLockHeld)
							break; // ApplicationException will be throwed
						result = NewAllocatedBuffer(reqSize, cleanOnRelease);
						buffers.Add(new WeakReference(result));
					}
				} finally {
					rwLock.ReleaseReaderLock();
				}
				wait = true;
			} while (result == null && DateTime.Now < deadLine);
			if (deadFound)
				ThreadPool.QueueUserWorkItem(new WaitCallback(CollectLostBuffers));
			if (result == null && timeOut != 0)
				throw new ApplicationException(RM.GetString("ErrTimeout"));
			return result;
		}

		/// <summary> Выделить новый буфер и перевести его в состояние "используется".</summary>
		/// <param name="size">Размер буфера в байтах</param>
		/// <param name="cleanOnRelease">true, если буфер надо очищать при возврате в пул.</param>
		/// <remarks>Этот метод может выделить буфер не того размера, который был запрошен пользователем.
		/// Умолчательная реализация метода дополняет размер буфера до кратного <see cref="Granularity"/>.</remarks>
		/// <seealso cref="Allocate"/>
		protected virtual Buffer NewAllocatedBuffer(int size, bool cleanOnRelease) {
			int mod = size % granularity;
			size = granularity * ((size / granularity) + ((size % granularity > 0) ? 1 : 0));
			Buffer r = new Buffer(this, size);
			r.Allocate(cleanOnRelease);
			return r;
		}

		// См. CollectLostBuffers()
		void CollectLostBuffers(object state) {
			CollectLostBuffers();
		}

		/// <summary> Провести оптимизацию пула. </summary>
		/// <remarks> Все ссылки на буферы, которые освобождены сборщиком мусора, будут
		/// удалены из <see cref="BufferPool"/>.<para>Обычно приложению не требуется вызывать
		/// этот метод напрямую.</para></remarks>
		/// <seealso cref="Allocate"/>
		public void CollectLostBuffers() {
			rwLock.AcquireWriterLock(Timeout.Infinite);
			if (rwLock.IsWriterLockHeld) try {
				int index = 0;
				while (index < buffers.Count) {
					WeakReference wr = buffers[index] as WeakReference;
					if (!wr.IsAlive)
						buffers.RemoveAt(index);
					else
						index++;
				}
			} finally {
				rwLock.ReleaseWriterLock();
			}
		}

		/// <summary> Вернуть количество буферов в пуле. </summary>
		/// <value> Количество буферов в пуле. </value>
		/// <remarks> Данный метод возвращает количество ссылок на буферы, которыми управляет
		/// <see cref="BufferPool"/>. Некоторые из этих буферов уже могут быть удалены сборщиком
		/// мусора. </remarks>
		/// <seealso cref="UsedMemory"/>
		public int ActiveBuffers {
			get {
				rwLock.AcquireReaderLock(Timeout.Infinite);
				int cnt = buffers.Count;
				rwLock.ReleaseReaderLock();
				return cnt;
			}
		}

		/// <summary> Вернуть суммарный объем памяти, занимаемый всеми активными буферами. </summary>
		/// <value> Суммарный объем памяти управляемой <see cref="BufferPool"/>. </value>
		/// <remarks> Метод учитывает только активные буфера, те которые еще не удалены сборщиком мусора. </remarks>
		/// <seealso cref="ActiveBuffers"/>
		public int UsedMemory {
			get {
				int size = 0;
				if (mrEvent != null) {
					rwLock.AcquireReaderLock(Timeout.Infinite);
					if (rwLock.IsReaderLockHeld) try {
						foreach (WeakReference wr in buffers)
							if (wr.IsAlive)
								size += ((Buffer)wr.Target).Size;
					} finally {
						rwLock.ReleaseReaderLock();
					}
				}
				return size;
			}
		}

		/// <summary>Получить или установить гранулярность пула.</summary>
		/// <remarks>При создании нового буфера в пуле, его размер дополняется до значения
		/// кратного <see cref="Granularity"/>.
		/// <para>Изменение гранулярности пула влияет только на вновь создаваемые буфера. Уже созданные
		/// буфера остаются с тем же размером.</para></remarks>
		/// <value>Гранулярность пула.</value>
		/// <seealso cref="MaxBufferCount"/>
		public int Granularity {
			get { return granularity; }
			set { granularity = value; }
		}

		/// <summary>Получить или установить максимальное количество активных буферов.</summary>
		/// <remarks>При изменении максимального количества буферов в меньшую сторону, может
		/// оказаться, что количество буферов в пуле уже приевышает указанное количество. В этом случае,
		/// пул не будет создавать новых потоков, но старые будут существовать.</remarks>
		/// <value>Максимальное количество буферов.</value>
		/// <seealso cref="Granularity"/>
		public int MaxBufferCount {
			get { return maxBufferCount; }
			set { maxBufferCount = value; }
		}

		/// <summary>Returns a <see cref="System.String"/> that represents the current <see cref="Object"/>.</summary>
		/// <value>A <see cref="System.String"/> that represents the current <see cref="Object"/>.</value>
		public override string ToString() {
			return String.Format(
					"{0}: Allocated {1} buffers, {2} Kb used.",
					this.GetType().Name, ActiveBuffers, UsedMemory / 1024);
		}

		static object managerLock = new object();
		static BufferPool defaultPool;
		/// <summary>Получить пул буферов по-умолчанию.</summary>
		/// <remarks>Пул буферов по-умолчанию создается при первом обращении к этому свойству и существуют
		/// все время до завершения приложения. Некоторые библиотечные ф-ии Front Common Library и зависимых
		/// быблиотек используют его для операций ввода-вывода.<para>Пул буферов по-умолчанию не ограничивает
		/// максимального количества буферов и имеет гранулярность 1024. Эти параметры можно изменить, используя
		/// открытые свойеста класса <see cref="BufferPool"/>.</para></remarks>
		public static BufferPool DefaultPool {
			get {
				if (defaultPool == null) {
					Debug.Assert(managerLock != null);
					lock (managerLock) {
						if (defaultPool == null)
							defaultPool = new BufferPool();
						managerLock = null;
					}
				}
				return defaultPool;
			}
		}
	}

	/// <summary>Вспомогательный класс, реализующий утилитарный методы работы с <see cref="Stream"/>.</summary>
	public sealed class Streams {
		Streams() {}
		/// <summary>Скопировать данные из одного <see cref="Stream"/> в другой.</summary>
		/// <param name="src">Поток - источник данных.</param>
		/// <param name="dst">Поток - получатель данных.</param>
		/// <param name="bp">Пул буферов, который будет использован для копирования.</param>
		/// <param name="bufferSize">Размер буфера, который будет использован для копирования.</param>
		public static void Copy(Stream src,
				Stream dst, BufferPool bp, int bufferSize) {
			BufferPool.Buffer buffer = null;
			if (bp != null) buffer = bp.Allocate(bufferSize, 0);
			if (buffer == null) buffer = new BufferPool.Buffer(null, bufferSize);
			using (buffer) {
				Copy(src, dst, buffer);
			}
		}

		/// <summary>Скопировать данные из одного <see cref="Stream"/> в другой.</summary>
		/// <param name="src">Поток - источник данных.</param>
		/// <param name="dst">Поток - получатель данных.</param>
		/// <param name="buffer">Буфер который будет использован для копирования.</param>
		public static void Copy(Stream src, Stream dst, byte[] buffer) {
			int length = buffer.Length;
			int readed;
			while ( (readed = src.Read(buffer, 0, length)) > 0 )
				dst.Write(buffer, 0, readed);
		}

		/// <summary>Скопировать данные из одного <see cref="Stream"/> в другой.</summary>
		/// <param name="src">Поток - источник данных.</param>
		/// <param name="dst">Поток - получатель данных.</param>
		/// <remarks>Для копирования будет использован буфер размером 8192 байта, полученный у
		/// пула буферов по-умолчанию.</remarks>
		public static void Copy(Stream src, Stream dst) {
			Copy(src, dst, BufferPool.DefaultPool, 8192);
		}
	}


	public class UnclosableStream : Stream {
		Stream stream;

		public UnclosableStream(Stream stream) {
			this.stream = stream;
		}

		public override void Close() {
			// ignore stream closing
		}

		public override void Flush() { stream.Flush(); }
		public override int Read(byte[] buffer, int offset, int count) {
			return stream.Read(buffer, offset, count);
		}
		public override int ReadByte() {
			return stream.ReadByte();
		}
		public override long Seek(long offset, SeekOrigin origin) {
			return stream.Seek(offset, origin);
		}
		public override void SetLength(long value) {
			stream.SetLength(value);
		}
		public override void Write(byte[] buffer, int offset, int count) {
			stream.Write(buffer, offset, count);
		}
		public override void WriteByte(byte value) {
			stream.WriteByte(value);
		}
		public override bool CanRead { get { return stream.CanRead; } }
		public override bool CanSeek { get { return stream.CanSeek; } }
		public override bool CanWrite { get { return stream.CanWrite; } }
		public override long Length { get { return stream.Length; } }
		public override long Position { get { return stream.Position; } set { stream.Position = value; } }
	}

}

