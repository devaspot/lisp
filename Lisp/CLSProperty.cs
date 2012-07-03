using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Front.Lisp {

	public class CLSProperty : CLSMember {

		#region Protected Fields
		//.........................................................................
		protected Type InnerType;
		protected PropertyInfo InnerPropertyInfo;
		protected bool InnerIsStatic;
		protected FastMethodCallDelegate InnerGetter;
		protected FastMethodCallDelegate InnerSetter;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public CLSProperty(string name, Type type, MemberInfo[] properties, bool isStatic) {
			InnerIsStatic = isStatic;
			InnerType = type;
			InnerPropertyInfo = (PropertyInfo)properties[0];
			InnerName = name;
			for (int i = 0; i < properties.Length; i++) {
				PropertyInfo pi = (PropertyInfo)properties[i];
				ParameterInfo[] info = pi.GetIndexParameters();
				if (info.Length > 0) //it's an indexed property - we don't support field-style access
				{
					throw new LispException("Field-style access for indexed property  of: " + type.Name +
											  " not supported -" +
											  " use get_" + name + " and set_" + name + " functions instead");
				}
			}
			GetSetter();
			GetGetter();
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public FastMethodCallDelegate Getter {
			get { return GetGetter(); }
		}

		public FastMethodCallDelegate Setter {
			get { return GetSetter(); }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override object Invoke(params object[] args) {
			object target = null;

			bool isSet = InnerIsStatic ? (args.Length == 1) : (args.Length == 2);
			if (!InnerIsStatic) {				 // instance field gets target from first arg
				target = args[0];
			}
			if (isSet) {	 // an extra arg indicates a "set"
				object val = InnerIsStatic ? args[0] : args[1];
				if (InnerSetter != null)
					InnerSetter(target, new object[] { val });

				return val;
			} else
				return InnerGetter(target, new object[0]);

		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected override object GetValue() {
			if (InnerIsStatic) {
				return InnerPropertyInfo.GetValue(null, null);
			}
			return this;
		}

		protected override void SetValue(object val) {
			if (InnerIsStatic)
				InnerPropertyInfo.SetValue(0, val, null);
		}

		protected virtual FastMethodCallDelegate GetGetter() {
			if (InnerGetter == null) {
				MethodInfo mi = InnerPropertyInfo.GetGetMethod(true);
				if (mi != null)
					InnerGetter = FastMethodCallBuilder.Current.Build(mi);
			}

			return InnerGetter;
		}

		protected virtual FastMethodCallDelegate GetSetter() {
			if (InnerSetter == null) {
				MethodInfo mi = InnerPropertyInfo.GetSetMethod(true);
				if (mi != null)
					InnerSetter = FastMethodCallBuilder.Current.Build(mi);
			}

			return InnerSetter;
		}
		//.........................................................................
		#endregion

	}

	public static class DictionaryHelper {
		public static object GetHash(IDictionary dict, object key) {
			object result = null;
			if (dict != null)
				result = dict[key];

			return result;
		}

		public static object SetHash(IDictionary dict, object key, object value) {
			object result = null;
			if (dict != null)
				dict[key] = result =value;
			return result;
		}
	}
}