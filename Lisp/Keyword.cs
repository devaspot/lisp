using System;

namespace Front.Lisp {

	//derived just to give it distinguished type
	public class Keyword : Symbol {

		#region Constructors
		//.........................................................................
		public Keyword(Package p, String name) : base(p, name) {
			InnerGlobalValue = this;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected override void SetGlobalValue(Object newval) {
			throw new LispException("Cannot set value of keyword: " + InnerName +
									  " - keywords evaluate to themselves");
		}
		//.........................................................................
		#endregion

	}
}