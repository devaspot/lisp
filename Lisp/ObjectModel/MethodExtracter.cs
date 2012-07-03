using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Front.Lisp;

namespace Front.ObjectModel {
	
	public class MethodExtracter {


		#region Protected Fields
		//.........................................................................
		protected Type InnerType;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public MethodExtracter(Type t) {
			InnerType = t;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Type Type {
			get { return InnerType; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		// TODO Может не для инстанса это делать?
		public virtual IList<MethodDefinition> GetMethods(object instance) {
			List<MethodDefinition> methods = new List<MethodDefinition>();

			MethodInfo[] minfos = Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo mi in minfos) {
				object[] attrs = mi.GetCustomAttributes(typeof(BehaviorMethodAttribute), true);
				if (attrs != null && attrs.Length > 0) {
					BehaviorMethodAttribute attr = (BehaviorMethodAttribute)attrs[0];
					MethodInfo mbody = GetMethodBody(mi.Name, mi.GetParameters());
					if (mbody != null) {
						FastMethodCallDelegate d = FastMethodCallBuilder.Current.Build(mbody);
						if (d != null) {
							methods.Add(new MethodDefinition(attr.MethodName, delegate(IObject obj, object[] args) {
								object o = args[0] ?? instance;
								object[] args1 = (object[])args[1];

								return d(o, args1);
							}));
						}
					}
				}
			}

			return methods;
		}

		public virtual IList<MethodDefinition> GetAccessors(object instance) {
			List<MethodDefinition> methods = new List<MethodDefinition>();

			MethodInfo[] minfos = Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo mi in minfos) {
				if (mi.Name.StartsWith("_get_") || mi.Name.StartsWith("_set_")) {
					MethodDefinition md = new MethodDefinition(mi.Name.Substring(1));
					if (instance != null) {
						FastMethodCallDelegate d = FastMethodCallBuilder.Current.Build(mi);
						md.Body = delegate(IObject obj, object[] args) {
							// TODO: нужно этот делегат как-то вынести в метод,
							// только не понятно, как instance заморозить...
							object o = args[0] ?? instance;
							object[] args1 = (object[])args[1];

							return d(o, args1);
						};
					}
					methods.Add(md);
				}
			}

			return methods;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual MethodInfo GetMethodBody(string name, ParameterInfo[] args) {
			Type[] t = null;
			if (args != null) {
				t = new Type[args.Length];
				for (int i = 0; i < args.Length; i++)
					t[i] = args[i].ParameterType;
			}

			return Type.GetMethod("_" + name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null, t, null);
		}

		//.........................................................................
		#endregion
	}
}