using System;

namespace Front.Lisp {

	public class Macro : Closure {

		#region Constructors
		//.........................................................................
		public Macro(Cons args, IEnvironment env, Interpreter interpreter, Location loc)
			: base(args, env, interpreter, loc) { }
		//.........................................................................
		#endregion

	}
}