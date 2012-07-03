using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Front.Lisp;
using System.Diagnostics;
namespace Repl {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
            
			ILisp lisp = Front.Lisp.Lisp.Current;
			Interpreter interpreter = lisp.Interpreter;
			for (; ; ) {
				try {
					Console.Write("> ");
					Object r = interpreter.Read("Console", Console.In);
					if (interpreter.Eof(r))
						return;
					Object x = interpreter.Eval(r);
					Console.WriteLine(interpreter.Str(x));
				} catch (BacktraceException ex1) {
					Console.WriteLine("!Exception: " + ex1.GetBaseException().Message);
				}  catch (Exception ex2) {
					Console.WriteLine("!Exception: " + ex2.GetBaseException().Message);
				}
			}
		}

		public static void MyMethod() {
			int x = 1;
		}

	}

	
}