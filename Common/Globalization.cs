//$Id: Globalization.cs 129 2006-04-06 12:00:46Z pilya $

using System;
using System.Threading;
using System.Globalization;

namespace Front.Globalization {

	//TODO DF0022: использовать ContextSwitch<T>
	/// <summary>Публикатор UI культуры.</summary>
	/// <remarks>Операции по замене текущей культуры потока с возможностью восстановления старой,
	/// достаточно утомительны. <see cref="UICulturePublisher"/> облегчают эту задачу. Смотрите пример.
	/// </remarks>
	/// <example><code>
	///	try {
	/// 	// тут умолчательная культура, например en-US
	///		using (new UICulturePublisher("ru-RU")) {
	///			// тут ru-RU
	///			try {
	///				throw new LocalizableException();
	///			} catch (LocalizableException e) {
	///				// в консоль выводится сообщени на русском языке
	///				Console.WriteLine(e.Message);
	///				throw;
	///			}
	///		}
	///		// тут опять умолчательная культура
	///	} catch (LocalizableException e) {
	///		// выводится второе сообщение - на английском языке
	///		Console.WriteLine(e.Message);
	///	}
	/// </code></example>
	public class UICulturePublisher : IDisposable {
		CultureInfo previous;

		/// <summary>Проинициализировать новый <see cref="UICulturePublisher"/>.</summary>
		/// <param name="cultureName">Имя новой UI культуры.</param>
		/// <remarks>Конструктор автоматически устанавлевает культуру в
		/// <see cref="System.Threading.Thread.CurrentUICulture"/> и в методе <see cref="Dispose"/>
		/// возвращает предыдущее значение культуры, делаю очень удобной впеменную смену культуры с использованием
		/// ключевого слова using.</remarks>
		/// <exception cref="ArgumentNullException">Если <c>cultureName</c> равен null (Nothing в Visual Basic).</exception>
		public UICulturePublisher(string cultureName):this(new CultureInfo(cultureName)) { }

		/// <summary>Проинициализировать новый <see cref="UICulturePublisher"/>.</summary>
		/// <param name="c">Новая UI культура.</param>
		/// <remarks>Конструктор автоматически устанавлевает культуру в
		/// <see cref="System.Threading.Thread.CurrentUICulture"/> и в методе <see cref="Dispose"/>
		/// возвращает предыдущее значение культуры, делаю очень удобной впеменную смену культуры с использованием
		/// ключевого слова using.</remarks>
		/// <exception cref="ArgumentNullException">Если <c>c</c> равен null (Nothing в Visual Basic).</exception>
		public UICulturePublisher(CultureInfo c) {
			if (c == null) throw new ArgumentNullException("c");
			previous = Thread.CurrentThread.CurrentUICulture;
			Thread.CurrentThread.CurrentUICulture = c;
		}

		/// <summary>Восстановить предыдущую культуру.</summary>
		/// <remarks>Конструктор автоматически устанавлевает культуру в
		/// <see cref="System.Threading.Thread.CurrentUICulture"/> и в методе <see cref="Dispose"/>
		/// возвращает предыдущее значение культуры, делаю очень удобной впеменную смену культуры с использованием
		/// ключевого слова using.</remarks>
		public void Dispose() {
			Thread.CurrentThread.CurrentUICulture = previous;
		}
	}

	/// <summary>Публикатор культуры.</summary>
	/// <remarks>Операции по замене текущей культуры потока с возможностью восстановления старой,
	/// достаточно утомительны. <see cref="CulturePublisher"/> облегчают эту задачу. Смотрите пример для
	/// <see cref="UICulturePublisher"/>.
	/// </remarks>
	public class CulturePublisher : IDisposable {
		CultureInfo previous;

		/// <summary>Проинициализировать новый <see cref="CulturePublisher"/>.</summary>
		/// <param name="cultureName">Имя новой культуры.</param>
		/// <remarks>Конструктор автоматически устанавлевает культуру в
		/// <see cref="System.Threading.Thread.CurrentCulture"/> и в методе <see cref="Dispose"/>
		/// возвращает предыдущее значение культуры, делаю очень удобной впеменную смену культуры с использованием
		/// ключевого слова using.</remarks>
		/// <exception cref="ArgumentNullException">Если <c>cultureName</c> равен null (Nothing в Visual Basic).</exception>
		public CulturePublisher(string cultureName):this(new CultureInfo(cultureName)) { }

		/// <summary>Проинициализировать новый <see cref="CulturePublisher"/>.</summary>
		/// <param name="c">Новая культура.</param>
		/// <remarks>Конструктор автоматически устанавлевает культуру в
		/// <see cref="System.Threading.Thread.CurrentCulture"/> и в методе <see cref="Dispose"/>
		/// возвращает предыдущее значение культуры, делаю очень удобной впеменную смену культуры с использованием
		/// ключевого слова using.</remarks>
		/// <exception cref="ArgumentNullException">Если <c>c</c> равен null (Nothing в Visual Basic).</exception>
		public CulturePublisher(CultureInfo c) {
			if (c == null) throw new ArgumentNullException("c");
			previous = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = c;
		}

		/// <summary>Восстановить предыдущую культуру.</summary>
		/// <remarks>Конструктор автоматически устанавлевает культуру в
		/// <see cref="System.Threading.Thread.CurrentUICulture"/> и в методе <see cref="Dispose"/>
		/// возвращает предыдущее значение культуры, делаю очень удобной впеменную смену культуры с использованием
		/// ключевого слова using.</remarks>
		public void Dispose() {
			Thread.CurrentThread.CurrentCulture = previous;
		}
	}
	
}
