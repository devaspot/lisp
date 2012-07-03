using System;
using System.Reflection;

namespace Front.Lisp {

	public abstract class CLSMember : IFunction {

		#region Protected Fields
		//.........................................................................
		protected string InnerName;
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public object Value {
			get { return GetValue(); }
			set { SetValue(value); }
		}

		public string Name {
			get { return InnerName; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public abstract object Invoke(params object[] args);

		public override String ToString() {
			return "CLSMember:" + InnerName;
		}

		public static Object GetDefaultIndexedProperty(object target, object[] args) {
			Type targetType = target.GetType();
			object r = null;
			try {
				r = targetType.InvokeMember("", BindingFlags.Default | BindingFlags.GetProperty, null,
														 target, args);
			} catch (Exception e) {
			}
			return r;
		}

		public static Object SetDefaultIndexedProperty(object target, object[] args) {
			Type targetType = target.GetType();
			return targetType.InvokeMember("", BindingFlags.Default | BindingFlags.SetProperty, null,
													 target, args);
		}

		public static CLSMember FindMember(string name, Type type, bool isStatic) {
			if (type == null) {
				return new CLSLateBoundMember(name);
			}
			//lookup name in type, create approriate derivee
			MemberInfo[] members = type.GetMember(name,
															  BindingFlags.Public |
															  (isStatic ? BindingFlags.Static : BindingFlags.Instance)
															 ); //all public members with matching isstatic
			if (members.Length == 0) {
				throw new LispException("Can't find " +
										  (isStatic ? "static" : "instance") +
										  " member: " + name + " in Type: " + type.Name);
			}

			//CLS says all same-named members must be same type (field or param or method)
			//so just check first one

			if (members[0] is FieldInfo) {
				FieldInfo fi = (FieldInfo)members[0];
				return new CLSField(name, type, fi, fi.IsStatic);
			} else if (members[0] is PropertyInfo) {
				PropertyInfo pi = (PropertyInfo)members[0];
				//why doesn't PropertyInfo have IsStatic?
				MethodInfo mi = pi.GetGetMethod();
				return new CLSProperty(name, type, members, mi.IsStatic);
			} else if (members[0] is MethodInfo) {
				MethodInfo mi = (MethodInfo)members[0];
				return new CLSMethod(name, type, members, mi.IsStatic);
			} else if (members[0] is EventInfo) {
				EventInfo mi = (EventInfo)members[0];
				return new CLSEvent(name, type, members);
			} else {
				throw new LispException("Unsupported type of member: " + name + " in Type: "
										  + type.Name + " MemberType: " + members[0].MemberType);
			}
		} 
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual object GetValue() {
			return this;
		}

		protected virtual void SetValue(object val) {
			throw new LispException("Can't set value of member symbol");
		}
		//.........................................................................
		#endregion
	}
}
