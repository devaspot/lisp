// $Id: Log.cs 204 2006-04-12 10:33:03Z pilya $

#define	TRACE

using System;
using System.Diagnostics;

namespace Front.Diagnostics {

	// TODO DF0013: не уверен в нужности этого класса. стоит посмотреть на Log4Net
	/// <summary>Provides a set of methods and properties that help you trace the execution of your code.</summary>
	/// <threadsafety static="true" instance="true"/>
	public class Log {
		static TraceSwitch  defaultTraceSwitch = null;
		static Log          defaultLog = null;
		TraceSwitch         traceSwitch;
		public bool			recurseException = true;
		public bool			ShowStack = false;

		static Log() {
			defaultTraceSwitch = new TraceSwitch("Default", RM.GetString("DefSwitchDesc"));
			defaultLog = new Log(defaultTraceSwitch);
		}

		/// <summary>Получить лог, который основан на <see cref="TraceSwitch"/> "Default".</summary>
		/// <value>Лог, который основан на <see cref="TraceSwitch"/> "Default".</value>
		public static Log Default {
			get { return defaultLog; }
		}

		/// <summary>Инициализирует новый <see cref="Log"/>, который открывает <see cref="TraceSwitch"/> с именем
		/// <c>displayName</c></summary>
		/// <param name="displayName">Имя <see cref="TraceSwitch"/>, на котором будет основан этот <see cref="Log"/>.</param>
		public Log(string displayName):this(displayName, displayName) {}

		/// <summary>Инициализирует новый <see cref="Log"/>, который открывает <see cref="TraceSwitch"/> с именем
		/// <c>displayName</c> и указанным описанием сообщений.</summary>
		/// <param name="displayName">Имя <see cref="TraceSwitch"/>, на котором будет основан этот <see cref="Log"/>.</param>
		/// <param name="description">Описание сообщений, которые выводит этот <see cref="Log"/>.</param>
		public Log(string displayName, string description) : this(new TraceSwitch(displayName, description, defaultTraceSwitch.Level.ToString())) {
		}

		/// <summary>Инициализирует новый <see cref="Log"/>, который основан на переданном <see cref="TraceSwitch"/>.</summary>
		/// <param name="sw"><see cref="TraceSwitch"/>, на котором будет основан этот <see cref="Log"/>.</param>
		public Log(TraceSwitch sw) {
			traceSwitch = sw;
		}

		/// <summary>Вывести в лог сообщение об ошибке.</summary>
		/// <param name="value">Объект, строковое представление которого будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Error</c>, <c>TraceLevel.Warning</c>, <c>TraceLevel.Info</c> или <c>TraceLevel.Verbose</c>.
		/// </remarks>
		/// <seealso cref="Warn"/>
		/// <seealso cref="Info"/>
		/// <seealso cref="Verb"/>
		public void Fail(object value) {
			WriteLineIf(traceSwitch.TraceError, value);
		}

		/// <summary>Вывести в лог сообщение об ошибке.</summary>
		/// <param name="message">Сообщение, которое будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Error</c>, <c>TraceLevel.Warning</c>, <c>TraceLevel.Info</c> или <c>TraceLevel.Verbose</c>.
		/// </remarks>
		/// <seealso cref="Warn"/>
		/// <seealso cref="Info"/>
		/// <seealso cref="Verb"/>
		public void Fail(string message) {
			WriteLineIf(traceSwitch.TraceError, "Error: " + message);
		}

		/// <summary>Вывести в лог сообщение об ошибке.</summary>
		/// <param name="e"><see cref="Exception"/>, который будет выведен в лог.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Error</c>, <c>TraceLevel.Warning</c>, <c>TraceLevel.Info</c> или <c>TraceLevel.Verbose</c>.
		/// <para>В зависимости от парамертра <see cref="RecurseException"/> в лог выведется только одно сообщение об
		/// ошибки или будут выведены и основное и <see cref="Exception.InnerException"/>.</para>
		/// </remarks>
		/// <seealso cref="Warn"/>
		/// <seealso cref="Info"/>
		/// <seealso cref="Verb"/>
		public void Fail(Exception e) {
			WriteLineIf(traceSwitch.TraceError, e);
		}

		/// <summary>Вывести в лог сообщение об ошибке.</summary>
		/// <param name="format">Форматирующая строка для сообщение, которое будет выведено в лог.</param>
		/// <param name="p">Парамерты, которые будут использованы при форматировании сообщения.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Error</c>, <c>TraceLevel.Warning</c>, <c>TraceLevel.Info</c> или <c>TraceLevel.Verbose</c>.
		/// </remarks>
		/// <seealso cref="Warn"/>
		/// <seealso cref="Info"/>
		/// <seealso cref="Verb"/>
		public void Fail(string format, params object[] p) {
			WriteLineIf(traceSwitch.TraceError, "Error: " + format, p);
		}

		/// <summary>Вывести в лог сообщение о предупреждении.</summary>
		/// <param name="value">Объект, строковое представление которого будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Warning</c>, <c>TraceLevel.Info</c> или <c>TraceLevel.Verbose</c>.
		/// </remarks>
		/// <seealso cref="Fail"/>
		/// <seealso cref="Info"/>
		/// <seealso cref="Verb"/>
		public void Warn(object value) {
			WriteLineIf(traceSwitch.TraceWarning, value);
		}

		/// <summary>Вывести в лог сообщение о предупреждении.</summary>
		/// <param name="message">Сообщение, которое будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Warning</c>, <c>TraceLevel.Info</c> или <c>TraceLevel.Verbose</c>.
		/// </remarks>
		/// <seealso cref="Fail"/>
		/// <seealso cref="Info"/>
		/// <seealso cref="Verb"/>
		public void Warn(string message) {
			WriteLineIf(traceSwitch.TraceWarning, "Warning: " + message);
		}

		/// <summary>Вывести в лог сообщение о предупреждении.</summary>
		/// <param name="format">Форматирующая строка для сообщение, которое будет выведено в лог.</param>
		/// <param name="p">Параметры, которые будут использованы при форматировании сообщения.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Warning</c>, <c>TraceLevel.Info</c> или <c>TraceLevel.Verbose</c>.
		/// </remarks>
		/// <seealso cref="Fail"/>
		/// <seealso cref="Info"/>
		/// <seealso cref="Verb"/>
		public void Warn(string format, params object[] p) {
			WriteLineIf(traceSwitch.TraceWarning, "Warning: " + format, p);
		}

		/// <summary>Вывести в лог информационное сообщение.</summary>
		/// <param name="value">Объект, строковое представление которого будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Info</c> или <c>TraceLevel.Verbose</c>.
		/// </remarks>
		/// <seealso cref="Fail"/>
		/// <seealso cref="Warn"/>
		/// <seealso cref="Verb"/>
		public void Info(object value) {
			WriteLineIf(traceSwitch.TraceInfo, value);
		}

		/// <summary>Вывести в лог информационное сообщение.</summary>
		/// <param name="message">Сообщение, которое будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Info</c> или <c>TraceLevel.Verbose</c>.
		/// </remarks>
		/// <seealso cref="Fail"/>
		/// <seealso cref="Warn"/>
		/// <seealso cref="Verb"/>
		public void Info(string message) {
			WriteLineIf(traceSwitch.TraceInfo, message);
		}

		/// <summary>Вывести в лог информационное сообщение.</summary>
		/// <param name="format">Форматирующая строка для сообщение, которое будет выведено в лог.</param>
		/// <param name="p">Парамерты, которые будут использованы при форматировании сообщения.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Info</c> или <c>TraceLevel.Verbose</c>.
		/// </remarks>
		/// <seealso cref="Fail"/>
		/// <seealso cref="Warn"/>
		/// <seealso cref="Verb"/>
		public void Info(string format, params object[] p) {
			WriteLineIf(traceSwitch.TraceInfo, format, p);
		}

		/// <summary>Вывести в лог сообщение, содержащее подробную информацию.</summary>
		/// <param name="value">Объект, строковое представление которого будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Verbose</c>. Обычно, этот уровень подробности логов применяется только для
		/// отладки приложения.</remarks>
		/// <seealso cref="Fail"/>
		/// <seealso cref="Warn"/>
		/// <seealso cref="Info"/>
		public void Verb(object value) {
			WriteLineIf(traceSwitch.TraceVerbose, value);
		}

		/// <summary>Вывести в лог сообщение, содержащее подробную информацию.</summary>
		/// <param name="message">Сообщение, которое будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Verbose</c>. Обычно, этот уровень подробности логов применяется только для
		/// отладки приложения.</remarks>
		/// <seealso cref="Fail"/>
		/// <seealso cref="Warn"/>
		/// <seealso cref="Info"/>
		public void Verb(string message) {
			WriteLineIf(traceSwitch.TraceVerbose, message);
		}

		/// <summary>Вывести в лог сообщение, содержащее подробную информацию.</summary>
		/// <param name="format">Форматирующая строка для сообщение, которое будет выведено в лог.</param>
		/// <param name="p">Парамерты, которые будут использованы при форматировании сообщения.</param>
		/// <remarks>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Verbose</c>. Обычно, этот уровень подробности логов применяется только для
		/// отладки приложения.</remarks>
		/// <seealso cref="Fail"/>
		/// <seealso cref="Warn"/>
		/// <seealso cref="Info"/>
		public void Verb(string format, params object[] p) {
			WriteLineIf(traceSwitch.TraceVerbose, format, p);
		}

		/// <summary>Вывести в лог сообщение о вызове метода.</summary>
		/// <param name="format">предположительно название метода.</param>
		/// <param name="p">Параметры, которые будут использованы при форматировании сообщения.</param>
		/// <remarks><para>Сообщение будет выведено в лог только в том случае, если <see cref="TraceLevel"/> будет
		/// равен <c>TraceLevel.Verbose</c>. Обычно, этот уровень подробности логов применяется только для
		/// отладки приложения.</para>
		/// <para>Сообщение формируется из <c>TraceSwitch.DisplayName</c> и переданой строки. Предполагается,
		/// что <c>TraceSwitch.DisplayName</c> соответствует имени класса (чаще всего так и есть).</para></remarks>
		/// <seealso cref="Fail"/>
		/// <seealso cref="Warn"/>
		/// <seealso cref="Info"/>
		public void Method(string format, params object[] p) {
			// TODO: получить название класса и метода из StackTrace
			Verb(String.Format("[{0}].{1}", traceSwitch.DisplayName, format), p);
		}

		/// <summary>Вывести в лог указанное сообщение.</summary>
		/// <param name="value">Объект, строковое представление которого будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в любом случае. Для условного вывода сообщений рассмотрите
		/// методы <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="WriteIf"/>
		/// <seealso cref="WriteLine"/>
		/// <seealso cref="WriteLineIf"/>
		public virtual void Write(object value) {
			WriteIf(traceSwitch.TraceVerbose, value);
		}

		/// <summary>Вывести в лог указанное сообщение.</summary>
		/// <param name="message">Сообщение, которое будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в любом случае. Для условного вывода сообщений рассмотрите
		/// методы <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="WriteIf"/>
		/// <seealso cref="WriteLine"/>
		/// <seealso cref="WriteLineIf"/>
		public virtual void Write(string message) {
			WriteIf(traceSwitch.TraceVerbose, message);
		}

		/// <summary>Вывести в лог указанное сообщение.</summary>
		/// <param name="format">Форматирующая строка для сообщение, которое будет выведено в лог.</param>
		/// <param name="p">Парамерты, которые будут использованы при форматировании сообщения.</param>
		/// <remarks>Сообщение будет выведено в любом случае. Для условного вывода сообщений рассмотрите
		/// методы <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="WriteIf"/>
		/// <seealso cref="WriteLine"/>
		/// <seealso cref="WriteLineIf"/>
		public virtual void Write(string format, params object[] p) {
			WriteIf(traceSwitch.TraceVerbose, format, p);
		}

		/// <summary>Вывести в лог указанное сообщение с добавлением перевода каретки.</summary>
		/// <param name="value">Объект, строковое представление которого будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в любом случае. Для условного вывода сообщений рассмотрите
		/// методы <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="Write"/>
		/// <seealso cref="WriteIf"/>
		/// <seealso cref="WriteLineIf"/>
		public virtual void WriteLine(object value) {
			WriteLineIf(traceSwitch.TraceVerbose, value);
		}

		/// <summary>Вывести в лог указанное сообщение с добавлением перевода каретки.</summary>
		/// <param name="e"><see cref="Exception"/>, который будет выведен в лог.</param>
		/// <remarks>Сообщение будет выведено в любом случае. Для условного вывода сообщений рассмотрите
		/// методы <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="Write"/>
		/// <seealso cref="WriteIf"/>
		/// <seealso cref="WriteLineIf"/>
		public virtual void WriteLine(Exception e) {
			WriteLineIf(traceSwitch.TraceVerbose, e);
		}

		/// <summary>Вывести в лог указанное сообщение с добавлением перевода каретки.</summary>
		/// <param name="format">Форматирующая строка для сообщение, которое будет выведено в лог.</param>
		/// <param name="p">Парамерты, которые будут использованы при форматировании сообщения.</param>
		/// <remarks>Сообщение будет выведено в любом случае. Для условного вывода сообщений рассмотрите
		/// методы <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="Write"/>
		/// <seealso cref="WriteIf"/>
		/// <seealso cref="WriteLineIf"/>
		public virtual void WriteLine(string format, params object[] p) {
			WriteLineIf(traceSwitch.TraceVerbose, format, p);
		}

		/// <summary>Вывести в лог указанное сообщение с добавлением перевода каретки.</summary>
		/// <param name="message">Сообщение, которое будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в любом случае. Для условного вывода сообщений рассмотрите
		/// методы <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="Write"/>
		/// <seealso cref="WriteIf"/>
		/// <seealso cref="WriteLineIf"/>
		public virtual void WriteLine(string message) {
			WriteLineIf(traceSwitch.TraceVerbose, message);
		}

		# region Условная запись
		/// <summary>Вывести в лог указанное сообщение.</summary>
		/// <param name="condition">Условие, при котором сообщение будет выведено в лог.</param>
		/// <param name="value">Объект, строковое представление которого будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в только в том случае, если <c>condition = true.</c>
		/// Рассмотрите также вариант использования для условного вывода сообщений 
		/// методов <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="Write"/>
		/// <seealso cref="WriteLine"/>
		/// <seealso cref="WriteLineIf"/>
		public void WriteIf(bool condition, object value) {
			string message = (value == null) ? "<null>" : value.ToString();
			WriteIf(condition, message);
		}

		/// <summary>Вывести в лог указанное сообщение.</summary>
		/// <param name="condition">Условие, при котором сообщение будет выведено в лог.</param>
		/// <param name="format">Форматирующая строка для сообщение, которое будет выведено в лог.</param>
		/// <param name="p">Парамерты, которые будут использованы при форматировании сообщения.</param>
		/// <remarks>Сообщение будет выведено в только в том случае, если <c>condition = true.</c>
		/// Рассмотрите также вариант использования для условного вывода сообщений 
		/// методов <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="Write"/>
		/// <seealso cref="WriteLine"/>
		/// <seealso cref="WriteLineIf"/>
		public void WriteIf(bool condition, string format, params object[] p) {
			string message = (p != null) ? String.Format(format, p) : format;
			WriteIf(condition, message);
		}

		/// <summary>Вывести в лог указанное сообщение с добавлением перевода каретки.</summary>
		/// <param name="condition">Условие, при котором сообщение будет выведено в лог.</param>
		/// <param name="value">Объект, строковое представление которого будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в только в том случае, если <c>condition = true.</c>
		/// Рассмотрите также вариант использования для условного вывода сообщений 
		/// методов <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="Write"/>
		/// <seealso cref="WriteIf"/>
		/// <seealso cref="WriteLine"/>
		public void WriteLineIf(bool condition, object value) {
			string message = (value == null) ? "<null>" : value.ToString();
			WriteLineIf(condition, message);
		}

		/// <summary>Вывести в лог указанное сообщение с добавлением перевода каретки.</summary>
		/// <param name="condition">Условие, при котором сообщение будет выведено в лог.</param>
		/// <param name="e"><see cref="Exception"/>, который будет выведен в лог.</param>
		/// <remarks>Сообщение будет выведено в только в том случае, если <c>condition = true.</c>
		/// Рассмотрите также вариант использования для условного вывода сообщений 
		/// методов <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="Write"/>
		/// <seealso cref="WriteIf"/>
		/// <seealso cref="WriteLine"/>
		public void WriteLineIf(bool condition, Exception e) {
			string message = (e != null) ? ExceptionToString(e) : "<null>";
			WriteLineIf(condition, message);
		}

		/// <summary>Вывести в лог указанное сообщение с добавлением перевода каретки.</summary>
		/// <param name="condition">Условие, при котором сообщение будет выведено в лог.</param>
		/// <param name="format">Форматирующая строка для сообщение, которое будет выведено в лог.</param>
		/// <param name="p">Парамерты, которые будут использованы при форматировании сообщения.</param>
		/// <remarks>Сообщение будет выведено в только в том случае, если <c>condition = true.</c>
		/// Рассмотрите также вариант использования для условного вывода сообщений 
		/// методов <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="Write"/>
		/// <seealso cref="WriteIf"/>
		/// <seealso cref="WriteLine"/>
		public void WriteLineIf(bool condition, string format, params object[] p) {
			// XXX это чревато Exception'ами при форматировании!
			string message = (p != null) ? String.Format(format, p) : format;
			WriteLineIf(condition, message);
		}

		/// <summary>Вывести в лог указанное сообщение.</summary>
		/// <param name="condition">Условие, при котором сообщение будет выведено в лог.</param>
		/// <param name="message">Сообщение, которое будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в только в том случае, если <c>condition = true.</c>
		/// Рассмотрите также вариант использования для условного вывода сообщений 
		/// методов <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="Write"/>
		/// <seealso cref="WriteLine"/>
		/// <seealso cref="WriteLineIf"/>
		public void WriteIf(bool condition, string message) {
			if (condition) Trace.Write(message);
		}

		/// <summary>Вывести в лог указанное сообщение с добавлением перевода каретки.</summary>
		/// <param name="condition">Условие, при котором сообщение будет выведено в лог.</param>
		/// <param name="message">Сообщение, которое будет выведено в лог.</param>
		/// <remarks>Сообщение будет выведено в только в том случае, если <c>condition = true.</c>
		/// Рассмотрите также вариант использования для условного вывода сообщений 
		/// методов <see cref="Fail"/>, <see cref="Warn"/>, <see cref="Info"/> и <see cref="Verb"/>.</remarks>
		/// <seealso cref="Write"/>
		/// <seealso cref="WriteIf"/>
		/// <seealso cref="WriteLine"/>
		public void WriteLineIf(bool condition, string message) {
			if (condition) 
				Trace.WriteLine(message);
		}
		#endregion

		/// <summary>Получить или установить уровень подробности логов.</summary>
		/// <value>Значение <see cref="TraceLevel"/>, указывающее текущий уровень логов.</value>
		/// <remarks>При создании экземрляра <see cref="Front.Diagnostics.Log"/>, уровень логов соответствует
		/// свойству <c>TraceLevel</c> объекта <see cref="TraceSwitch"/>, на
		/// котором основан <see cref="Front.Diagnostics.Log"/>.</remarks>
		public TraceLevel Level {
			get { return traceSwitch.Level; }
			set { traceSwitch.Level = value; }
		}

		/// <summary>Получить или установить значение указывающее, будет ли производится рекурсивный поиск
		/// вложенных исключений при выводе в лог <see cref="Exception"/>.</summary>
		/// <value>true, если при выводе логов будет производится поиск вложенных исключений; иначе false</value>
		public bool RecurseException {
			get { return recurseException; }
			set { recurseException = value; }
		}

		/// <summary>Получить строковое представление объекта <see cref="Exception"/>.</summary>
		/// <value>Строковое представление объекта <see cref="Exception"/>.</value>
		/// <remarks>Если свойство <see cref="RecurseException"/> имеет значение <c>false</c>, то этот
		/// метод вернет результат вызова <see cref="Exception.ToString"/> у объекта <c>e</c>.
		/// Иначе, результат будет содержать все вложенные исключения.</remarks>
		/// <exception cref="ArgumentNullException">Если <c>e</c> равно <c>null</c>.</exception>
		protected virtual string ExceptionToString(Exception e) 
		{
			if (e == null) 
				throw new ArgumentNullException("e");
			if (recurseException) {
				System.Text.StringBuilder sb = new System.Text.StringBuilder(400);
				while (e != null) {
					sb.AppendFormat("{0}", (ShowStack) ? e.ToString(): e.Message );
					
					e = e.InnerException;
					if (e != null)
						sb.AppendFormat("\n{0}\n", RM.GetString("LogNestedException"));
				}
				return sb.ToString();
			} else
				return e.ToString();
		}

		public virtual void Indent() {
			Trace.Indent();
		}

		public virtual void Unindent() {
			Trace.Unindent();
		}
	}
}

