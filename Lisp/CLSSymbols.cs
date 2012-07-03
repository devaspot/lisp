using System;

namespace Front.Lisp {

	public class CLSInstanceSymbol : Symbol {

		#region Constructors
		//.........................................................................
		public CLSInstanceSymbol(Package p, string name, string memberName, Type type) : base(p, name) {
			InnerGlobalValue = CLSMember.FindMember(memberName, type, false);
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected override Object GetGlobalValue() {
			return InnerGlobalValue;
		}

		protected override void SetGlobalValue(Object newval) {
			throw new LispException("Cannot set value of instance member symbol: " + InnerName);
		}
		//.........................................................................
		#endregion
	}

	public class CLSStaticSymbol : Symbol {
		#region Protected Fields
		//.........................................................................
		protected CLSMember InnerMember;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public CLSStaticSymbol(Package p, string name, string memberName, Type type) : base(p, name) {
			InnerGlobalValue = InnerMember = CLSMember.FindMember(memberName, type, true);
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public CLSMember Member {
			get { return InnerMember; }
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected override object GetGlobalValue() {
			return InnerMember.Value;
		}

		protected override void SetGlobalValue(object newval) {
			InnerMember.Value = newval;
		}
		//.........................................................................
		#endregion
	}

	public class CLSTypeSymbol : Symbol {
		protected TypeResolver InnerTypeResolver;

		#region Constructors
		//.........................................................................
		public CLSTypeSymbol(Package p, string name, Type type, TypeResolver tr) : base(p, name) {
			//InnerGlobalValue = type;
			InnerGlobalValue = null;
			InnerTypeResolver = tr;
		}

		//public CLSTypeSymbol(Package p, string name, Type type) : this(p, name, type, null) {			
		//}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected override object GetGlobalValue() {
			if (InnerGlobalValue == null && InnerTypeResolver != null) {
				// попытка отложенно найти тип...
				// что, если его не запоминать а искать всегда? не будет ли притормаживать?
				Type t;
				if (InnerTypeResolver.FindType( InnerName.Substring(0, InnerName.Length-1), out t))
					return t; // не запоминаем, ато (with-namespace не правильно работает..)			
			}				
			return InnerGlobalValue;
		}

		protected override void SetGlobalValue(object newval) {
			throw new LispException("Cannot set value of Type symbol: " + InnerName);
		}
		//.........................................................................
		#endregion
	}

}