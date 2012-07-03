using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Remoting.Messaging;

namespace ILS.Logging {
	public class LogPropertyBag : MarshalByRefObject, ILogicalThreadAffinative {
		LogPropertyBag() {
		}

		public override object InitializeLifetimeService() {
			return null;
		}

		static Hashtable hash = new Hashtable();

		public void SetValue(string name, object value) {
			hash[name] = value;
		}

		public object GetValue(string name) {
			return hash[name];
		}

		public static LogPropertyBag Current {
			get {
				LogPropertyBag bag = (LogPropertyBag)CallContext.GetData(typeof(LogPropertyBag).FullName);
				if (bag == null) {
					bag = new LogPropertyBag();
					CallContext.SetData(typeof(LogPropertyBag).FullName, bag);
				}
				return bag;
			}
		}

		public static void Delete() {
			CallContext.FreeNamedDataSlot(typeof(LogPropertyBag).FullName);
		}

		public static LogPropertyBag CreateNew() {
			LogPropertyBag bag = new LogPropertyBag();
			CallContext.SetData(typeof(LogPropertyBag).FullName, bag);
			return bag;			
		}
	}
}
