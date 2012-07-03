using System;
using System.Reflection;

namespace Front.Lisp {

	public delegate object Function(params object[] args);

	public interface IFunction {
		object Invoke(params object[] args);
	}

}