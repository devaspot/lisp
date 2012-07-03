using System;

namespace Front.Lisp {

	public class DynamicEnvironment {

		#region Protected Fields
		//.........................................................................
		protected Symbol InnerSymbol;
		protected object InnerValue;
		protected DynamicEnvironment InnerNext;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		protected DynamicEnvironment(Symbol sym, Object val, DynamicEnvironment next) {
			InnerSymbol = sym;
			InnerValue = val;
			InnerNext = next;
		}
		//.........................................................................
		#endregion


		#region Static Thread
		//.........................................................................
		//this will have to be initialized with a copy of the creating thread's version
		//on thread start - no interface to thread start yet

		//hmmm... interpreter isolation will be broken if multiple interpreters on same thread
		//i.e. they will share the dynamic namespace
		[ThreadStatic]
		static DynamicEnvironment denv = null;

		public static DynamicEnvironment Current {
			get { return denv; }
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Symbol Symbol {
			get { return InnerSymbol; }
		}

		public object Value {
			get { return InnerValue; }
			set { InnerValue = value; }
		}

		public DynamicEnvironment Next {
			get { return InnerNext; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public static void Restore(DynamicEnvironment olddenv) {
			denv = olddenv;
		}

		public static void Extend(Symbol sym, Object val) {
			if (!sym.IsDynamic)
				throw new LispException("Dynamic vars must have prefix *");
			denv = new DynamicEnvironment(sym, val, denv);
		}

		public static DynamicEnvironment Find(Symbol s) {
			for (DynamicEnvironment d = denv; d != null; d = d.InnerNext) {
				if (d.InnerSymbol == s)
					return d;
			}
			return null;
		}
		//.........................................................................
		#endregion
	}
}