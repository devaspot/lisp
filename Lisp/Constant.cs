using System;

namespace Front.Lisp {

	//derived just to give it distinguished type
	public class Constant : Symbol {

		#region Constructors
		//.........................................................................
		public Constant(Package p, String name, Object val)
			: base(p, name) {
			InnerGlobalValue = val;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected override void SetGlobalValue(Object newval) {
			throw new LispException("Cannot set value of constant: " + InnerName);
		}
		//.........................................................................
		#endregion

	}
}
