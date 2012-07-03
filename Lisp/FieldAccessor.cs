using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Front.Lisp {

	// TODO Переписать на DynamicMethod!
	public class FieldAccessor : MemberAccessorBase {

		#region Constructors
		//.........................................................................
		public FieldAccessor(Type type, string name, object[] o) : this(type, name) { }
		public FieldAccessor(Type type, string name) : base(type, name, null) { }
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public FieldInfo FieldInfo {
			get { return (FieldInfo)MemberInfo; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override object Invoke(params object[] args) {
			object result = null;
			object target = null;
			bool isSet = FieldInfo.IsStatic ? (args.Length == 1) : (args.Length == 2);
			if (!FieldInfo.IsStatic) 
				target = args[0];
			if (isSet) {
				object val = result = FieldInfo.IsStatic ? args[0] : args[1];
				FieldInfo.SetValue(target, val);
			} else
				result = FieldInfo.GetValue(target);

			return result;
		}

		public override string ToString() {
			return string.Format("[Method: {0}]", base.ToString());
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected override MemberInfo RetrieveMember(Type t, string name, Type[] args) {
			MemberInfo mi = null;
			if (t != null && name != null) {
				mi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic |
					BindingFlags.Static | BindingFlags.Instance |
					BindingFlags.IgnoreCase);
			}

			return mi;
		}
		//.........................................................................
		#endregion
	}
}
