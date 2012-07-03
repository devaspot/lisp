using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Front.Lisp {

	public class PropertyAccessor : MemberAccessorBase {

		#region Protected Fields
		//.........................................................................
		protected bool InnerIsStatic;
		protected Type InnerReturnType;
		protected FastMethodCallDelegate InnerGetter;
		protected FastMethodCallDelegate InnerSetter;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public PropertyAccessor(Type type, string name, object[] o) : this(type, name, null) { }
		public PropertyAccessor(Type type, string name, params Type[] args) : base(type, name, Util.VectorRest(args)) {
			InnerReturnType = args[0];
			GetGetter();
			GetSetter();
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public PropertyInfo PropertyInfo {
			get { return (PropertyInfo)MemberInfo; }
		}

		public bool IsStatic {
			get { return InnerIsStatic; }
		}

		public Type ReturnType {
			get { return InnerReturnType; }
		}

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
			object result = null;
			object target = null;

			bool isSet = IsStatic ? (args.Length == Args.Length + 1) : (args.Length == Args.Length + 2);
			if (!IsStatic)
				target = args[0];

			if (isSet) {
				result = SetValue(args, result, target);
			} else
				result = GetValue(args, target);

			return result;
		}

		protected virtual object GetValue(object[] args, object target) {
			object result = null;
			if (Getter != null) 
				result = Getter(target, GetIndex(args));

			return result;
		}

		protected virtual object SetValue(object[] args, object value, object target) {
			object result = null;
			if (Setter != null) {
				value = result = args[args.Length - 1];
				object[] parameters = null;
				object[] index = GetIndex(args);
				if (index != null && index.Length > 0) {
					parameters = new object[index.Length + 1];
					for (int i = 0; i < index.Length; i++)
						parameters[i] = index[i];
					parameters[index.Length] = value;
				} else 
					parameters = new object[] { value };

				Setter(target, parameters);
			}
			return result;
		}

		protected virtual object[] GetIndex(object[] args) {
			object[] index = new object[0];
			if (Args.Length > 0) {
				index = new object[Args.Length];
				int delta = IsStatic ? 0 : 1;
				for (int i = 0; i < Args.Length; i++)
					index[i] = args[i + delta];
			}

			return index;
		}

		public override string ToString() {
			return string.Format("[Property: {0}]", base.ToString());
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected override MemberInfo RetrieveMember(Type t, string name, Type[] args) {
			PropertyInfo pi = null;
			if (t != null && name != null) {
				pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
					BindingFlags.IgnoreCase,
					null, ReturnType, args, null);
				if (pi == null) {
					pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
					BindingFlags.IgnoreCase,
					null, ReturnType, args, null);
					if (pi != null)
						InnerIsStatic = true;
				}
			}

			return pi;
		}

		protected virtual FastMethodCallDelegate GetGetter() {
			if (InnerGetter == null) {
				MethodInfo mi = PropertyInfo.GetGetMethod(true);
				if (mi != null)
					InnerGetter = FastMethodCallBuilder.Current.Build(mi);
			}

			return InnerGetter;
		}

		protected virtual FastMethodCallDelegate GetSetter() {
			if (InnerSetter == null) {
				MethodInfo mi = PropertyInfo.GetSetMethod(true);
				if (mi != null)
					InnerSetter = FastMethodCallBuilder.Current.Build(mi);
			}

			return InnerSetter;
		}
		//.........................................................................
		#endregion
	}
}
