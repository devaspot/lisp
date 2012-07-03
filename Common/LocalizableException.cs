// $Id: LocalizableException.cs 1836 2006-07-26 11:00:47Z kostya $

using System;
using System.Collections;
using System.Resources;
using System.Reflection;
using System.Runtime.Serialization;
using System.Globalization;
using System.Text;

namespace Front {

	/// <summary>Наследник <see cref="Exception"/>, который готов к локализации в распределенном окружении.</summary>
	/// <remarks>Класс <see cref="LocalizableException"/>, предназначен для работы в распределенных системах,
	/// в случае, если необходима возможность разной локализации взаимодействующих объектов.<para>
	/// Основное преимущество в том, что ошибка, сгенерированная, например, в англоязычном окружении, при передаче в
	/// русскоязычное, может быть показана на русском языке, при наличии соответствующих ресурсов.</para>
	/// <para>При наследовании класса <see cref="LocalizableException"/> в сборках отличных от <b>Front.Common</b>,
	/// необходимо переопредеять метод <see cref="LoadString"/>.</para></remarks>
	[Serializable]
	public class LocalizableException : Exception {
		protected string InnerErrorCode;
		protected object[] InnerArguments;

		/// <summary>Код ошибки.</summary>
		public string ErrorCode { get { return InnerErrorCode; } }
		/// <summary>Параметры для подстановки в локализованную строку сообщения.</summary>
		public object[] Arguments { get { return InnerArguments; } }

		/// <summary>Инициализирует новый экземпляр <see cref="LocalizableException"/>.</summary>
		/// <remarks>Этот конструктор инициализирует <see cref="ErrorCode"/> значением <b>EUnknownError</b></remarks>
		public LocalizableException() : this("EUnknownError") { }

		/// <summary>Инициализирует новый экземпляр <see cref="LocalizableException"/> указанным 
		/// кодом ошибки и параметрами.</summary>
		/// <param name="errorCode">Код ошибки.</param>
		/// <param name="args">Параметры для подстановки в локализованное сообщение об ошибке.</param>
		/// <remarks> Если <paramref name="errorCode"/> равен null (Nothing в VB.NET) или пустой строке, 
		/// то будет принято значение EUnknownError.</remarks>
		public LocalizableException( string errorCode, params object[] args )
			: this(null, errorCode, args)
		{ }

		/// <summary>Инициализирует новый экземпляр <see cref="LocalizableException"/> указанным кодом ошибки,
		/// параметрами, и вложенным исключением.</summary>
		/// <param name="inner">Вложенное исключение.</param>
		/// <param name="errorCode">Код ошибки.</param>
		/// <param name="args">Параметры для подстановки в локализованное сообщение об ошибке.</param>
		/// <remarks> Если <paramref name="errorCode"/> равен null (Nothing в VB.NET) или пустой строке, 
		/// то будет принято значение EUnknownError.</remarks>
		public LocalizableException( Exception inner, string errorCode, params object[] args )
			: base(null, inner)
		{
			this.InnerErrorCode = errorCode;
			this.InnerArguments = args;
		}

		/// <summary>Инициализирует новый экземпляр исключения. Реализация шаблона <see cref="ISerializable"/>.</summary>
		protected LocalizableException(SerializationInfo info, StreamingContext context)
			: base(info, context) 
		{
			this.InnerErrorCode = info.GetString("ErrorCode");
			this.InnerArguments = info.GetValue("Arguments", typeof(object[])) as object[];
		}

		public override void GetObjectData( SerializationInfo info, StreamingContext context ) 
		{
			base.GetObjectData(info, context);
			info.AddValue("ErrorCode", InnerErrorCode);
			info.AddValue("Arguments", InnerArguments);
		}

		/* TODO DF0003: продумать и внедрить коды!
		/// <summary>Прочитать код ошибки.</summary>
		/// <value>Код ошибки.</value>
		/// <remarks>Код ошибки это строка, содержащая имя строкового ресурса с текстовым описанием ошибки.
		/// Код ошибки уникален в рамках конкретного наследника <see cref="LocalizableException"/>.</remarks>
		public virtual string ErrorCode {
			get {
				return (this.Message == null || this.Message.Length == 0) ? "EUnknownError" : base.Message;
			}
		}
		*/

		/// <summary>Прочитать текстовое сообщение об ошибке.</summary>
		/// <value>Текстовое сообщение об ошибке.</value>
		/// <remarks>Сообщение об ошибке не передается при сериализации исключения. Таким образом, если
		/// объект-источник исключения и объект-получатель работают в окружениях с различными настройками
		/// текущей культуры, получателем будет использована версия сообщения, локализованная для
		/// окружения получателя.
		/// <para>При наследовании класса <see cref="LocalizableException"/> в сборках отличных от <b>Front.Common</b>,
		/// необходимо переопределять метод <see cref="FormatLocalizedMessage"/>.</para>
		/// </remarks>
		public override string Message {
			get {
				//return String.Format(ErrorCode, Arguments);
				// Пока отключаем, надо будет доделать работу с ресурсами
				string localized = FormatLocalizedMessage(ErrorCode, Arguments);
				return (localized == null) ? ErrorCode : localized;
			}
		}

		/// <summary>Отформатировать сообщение используя текущую культуру.</summary>
		/// <returns>Отформатированное локализованое текстовое сообщения об ошибке.</returns>
		/// <remarks>Этот метод получает код ошибки и используя настройки текущего потока
		/// (<c>Thread.CurrentUICulture</c>) читает ресурсы в поисках локализованной строки
		/// формата сообщения. Так как наследники <see cref="LocalizableException"/> должны искать
		/// строку формата в своих ресурсах, этот метод необходимо переопределять в наследниках, если
		/// они используют собственные коды ошибок.</remarks>
		/// <param name="errorCode">Код ошибки для форматирования.</param>
		/// <param name="args">Параметры для подстановки в текстовое сообщение.</param>
		protected string FormatLocalizedMessage( string errorCode, params object[] args ) 
		{
			string s = LoadString(errorCode);
			return String.Format( ((s == null) ? errorCode : s), args);
		}

		/// <summary>Прочитать из ресурсов сборки, в которой определен данный класс строку с
		/// указанным идентификатором и текущей культурой UI.</summary>
		/// <returns>Строка в текущей UI культуре, или null, если строки с таким идентификатором не найдена.</returns>
		/// <remarks>Этот метод получает код ошибки и используя настройки текущего потока
		/// (<c>Thread.CurrentUICulture</c>) читает ресурсы в поисках локализованной строки
		/// формата сообщения. Так как наследники <see cref="LocalizableException"/> должны искать
		/// строку формата в своих ресурсах, этот метод необходимо переопределять в наследниках, если
		/// они используют собственные коды ошибок.</remarks>
		/// <param name="code">Код ошибки для форматирования.</param>
		protected virtual string LoadString(string code) {
			return RM.GetString(code);
		}

		public static string PackExceptionInfo(Exception ex, bool includeStack) {
			StringBuilder sb = new StringBuilder();
			while (ex != null) {
				sb.AppendFormat("{0}\n", (includeStack ? ex.ToString() : ex.Message));
				ex = ex.InnerException;
			}
			return sb.ToString();
		}
	}
}
