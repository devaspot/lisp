// $Id: Wrapper.cs 944 2006-06-08 15:14:49Z john $

using System;

namespace Front {

	/// <summary>Общий интерфейс для классов-оберток.</summary>
	/// <remarks>Реализация <see cref="IWrapper"/> может запрещать или разрешать доступ к инкапсулированному объекту.</remarks>
	/// <seealso cref="Wrapper"/>
	public interface IWrapper {
		/// <summary>Предоставляет инкапсулированный объект.</summary>
		/// <value>Может возвращать Null если доступ к объекту запрещен или невозможен.</value>
		object Wrapped { get; }

		/// <summary>Получить инкапсулированный объект указанного типа.</summary>
		/// <param name="type">Тип искомого объекта (или интерфейса, который объект должен поддерживать).</param>
		/// <returns>Один из инкапсулированных объектов, который соответствует указаному типу или реализует интерфейс.</returns>
		object GetWrapped(Type type);

		/// <summary>Получить следующий инкапсулированный объект указанного типа.</summary>
		/// <param name="type">Тип искомого объекта (или интерфейса, который объект должен поддерживать).</param>
		/// <param name="obj">Предыдущий объект</param>
		/// <returns>Один из инкапсулированных объектов, который соответствует указаному типу или реализует интерфейс.
		/// Используется, если инкапсулировано несколько объектов. Если <see cref="obj"/> не указан (имеет значение null),
		/// то работа эквивалентна методу <see cref="GetWrapped">GetWrapped(Type type)</see>.</returns>
		object GetWrapped(Type type, object obj);
	}


	/// <summary>Шаблон интерфейса типизированной обертки.</summary>
	/// <remarks>Этот интерфейс расширяет <see cref="IWrapper"/> добавляет в него контроль типа инкапсулируемого объекта.</remarks>
	public interface IWrapper<T> : IWrapper {
		new T Wrapped { get; }
	}

	
	/// <summary>Простейший класс-обертка. Может служить базовым для класов оберток.</summary>
	/// <remarks>Соодержит утилитарную функциональность для работы с любыми обертками.</remarks>
	[Serializable]
	public abstract class Wrapper : IWrapper {
		
		protected object InnerWrapped;

		protected Wrapper() { }

		/// <summary>Создать новый объект-обертку.</summary>
		/// <param name="o">инкапсулируемый объект.</param>
		public Wrapper(object o) {
			InnerWrapped = o; 
		}

		public virtual object Wrapped { get { return InnerWrapped; } }

		/// <summary>Возвращает обернутый объект, как объект указанного типа.</summary>
		/// <param name="type">Тип, который должен иметь результат вызова.</param>
		/// <returns>Сам объект-обертка или вложенный объект, если обертка не поддерживает
		/// указанный тип.</returns>
		/// <remarks>Эта реализация просто вызывает статический метод <see cref="Lookup"/>.</remarks>
		public virtual object GetWrapped(Type type) {
			return Wrapper.Lookup(this, type);
		}

		public virtual object GetWrapped( Type type, object obj) {
				// TODO DF0027: написать! написать тест и задокументировать!
				// надо подумать как уровне базового класса выполнить поиск в ширину
				// пока оставим его как абстрактный класс
				throw new NotImplementedException("TODO DF0027");
		}

		/// <summary>Найти рекурсивно среди вложенных объектов того, кто имеет указанный тип.</summary>
		public static object Lookup(object w, Type t) {
			object res = null;
			while (res == null && w != null) {
				res = t.IsInstanceOfType( w ) ? w : null;
				if (res == null)
					w = (w is IWrapper) ? ((IWrapper)w).Wrapped : null;
			}
			return res;
		}
	}

	
	// TODO DF0001: написать
	// сделано, проверить!

	// базовый типизированный Wrapper
	[Serializable]
	public class Wrapper<T> : Wrapper, IWrapper<T> {
		protected Wrapper() {}

		protected Wrapper(object o) : base(o) { }
		public Wrapper(T o):base(o) { }
		
		new public virtual T Wrapped { get { return (T)base.Wrapped; } }
		object IWrapper.Wrapped { get { return base.Wrapped; } }
	}

	
	// TODO DF0002: написать
	// Так как InitializableBase унаследован от MarshalByRefObject, то 
	// используется для написания сервисов, публикуемых по ремоутингу
	public class ServiceWrapper<T> : InitializableBase, IWrapper<T>  {
		protected T InnerWrapped;
		protected bool wrappedSet = false;

		public ServiceWrapper() : base() {}
		
		public ServiceWrapper(T obj) : base() {
			InnerWrapped = obj;
			wrappedSet = true;
			AttachToWrapped();
		}

		public ServiceWrapper(IServiceProvider sp) : base(sp) { }

		public ServiceWrapper(IServiceProvider sp, bool init) : base(sp, init) { }

		protected override bool OnInitialize(IServiceProvider sp) {
			if (!wrappedSet)
				try {
					if (sp != null)
						InnerWrapped = (T)sp.GetService(typeof(T));
					if (InnerWrapped != null) {
						wrappedSet = true;
						AttachToWrapped();
					}
				} catch (Exception ex) { } 
			return base.OnInitialize(sp);
		}

		protected virtual void AttachToWrapped() {
		}

		protected virtual void DetachFromWrapped() {
		}

		public virtual T Wrapped { get { return InnerWrapped; } }
		object IWrapper.Wrapped { get { return this.Wrapped; } }

		public virtual object GetWrapped(Type type) {
			return Wrapper.Lookup(this, type);
		}

		public virtual object GetWrapped(Type type, object obj) {
			throw new NotImplementedException();
		}
	}
	
}
