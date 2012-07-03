using System;

namespace Front.Lisp {

	public class LocalVariable {

		#region Protected Fields
		//.........................................................................
		protected Int32 InnerLevel;
		protected Int32 InnerIndex;
		protected Symbol InnerSymbol;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public LocalVariable(Int32 level, Int32 index, Symbol name) {
			InnerLevel = level;
			InnerIndex = index;
			InnerSymbol = name;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Int32 Level {
			get { return InnerLevel; }
		}

		public Int32 Index {
			get { return InnerIndex; }
		}

		public Symbol Symbol {
			get { return InnerSymbol; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override String ToString() {
			return Symbol.ToString() + "^" + Level + "->" + Index;
		}
		//.........................................................................
		#endregion

	}
}