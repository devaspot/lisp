using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;

using Front.Diagnostics;
using System.Diagnostics;

namespace Front {

	public enum ErrorLevel {
		Notification   = 10,
		Warning        = 30,
		Critical       = 40,
		Fatal          = 50
	}

	public struct ErrorInfo {
		public int			ErrorLevel;
		public Exception	Exception;
		public Type			Module;
		public string		CallStack;

		public override string ToString() {
			return string.Format("Exception: {0}\r\nErrorLevel: {1}\r\nModule: {2}\r\nCall Stack:\r\n{3}\r\n",
				Exception, ErrorLevel, Module, CallStack);
		}
	}

	public class ErrorEventArgs : EventArgs {
		protected Exception InnerException;

		public ErrorEventArgs(Exception ex) {
			InnerException = ex;
		}

		public virtual Exception Exception {
			get { return InnerException;}
			set { InnerException = value; }
		}
	}
	

	//................................................................................
	public static class Error {

		private static ErrorPolicy  policy;
		private static Log  Log = new Log(new TraceSwitch("Front.Error", "Front.Error", "Warning"));

		internal static ICollection<ErrorInfo> emptyList = 
			new Collection<ErrorInfo>(new Collection<ErrorInfo>());

		// TODO:	ѕодумать, как и когда правильно инициализировать policy
		static Error() {
			policy = System.Configuration.ConfigurationManager.GetSection("errorConfig") as ErrorPolicy;
		}

		public static void SetPolicy(ErrorPolicy errorPolicy) {
			policy = errorPolicy;
		}

		public static object Fatal( Exception ex, Type module ) {
			return LogThrow(ex, module, (int)ErrorLevel.Fatal);
		}

		public static object Fatal( string message, Type module ) {
			return LogThrow(new Exception(message), module, (int)ErrorLevel.Fatal);
		}

		public static object Critical( Exception ex, Type module ) {
			return LogThrow(ex, module, (int)ErrorLevel.Critical);
		}

		public static object Critical( string message, Type module ) {
			return LogThrow(new Exception(message), module, (int)ErrorLevel.Critical);
		}

		public static object Warning( Exception ex, Type module ) {
			return LogThrow(ex, module, (int)ErrorLevel.Warning);
		}

		public static object Warning( string message, Type module ) {
			return LogThrow(new Exception(message), module, (int)ErrorLevel.Warning);
		}

		public static object Notification( Exception ex, Type module ) {
			return LogThrow(ex, module, (int)ErrorLevel.Notification);
		}

		public static object Notification( string message, Type module ) {
			return LogThrow(new Exception(message), module, (int)ErrorLevel.Notification);
		}


		public static object Throw( Exception ex, Type module, int level ) {
			return LogThrow(ex, module, level);
		}

		public static object LogThrow( Exception ex, Type module, int level ) 
		{
			// «агл€нуть в ErrorPolicy
			int policySilence = 0;

			ErrorPolicyRule policyRule = null;
			if (policy != null) {
				// TODO: сделать defaultRule....
				policySilence = policy.DefaultSilence;

				if (module != null && policy.ContainsKey(module.FullName)) {
					policyRule = policy[module.FullName];
					policySilence = policyRule.Level;
				}
			}

			string str = System.Environment.StackTrace;
			int i2 = str.IndexOf("\n");
			i2 = str.IndexOf("\n", i2 + 1);
			i2 = str.IndexOf("\n", i2 + 1);
			i2 = str.IndexOf("\n", i2 + 1);
			string stackTrace = str.Substring(i2 + 1);

			// XXX (Pilya) пробное решение. ’очу, что бы можно было шуметь, несмотр€ на общий Silence
			// дл€ этого указываем отрицательный Silence (значение считаетс€ по абсолютной величине)
			int actualSilence = (policySilence < 0 ) 
						? Math.Abs(policySilence) 
						: Math.Max(Silence, policySilence);

//			if (level < Silence || level < policySilence) {....
				// глушим и логаем

			SilenceKeeper sk = SilenceKeeper.Current;
			if (sk != null) {
				ErrorInfo errorInfo = new ErrorInfo();
				errorInfo.Exception = ex;
				errorInfo.Module = module;
				errorInfo.ErrorLevel = level;
				errorInfo.CallStack = stackTrace;
				sk.ErrorList.Add(errorInfo);
			}

			// TODO: использовать Log(Exception), а там реагировать на какой-нибудь флаг
			// дл€ раскрутки стека
			string msg = ( (policyRule != null && policyRule.ShowStack) || 
							level > actualSilence || Log.Level == TraceLevel.Verbose || 
							Log.Level == TraceLevel.Info)

				? (ex.GetType().ToString() + ": " + ex.Message + '\n' + stackTrace + "\nIgnored!")
				: (ex.GetType().ToString() + ": " + ex.Message + "\nIgnored!");
				
			Log.Fail( msg );
			if (policyRule != null && policyRule.ShowMessage)
				System.Windows.Forms.MessageBox.Show(msg);

			if (level > actualSilence)
				throw ex;			

			return null;
		}

		public static IEnumerable<ErrorInfo> ErrorList {
			get {
				SilenceKeeper sk = SilenceKeeper.Current;
				return (sk != null) ? sk.ErrorList : emptyList;  // возращать null плохо, т.к. foreach не провер€ет коллекцию на null. ѕоэтому возвращаем пустую коллекцию.
			}
		}

		public static SilenceKeeper KeepSilence( ErrorLevel silenceLevel ) {
			return KeepSilence((int)silenceLevel);
		}

		public static SilenceKeeper KeepSilence( int silenceLevel ) {
			return new SilenceKeeper(silenceLevel);
		}

		public static int Silence {
			get {

				SilenceKeeper cur = SilenceKeeper.Current;
				return (cur != null) ? cur.Level : (int)ErrorLevel.Critical;
			}
		}
	}
	//========================================================================//

	public class SilenceKeeperEventArgs : EventArgs {
		public ICollection<ErrorInfo> ErrorList;
		public SilenceKeeperEventArgs( ICollection<ErrorInfo> errList ) {
			ErrorList = errList;
		}
	}
	//========================================================================//

	public class SilenceKeeper : ContextSwitch<SilenceKeeper> 
	{
		protected int SilenceLevel;
		protected List<ErrorInfo>  InnerErrorList = new List<ErrorInfo>();
		
		public ICollection<ErrorInfo> ErrorList { get { return InnerErrorList; } }

		public static event EventHandler<SilenceKeeperEventArgs> OnBottom;

		public SilenceKeeper( int level ) {
			SilenceLevel = level;
			InnerValue = this;
			Publish();
		}

		public int Level { 
			get { return SilenceLevel; } 
		}

		public override void Dispose() {
			SilenceKeeper sk = SilenceKeeper.Current;
			if (sk != null)
			{
				if (previous != null)
				{
					SilenceKeeper prev = previous as SilenceKeeper;
					if (prev != null)
					{
						prev.InnerErrorList.AddRange(sk.ErrorList);
						sk.ErrorList.Clear();
					}
				}
				else {
					// верхнего уровн€ нет.
					EventHandler<SilenceKeeperEventArgs> e = OnBottom;
					if (e != null) 
						e(this, new SilenceKeeperEventArgs(ErrorList));
				}
			}
			base.Dispose();
		}
	}
	//========================================================================//

/*
*   ErrorPolicy - в этом классе будет хранитс€ таблица с дефолтными настройками уровн€ silence
*   дл€ разных модулей.  Ќужно сделать так, чтобы это таблицу можно было бы мен€ть/наращивать.
*   «агружать в тот или иной AppDomain.
* 
*/
	public class ErrorPolicyRule {
		public int Level;
		public int Verbose;
		public bool ShowStack;
		public bool ShowMessage;

		public ErrorPolicyRule(int level, int verbose, bool showStack, bool showMessage) {
			Level = level;
			Verbose = verbose;
			ShowStack = showStack;
			ShowMessage = showMessage;
		}

		public ErrorPolicyRule(int level) : this(level, 0, false, false) {}
	}

	public class ErrorPolicy : Dictionary<string, ErrorPolicyRule> {
		//private struct SilenceInfo
		//{
		//   public Type		type;
		//   public string	typeName;
		//   public int		level;
		//}
		//private List<SilenceInfo> silenceInfoList = new List<SilenceInfo>();
		
		private int	defaultSilence = (int)ErrorLevel.Critical;
		
		public int DefaultSilence {
			get { return defaultSilence; }
			set { defaultSilence = value; }
		}

		public void ApplyPolicy() {
			// применить данный ErrorPolicy во все AppDomains.
		}
	}
	//========================================================================//

	public class ErrorConfigHandler : IConfigurationSectionHandler 
	{
		public object Create( object parent, object configContext, XmlNode section )
		{
			ErrorPolicy errorPolicy = new ErrorPolicy();

			if (section.HasChildNodes)
				foreach (XmlNode child in section.ChildNodes) {
					if (child.NodeType == XmlNodeType.Element) {
						if (child.Name == "default") {
							string strDefLevel = child.Attributes.GetNamedItem("level").Value;
							errorPolicy.DefaultSilence = Convert.ToInt32(strDefLevel);
						} else if (child.Name == "silence") {
							string typeName = child.Attributes.GetNamedItem("module").Value;
							string strLevel = child.Attributes.GetNamedItem("level").Value;
							XmlNode shmgNode = child.Attributes.GetNamedItem("ShowMessage");
							bool showMessage = false;
							if (shmgNode != null) {
								Boolean.TryParse(shmgNode.Value, out showMessage);
							}

							int level = errorPolicy.DefaultSilence;

							switch (strLevel) {
								case "Warning":
									level = (int)ErrorLevel.Warning;
									break;
								case "Notification":
									level = (int)ErrorLevel.Notification;
									break;
								case "Critical":
									level = (int)ErrorLevel.Critical;
									break;
								case "Fatal":
									level = (int)ErrorLevel.Fatal;
									break;
								default:
									level = Convert.ToInt32(strLevel);
									break;
							}

							ErrorPolicyRule r1 = new ErrorPolicyRule(level);
							r1.ShowMessage = showMessage;
							errorPolicy.Add(typeName, r1);
						}
					}
				}
			return errorPolicy;
		}
	}
}
