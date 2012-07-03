using System;
using System.Collections.Generic;
using System.Text;

namespace Front.Lisp {

	// TODO Сделать его LocalizableException
	public class LispException : Exception {
		public LispException(string message) : this(message, null) { }
		public LispException(string message, Exception innerException) : base(message, innerException) { }
	}
}
