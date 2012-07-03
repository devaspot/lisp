using System;
using System.Text;

namespace Front.Lisp {

	public class Parameter {

		#region Protected Fields
		//.........................................................................
		protected Symbol InnerSymbol;
		protected Specification InnerSpec = Specification.REQ;
		protected IExpression InnerInitCode = null;
		protected Symbol InnerKey = null;
		protected Object InnerTypeSpec = typeof(Object);
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public Parameter(Symbol symbol, Specification spec, IExpression initCode) {
			if (symbol.IsDynamic)
				throw new LispException("Dynamic variables cannot be parameters to functions or let");
			if (symbol is Keyword)
				throw new LispException("Keywords cannot be parameters to functions or let");
			InnerSymbol = symbol;
			InnerSpec = spec;
			InnerInitCode = initCode;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Symbol Symbol {
			get { return InnerSymbol; }
		}

		public Specification Spec {
			get { return InnerSpec; }
		}

		public IExpression InitCode {
			get { return InnerInitCode; }
		}

		public Symbol Key {
			get { return InnerKey; }
			set { InnerKey = value; }
		}

		public Object TypeSpec {
			get { return InnerTypeSpec; }
			set { InnerTypeSpec = value; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override String ToString() {
			return "(" + Symbol + " " + Spec + " " + InitCode + ")";
		}
		//.........................................................................
		#endregion


		#region Nested Types
		//.........................................................................
		public enum Specification {
			REQ, OPT, KEY, REST
		}
		//.........................................................................
		#endregion
	}

	public class ArgSpecs {

		#region Protected Fields
		//.........................................................................
		protected IEnvironment InnerEnvironment;
		protected Parameter[] InnerParameters;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public ArgSpecs(IEnvironment env) {
			InnerEnvironment = env;
		}
		//.........................................................................
		#endregion


		#region Public Properties & Fields
		//.........................................................................
		public int NumReq = 0;
		public int NumOpt = 0;
		public int NumKey = 0;
		public int NumRest = 0; //0 or 1

		public IEnvironment Environment {
			get { return InnerEnvironment; }
			set { InnerEnvironment = value; }
		}

		public Parameter[] Parameters {
			get { return InnerParameters; }
			set { InnerParameters = value; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public ArgSpecs Copy(IEnvironment env) {
			ArgSpecs a = (ArgSpecs)MemberwiseClone();
			a.Environment = env;
			return a;
		}
		
		public Int32 GetParamCount() {
			return Parameters.Length;
		}

		public override String ToString() {
			StringBuilder str = new StringBuilder("(");
			foreach (Parameter p in Parameters) {
				str.Append(p.ToString());
				str.Append(" ");
			}
			str.Append(")");
			return str.ToString();
		}
		//.........................................................................
		#endregion
	}
}