using System;
using System.Reflection;

namespace Front.Lisp {

	public class CLSField : CLSMember {

		#region Protected Fields
		//.........................................................................
		protected Type InnerType;
		protected FieldInfo InnerFieldInfo;
		protected bool InnerIsStatic;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public CLSField(string name, Type type, FieldInfo fi, bool isStatic) {
			this.InnerIsStatic = isStatic;
			this.InnerType = type;
			this.InnerFieldInfo = fi;
			this.InnerName = name;
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override Object Invoke(Object[] args) {
			Object target = null;
			Boolean isSet = InnerIsStatic ? (args.Length == 1) : (args.Length == 2);
			if (!InnerIsStatic) {				 // instance field gets target from first arg
				target = args[0];
			}
			if (isSet)		 // an extra arg indicates a "set"
			{
				Object val = InnerIsStatic ? args[0] : args[1];
				InnerFieldInfo.SetValue(target, val);
				return val;
			} else
				return InnerFieldInfo.GetValue(target);
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected override Object GetValue() {
			if (InnerIsStatic) {
				return InnerFieldInfo.GetValue(null);
			}

			return this;
		}

		protected override void SetValue(Object val) {
			if (InnerIsStatic)
				InnerFieldInfo.SetValue(null, val);
		}
		//.........................................................................
		#endregion


	}
}

