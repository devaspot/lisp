using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace Front.Lisp {

	public class MethodAccessor : MemberAccessorBase {

		#region Protected Fields
		//.........................................................................
		protected FastMethodCallDelegate InnerCaller;
		protected bool InnerIsStatic;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public MethodAccessor(Type type, string name, object o) : this(type, name, null) { }
		public MethodAccessor(Type type, string name, params Type[] args) : base(type, name, args) {
			if (Caller == null)
				Error.Warning(new ApplicationException("Can not create a fast method caller for " + ToString()), typeof(MethodAccessor));
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public MethodInfo MethodInfo {
			get { return (MethodInfo)MemberInfo; }
		}

		public FastMethodCallDelegate Caller {
			get { return GetCaller(); }
		}

		public bool IsStatic {
			get { return GetIsStatic(); }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override object Invoke(params object[] args) {
			object target = null;
			object[] p = args;
			if (!IsStatic) {
				target = args[0];
				p = Util.VectorRest(args);
			}

			return Call(target, p);
		}

		public override string ToString() {
			return string.Format("[Method: {0}]", base.ToString());
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual object Call(object target, object[] parameters) {
			return Caller(target, parameters);
		}

		protected override MemberInfo RetrieveMember(Type t, string name, Type[] args) {
			MemberInfo mi = null;
			if (t != null && name != null) {
				mi = t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | 
					BindingFlags.Static | BindingFlags.Instance | 
					BindingFlags.IgnoreCase,
					null, args, null);
				if (mi == null)
					throw new LispException("Can't find matching method: " + name + " for: " + t.Name +
											  " taking those arguments");
				InnerIsStatic = ((MethodInfo)mi).IsStatic;
			}

			return mi;
		}

		protected virtual FastMethodCallDelegate GetCaller() {
			if (InnerCaller == null)
				InnerCaller = FastMethodCallBuilder.Current.Build(MethodInfo);

			return InnerCaller;
		}

		protected virtual bool GetIsStatic() {
			return InnerIsStatic;
		}
		//.........................................................................
		#endregion

	}
}
