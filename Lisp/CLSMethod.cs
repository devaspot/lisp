using System;
using System.Reflection;
using System.Collections.Generic;

namespace Front.Lisp {

	
	public class CLSMethod : CLSMember {

		#region Protected Fields
		//.........................................................................
		protected Type InnerType;
		protected MemberInfo[] InnerMethInfos;
		protected bool InnerIsStatic;
		protected Dictionary<MethodInfo, FastMethodCallDelegate> InnerInvokers 
			= new Dictionary<MethodInfo, FastMethodCallDelegate>();
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public CLSMethod(string name, Type type, MemberInfo[] methods, bool isStatic) {
			InnerIsStatic = isStatic;
			InnerType = type;
			InnerMethInfos = methods;
			InnerName = name;
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override object Invoke(params object[] args) {
			object target = null;
			object[] argarray = args;
			if (!InnerIsStatic) {				 // instance field gets target from first arg
				target = args[0];
				argarray = Util.VectorRest(args);
			}
			MethodInfo mi = null;
			if (InnerMethInfos.Length == 1)	//it's not overloaded
			{
				mi = (MethodInfo)InnerMethInfos[0];
				return InvokeMethod(mi, target, argarray);
			} else {
				//this should always work, but seems to have problems, i.e. String.Concat
				if (Util.ContainsNull(argarray)) {
					return InnerType.InvokeMember(this.InnerName,
													 BindingFlags.Public | BindingFlags.InvokeMethod//|BindingFlags.FlattenHierarchy
													 | (InnerIsStatic ? BindingFlags.Static : BindingFlags.Instance)
													 | BindingFlags.NonPublic
													 , null, target, argarray);
				}
				///*
				Type[] argtypes = Util.GetTypeArray(argarray);
				//todo cache result?
				//n.b. we are not specifying static/instance here - hmmm...
				mi = InnerType.GetMethod(InnerName, argtypes);
				if (mi == null) {
					throw new LispException("Can't find matching method: " + InnerName + " for: " + InnerType.Name +
											  " taking those arguments");
				}
				//*/
				return InvokeMethod(mi, target, Util.GetValues(argarray));
			}
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual object InvokeMethod(MethodInfo mi, object target, object[] args) {
			FastMethodCallDelegate invoker;
			if (!InnerInvokers.TryGetValue(mi, out invoker)) {
				invoker = FastMethodCallBuilder.Current.Build(mi);
				InnerInvokers[mi] = invoker;
			}

			return invoker(target, args);
		}
		//.........................................................................
		#endregion

	}
}
