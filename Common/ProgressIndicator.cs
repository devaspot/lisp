// $Id: ProgressIndicator.cs 2421 2006-09-19 07:26:55Z kostya $

using System;

namespace Front.Diagnostics {

	// TODO DF0024: посмотреть совместимость с каналом обратной связи (при ассинхронной работе).

	/// <summary>Индикатор прогресса длительной операции.</summary>
	/// <remarks>Компонент А, поддерживающий индикацию прогресса, перед началом длительной
	/// операции вызывает метод Started, Функция Started должна вернуть объект-ключ, который
	/// потом будет передан компонентом А во все остальные методы и может использоватся
	/// реализацией <see cref="IProgressIndicator"/>, для того, чтобы отличать одну активную операцию от другой.
	/// <para>Во время выполнения операции, компонент А может время от времени вызывать метод
	/// Progress, и сообщать сколько процентов операции выполнено. Если при вызове <see cref="Started"/>,
	/// <c>canProgress == false</c>, то значение percent смысла не имеет (скорее всего будет == 0).
	/// Результат функции Progress указывает компоненту А, нужно ли продолжать операцию или надо
	/// ее прекратить. Но, если при вызове <see cref="Started"/>, canAbort == false, то компонент А может
	/// не послушаться.</para></remarks>
	public interface IProgressIndicator {

		/// <summary>Сообщить <see cref="IProgressIndicator"/> о начавшейся операции.</summary>
		/// <param name="source">Компонент выполняющий операцию (или управляющий ею).</param>
		/// <param name="message">Текстовое сообщение, описывающее операцию.</param>
		/// <param name="canProgress">Информация о том, может ли компонент А вычислять прогресс операции.</param>
		/// <param name="canAbort">Информация о том, можно ли эту операцию прерывать.</param>
		/// <returns>Ключ зарегистрированной операции.</returns>
		/// <exception cref="ArgumentNullException">Если <b>source</b> равен <c>null</c>.</exception>
		/// <remarks>Ключ, который возвращает эта ф-ия может быть передан в методы <see cref="Finished"/> для
		/// указания какая именно операция завершилась.<para> Если <c>message</c> равен null, или пустой строке
		/// <see cref="IProgressIndicator"/> может заменить сообщение на сообщение по умолчанию.</para></remarks>
		/// <seealso cref="Finished"/>
		/// <seealso cref="Progress"/>
		/// <seealso cref="HasActiveOperation"/>
		object Started(object source, string message, bool canProgress, bool canAbort);
		
		/// <summary>Сообщить <see cref="IProgressIndicator"/> о том, что одна из зарегистрированных операций
		/// успешно завершилась.</summary>
		/// <param name="key">Ключ, полученный при регистрации операции.</param>
		/// <param name="message">Текстовое сообщение, комментирующее завершение операции. Может быть пустой строкой или null.</param>
		/// <remarks>Если операции с соответствующим ключем не зарегистрировано (например, она уже завершена), вызов
		/// игнорируется. </remarks>
		/// <seealso cref="Started"/>
		/// <seealso cref="Canceled"/>
		/// <seealso cref="Progress"/>
		void Finished(object key, string message);
		
		/// <summary>Сообщить <see cref="IProgressIndicator"/> о том, что одна из зарегистрированных операций
		/// отменена пользователем.</summary>
		/// <param name="key">Ключ, полученный при регистрации операции.</param>
		/// <param name="message">Текстовое сообщение, комментирующее завершение операции. Может быть пустой строкой или null.</param>
		/// <remarks>Если операции с соответствующим ключем не зарегистрировано (например, она уже завершена), вызов
		/// игнорируется. </remarks>
		/// <seealso cref="Started"/>
		/// <seealso cref="Finished"/>
		/// <seealso cref="Progress"/>
		void Canceled(object key, string message);

		/// <summary>Сообщить <see cref="IProgressIndicator"/> о прогрессе операции и узнать у него следует ли продолжать
		/// выполнение операции.</summary>
		/// <param name="key">Ключ, полученный при регистрации операции.</param>
		/// <param name="percent">Значение от 0 до 100, указывающее процент завершенности операции.</param>
		/// <returns>Логическое значение, указывающее, стоит ли продожать выполнение операции или следует
		/// немедленно ее прекратить.</returns>
		/// <remarks>Если операции с соответствующим ключем не зарегистрировано (например, она уже завершена), вызов
		/// игнорируется. <para>Если операция была зарегистрирована вызовом <see cref="Started"/> со значением
		/// параметра <b>canAbort</b> равным <c>false</c>, то компонент выполняющий операцию может проигнорировать
		/// сигнал завершения операции.</para></remarks>
		/// <seealso cref="Started"/>
		/// <seealso cref="Finished"/>
		/// <seealso cref="Message"/>
		bool Progress(object key, byte percent);

		/// <summary>Предоставить <see cref="IProgressIndicator"/> комментарий относительно хода операции.</summary>
		/// <param name="key">Ключ, полученный при регистрации операции.</param>
		/// <param name="message">Текстовое сообщение, комментирующее завершение операции. Может быть пустой строкой или null.</param>
		/// <remarks>Если операции с соответствующим ключем не зарегистрировано (например, она уже завершена), вызов
		/// игнорируется. <para>Скорее всего, сообщение переданное <see cref="IProgressIndicator"/> будет каком-то образом
		/// показано пользователю, например изменением строки статуса приложения.</para></remarks>
		/// <seealso cref="Started"/>
		/// <seealso cref="Finished"/>
		/// <seealso cref="Progress"/>
		void Message(object key, string message);

		/// <summary>Узнать есть ли текущие зарегистрированные операции.</summary>
		/// <value>true, если есть хоть одна зарегистрированная операция; иначе false.</value>
		/// <seealso cref="Started"/>
		/// <seealso cref="Finished"/>
		bool HasActiveOperation { get; }
	}

	[Serializable]
	internal class NullIndicator : IProgressIndicator {
		object IProgressIndicator.Started(object source, string message, bool canProgress, bool canAbort) {
			return this;
		}
		void IProgressIndicator.Finished(object key, string message) { }
		void IProgressIndicator.Canceled(object key, string message) { }
		bool IProgressIndicator.Progress(object key, byte percent) { return true; }
		void IProgressIndicator.Message(object key, string msg) { }
		bool IProgressIndicator.HasActiveOperation { get { return false; } }
	}

	/// <summary>Статический класс, позволяющий более лаконично выполнить получение <see cref="IProgressIndicator"/>
	/// из указанного <see cref="IServiceProvider"/> и вызвать один из его методов.</summary>
	/// <remarks>Если планируется вызвать несколько методов <see cref="IProgressIndicator"/> подряд, учтите что
	/// вызов их через методы этого класса может привести к нежелательным побочным эффектам. <see cref="ProgressIndicator"/>
	/// выполняет поиск сервиса <see cref="IProgressIndicator"/> в <see cref="IServiceProvider"/> при каждом
	/// вызове. Это ухудшает производительность и может привести к неожидаемому поведению, в случае изменения конфигурации
	/// в <see cref="IServiceProvider"/> между двумя вызовами <see cref="ProgressIndicator"/>.
	/// <para>Смотрите описание использования интерфейса <see cref="IProgressIndicator"/> для более детальной информации.</para></remarks>
	public sealed class ProgressIndicator {
		static IProgressIndicator nullIndicator;
		

		/// <summary>Защищенный конструктор, исключающий создание экземпляра класса <see cref="ProgressIndicator"/></summary>
		ProgressIndicator() {}
		
		/// <summary>Сообщить текущему <see cref="IProgressIndicator"/> о начавшейся операции.</summary>
		/// <param name="sp"><see cref="System.IServiceProvider"/>, используемый для поиска текущего <see cref="IProgressIndicator"/>.</param>
		/// <param name="source">Компонент выполняющий операцию (или управляющий ею).</param>
		/// <param name="message">Текстовое сообщение, описывающее операцию.</param>
		/// <param name="canProgress">Информация о том, может ли компонент А вычислять прогресс операции.</param>
		/// <param name="canAbort">Информация о том, можно ли эту операцию прерывать.</param>
		/// <returns>Ключ зарегистрированной операции.</returns>
		/// <exception cref="ArgumentNullException">Если <b>source</b> равен <c>null</c>.</exception>
		/// <remarks>Ключ, который возвращает эта ф-ия может быть передан в методы <see cref="Finished"/> для
		/// указания какая именно операция завершилась.<para> Если <c>message</c> равен null, или пустоц строке
		/// <see cref="IProgressIndicator"/> может заменить сообщение на сообщение по-умолчанию.</para></remarks>
		/// <seealso cref="Finished"/>
		/// <seealso cref="Progress"/>
		public static object Started(IServiceProvider sp, object source, string message, bool canProgress, bool canAbort) {
			return GetIndicator(sp).Started(source, message, canProgress, canAbort);
		}
		
		/// <summary>Сообщить текущему <see cref="IProgressIndicator"/> о том, что одна из зарегистрированных операций
		/// успешно завершилась.</summary>
		/// <param name="sp"><see cref="System.IServiceProvider"/>, используемый для поиска текущего <see cref="IProgressIndicator"/>.</param>
		/// <param name="key">Ключ, полученный при регистрации операции.</param>
		/// <remarks>Если операции с соответствующим ключем не зарегистрировано (например, она уже завершена), вызов
		/// игнорируется. </remarks>
		/// <seealso cref="Started"/>
		/// <seealso cref="Canceled"/>
		/// <seealso cref="Progress"/>
		public static void Finished(IServiceProvider sp, object key) {
			GetIndicator(sp).Finished(key, null);
		}

		/// <summary>Сообщить текущему <see cref="IProgressIndicator"/> о том, что одна из зарегистрированных операций
		/// успешно завершилась.</summary>
		/// <param name="sp"><see cref="System.IServiceProvider"/>, используемый для поиска текущего <see cref="IProgressIndicator"/>.</param>
		/// <param name="key">Ключ, полученный при регистрации операции.</param>
		/// <param name="message">Текстовое сообщение, комментирующее завершение операции. Может быть пустой строкой или null.</param>
		/// <remarks>Если операции с соответствующим ключем не зарегистрировано (например, она уже завершена), вызов
		/// игнорируется. </remarks>
		/// <seealso cref="Started"/>
		/// <seealso cref="Canceled"/>
		/// <seealso cref="Progress"/>
		public static void Finished(IServiceProvider sp, object key, string message) {
			GetIndicator(sp).Finished(key, message);
		}
		
		/// <summary>Сообщить текущему <see cref="IProgressIndicator"/> о том, что одна из зарегистрированных операций
		/// отменена пользователем.</summary>
		/// <param name="sp"><see cref="System.IServiceProvider"/>, используемый для поиска текущего <see cref="IProgressIndicator"/>.</param>
		/// <param name="key">Ключ, полученный при регистрации операции.</param>
		/// <remarks>Если операции с соответствующим ключем не зарегистрировано (например, она уже завершена), вызов
		/// игнорируется. </remarks>
		/// <seealso cref="Started"/>
		/// <seealso cref="Finished"/>
		/// <seealso cref="Progress"/>
		public static void Canceled(IServiceProvider sp, object key) {
			GetIndicator(sp).Canceled(key, null);
		}
		
		/// <summary>Сообщить текущему <see cref="IProgressIndicator"/> о том, что одна из зарегистрированных операций
		/// отменена пользователем.</summary>
		/// <param name="sp"><see cref="System.IServiceProvider"/>, используемый для поиска текущего <see cref="IProgressIndicator"/>.</param>
		/// <param name="key">Ключ, полученный при регистрации операции.</param>
		/// <param name="message">Текстовое сообщение, комментирующее завершение операции. Может быть пустой строкой или null.</param>
		/// <remarks>Если операции с соответствующим ключем не зарегистрировано (например, она уже завершена), вызов
		/// игнорируется. </remarks>
		/// <seealso cref="Started"/>
		/// <seealso cref="Finished"/>
		/// <seealso cref="Progress"/>
		public static void Canceled(IServiceProvider sp, object key, string message) {
			GetIndicator(sp).Canceled(key, message);
		}
		
		/// <summary>Сообщить текущему <see cref="IProgressIndicator"/> о прогрессе операции и узнать у него следует ли продолжать
		/// выполнение операции.</summary>
		/// <param name="sp"><see cref="System.IServiceProvider"/>, используемый для поиска текущего <see cref="IProgressIndicator"/>.</param>
		/// <param name="key">Ключ, полученный при регистрации операции.</param>
		/// <param name="percent">Значение от 0 до 100, указывающее процент завершенности операции.</param>
		/// <returns>Логическое значение, указывающее, стоит ли продожать выполнение операции или следует
		/// немедленно ее прекратить.</returns>
		/// <remarks>Если операции с соответствующим ключем не зарегистрировано (например, она уже завершена), вызов
		/// игнорируется. <para>Если операция была зарегистрирована вызовом <see cref="Started"/> со значением
		/// параметра <b>canAbort</b> равным <c>false</c>, то компонент выполняющий операцию может проигнорировать
		/// сигнал завершения операции.</para></remarks>
		/// <seealso cref="Started"/>
		/// <seealso cref="Finished"/>
		/// <seealso cref="Message"/>
		public static bool Progress(IServiceProvider sp, object key, byte percent) {
			return GetIndicator(sp).Progress(key, percent);
		}

		/// <summary>Предоставить текущему <see cref="IProgressIndicator"/> комментарий относительно хода операции.</summary>
		/// <param name="sp"><see cref="System.IServiceProvider"/>, используемый для поиска текущего <see cref="IProgressIndicator"/>.</param>
		/// <param name="key">Ключ, полученный при регистрации операции.</param>
		/// <param name="message">Текстовое сообщение, комментирующее завершение операции. Может быть пустой строкой или null.</param>
		/// <remarks>Если операции с соответствующим ключем не зарегистрировано (например, она уже завершена), вызов
		/// игнорируется. <para>Скорее всего, сообщение переданное <see cref="IProgressIndicator"/> будет каком-то образом
		/// показано пользователю, например изменением строки статуса приложения.</para></remarks>
		/// <seealso cref="Started"/>
		/// <seealso cref="Finished"/>
		/// <seealso cref="Progress"/>
		public static void Message(IServiceProvider sp, object key, string message) {
			GetIndicator(sp).Message(key, message);
		}

		/// <summary>Получить экземпляр объекта, реализовующего интерфейс <see cref="IProgressIndicator"/> и игнорирующий
		/// все вызовы.</summary>
		/// <value>Экземпляр внутреннего класса <b>NullIndicator</b>, который реализует все методы <see cref="IProgressIndicator"/>.</value>
		/// <remarks>Реализация класса <b>NullIndicator</b> такова, что он игнорирует все вызовы. Он не бросает исключений и
		/// может быть использован в виде заглушки во всех местах, где требуется интерфейс <see cref="IProgressIndicator"/>,
		/// в случае, если сама функциональность <see cref="IProgressIndicator"/> не нужна.</remarks>
		/// <seealso cref="Started"/>
		/// <seealso cref="Finished"/>
		/// <seealso cref="Progress"/>
		public static IProgressIndicator NullIndicator {
			get {
				if (nullIndicator == null) nullIndicator = new NullIndicator();
				return nullIndicator;
			}
		}

		/// <summary>Получить текущий <see cref="IProgressIndicator"/>.</summary>
		/// <returns><see cref="IProgressIndicator"/>, который определен в указанном <see cref="IServiceProvider"/> или
		/// <c>NullIndicator</c>, если сервис <see cref="IProgressIndicator"/> не определен.</returns>
		public static IProgressIndicator GetIndicator(IServiceProvider sp) {
			IProgressIndicator pi = sp.GetService(typeof(IProgressIndicator)) as IProgressIndicator;
			return (pi == null) ? NullIndicator : pi;
		}
	}
}
