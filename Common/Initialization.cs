// $Id: Initialization.cs 1628 2006-07-14 14:04:42Z pilya $

using System;
using System.Collections;
using System.Collections.Specialized;

namespace Front {

	/// <summary>Интерфейс для классов с поздней инициализацией.</summary>
	/// <remarks>Некоторые классы, например сильно взаимодействующие сервисы, не могут быть
	/// проинициализированы в конструкторе и требуют второй фазы инициализации. Для стандартизации
	/// решения такой задачи предназначен <see cref="IInitializable"/>.</remarks>
	public interface IInitializable {
		/// <summary>Проверить или объект полностью инициализирован.</summary>
		/// <value>true, если объект инициализирован и готов к употреблению и false, если еще требуется
		/// вызов <see cref="Initialize"/>.</value>
		bool IsInitialized { get; }
		/// <summary>Проверить. находится ли объект в процессе незавершенной инициализации.</summary>
		/// <value>true, если объект находится в процессе незавершенной инициализации.</value>
		bool IsInitializing { get; }
		/// <summary>Проинициализировать объект.</summary>
		/// <param name="sp"><see cref="IServiceProvider"/>, который предоставляет сервисы для данного объекта.</param>
		/// <remarks>Объект обязан допускать повторный вызов <see cref="Initialize"/>. В этом случае он может
		/// выполнить переинициализацию или просто проигнорировать вызов.
		/// <para>Если кроме <see cref="IServiceProvider"/> для инициализации объекта необходим набор параметров,
		/// используйте класс <see cref="ParametersBag"/>.</para>
		/// </remarks>
		/// <example><code>
		/// void ForceObjectInitialization(IInitializable obj) {
		///		ParametersBag sp = new ParametersBag(myServiceProvider);
		///		sp.Parameters["src"] = @"C:\Temp";
		///		sp.Parameters["dst"] = @"D:\Work";
		///		obj.Initialize(sp);
		/// }
		/// </code></example>
		void Initialize();
		void Initialize( IServiceProvider sp );

		// TODO DF0004: обработчики заменить на типизированные!
		// TODO DF0009: сделать Before обработчик CancelHandler'ом
		event EventHandler BeforeInitialize;
		event EventHandler AfterInitialize;

	}



	/// <summary>Реализация <see cref="IServiceProvider"/> и <see cref="System.ComponentModel.Design.IServiceContainer"/>, которая
	/// содержит еще и коллекцию именнованых параметров. Используется для инициализации по интерфейсу <see cref="IInitializable"/>.</summary>
	public class ParametersBag : ServiceContainer {
		IDictionary parameters;
		
		/// <summary>Создать новый экземпляр <see cref="ParametersBag"/>.</summary>
		public ParametersBag():this(null) { }
		
		/// <summary>Создать новый экземпляр <see cref="ParametersBag"/> и передать ему
		/// родительский <see cref="System.ComponentModel.Design.IServiceContainer"/>.</summary>
		public ParametersBag(IServiceProvider parent) {
			//
			parameters = new ListDictionary();
		}

		/// <summary>Список параметров.</summary>
		public IDictionary Parameters { get { return parameters; } }
	}

	

	/// <summary>Контейнер для параметров обработчика событий инициализации по интерфейсу IInitializable.</summary>
	public class InitializeEventArgs : EventArgs {
		public IServiceProvider SP;
		public InitializeEventArgs(IServiceProvider sp) {
			this.SP = sp;
		}
	}

	

	// TODO DF0005: написать
	public class InitializationException : LocalizableException {
		public InitializationException(string m) : base(m) {}
	}



	/// <summary>Базовый класс для инициализируемых серверных объектов.</summary>
	/// <remarks>(класс является наследником <see cref="MarshalByRefObject"/>.)</remarks>
	public abstract class InitializableBase : MarshalByRefObject, IInitializable, IDisposable {
		protected IServiceProvider InnerSP;
		public IServiceProvider SP { get { return InnerSP; } }

		// TODO DF0006: продумать порядок вызова конструкторов.
		protected InitializableBase() { }
		protected InitializableBase(IServiceProvider sp): this(sp, false) {
		}
		protected InitializableBase(IServiceProvider sp, bool init) {
			if (init) 
				Initialize(sp);
			else
				this.InnerSP = sp;
		}

        int init_lock = 0;
		public virtual void Initialize() {
			Initialize(ProviderPublisher.Provider);
		}

		public virtual void Initialize(IServiceProvider sp) {
			// TODO DF0007: сделать защиту от многократной инициализации и корректную работу 
			//    в многопоточном окружении (done. Проверить!)
			// TODO ВА0008: сделать реакцию на переменную окружения "Silent" (продумать уровни шумности)
			//		если ошибка ниже уровня шумности - то писать ее в список ошибок и тихо выходить.
			//		продумать механизм доступа к списку ошибок.
			try {
				int x = System.Threading.Interlocked.Increment(ref init_lock);
				if (x > 1)
					throw new InitializationException("Concurrent initialization");

				if (IsInitialized)
					throw new InitializationException("Repeated initialization");

				// если класс был создан с ServiceProvider, но с отложеной инициализацией...
				if (SP != null && sp == null) sp = SP;
				EventHandler bi = BeforeInitialize;
				if (bi != null) bi(this, new InitializeEventArgs(sp));

				bool res = OnInitialize(sp);
				if (res) {
					EventHandler ai = AfterInitialize;
					if (ai != null) ai(this, new InitializeEventArgs(sp));
				}
				InnerIsInitialized = res;
			} 
			//catch (Exception ex) {
			//    //System.Windows.Forms.MessageBox.Show(ex.ToString());
			//    int i = 0;
			//}
			finally {
                System.Threading.Interlocked.Decrement(ref init_lock);
                init_lock = 0;
			}
		}

		public virtual void Dispose() { }

		// основной метод, который будет меняться в наследниках
		protected virtual bool OnInitialize(IServiceProvider sp) {
			this.InnerSP = sp;
			return true;
		}

		public override object InitializeLifetimeService() {
			return null;
		}

		protected bool InnerIsInitialized = false;
		public virtual bool IsInitialized { get { return InnerIsInitialized; } }

		public virtual bool IsInitializing { get { return (init_lock > 0); } }

		public event EventHandler BeforeInitialize;
		public event EventHandler AfterInitialize;
	}
}
