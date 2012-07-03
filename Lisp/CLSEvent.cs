using System;
using System.Reflection;

namespace Front.Lisp {

	public class CLSEvent : CLSMember {

		#region Protected Fields
		//.........................................................................
		protected Type InnerType;
		protected EventInfo InnerEventInfo;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public CLSEvent(string name, Type type, MemberInfo[] properties) {
			InnerEventInfo = (EventInfo)properties[0];
			InnerType = type;
			InnerName = name;
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override Object Invoke(params Object[] args) {
			bool isSet = (args.Length == 2);
			if (isSet) {
				// an extra arg indicates a "set"
				object val = args[1];
				MethodInfo adder = InnerEventInfo.GetAddMethod();
				ParameterInfo[] plist = adder.GetParameters();
				adder.Invoke(args[0], new object[] { val });
				return null; //val;
			} else
				return null; //pi.GetValue(target,null);

		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected override Object GetValue() {
			return this;
		}

		protected override void SetValue(Object val) { }
		//.........................................................................
		#endregion

	}
}