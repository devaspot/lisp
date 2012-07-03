using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Front.Lisp {

	public class BacktraceFrame {

		#region Protected Fields
		//.........................................................................
		protected string InnerFile;
		protected int InnerLine;
		protected object InnerFrame;
		protected object InnerArgs;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public BacktraceFrame(Location loc, Object f, Object args) {
			if (loc != null) {
				InnerFile = loc.File;
				InnerLine = loc.Line;
			} else {
				InnerFile = "no file";
				InnerLine = 0;
			}

			InnerFrame = f;
			InnerArgs = args;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public String File {
			get { return InnerFile; }
		}

		public Int32 Line {
			get { return InnerLine; }
		}

		public Object Frame {
			get { return InnerFrame; }
		}

		public Object Args {
			get { return InnerArgs; }
		}
		//.........................................................................
		#endregion
	}

	public class BacktraceException : LispException {

		#region Protected Fields
		//.........................................................................
		protected List<BacktraceFrame> InnerFrames = new List<BacktraceFrame>();
		protected Interpreter InnerInterpreter;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public BacktraceException(Exception inner, BacktraceFrame frame, Interpreter interpreter) : base(inner.Message, inner) {
			InnerFrames.Add(frame);
			InnerInterpreter = interpreter;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public List<BacktraceFrame> Frames {
			get { return InnerFrames; }
		}

		public Interpreter Interpreter {
			get { return InnerInterpreter; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override String ToString() {
			StringBuilder sb = new StringBuilder(InnerException.Message);
			sb.Append('\n');
			foreach (BacktraceFrame frame in InnerFrames) {
				sb.Append(frame.File + "(" + frame.Line + "): ");
				sb.Append(frame.Frame == null ? "" : frame.Frame.ToString());
				sb.Append(" " + InnerInterpreter.Str(frame.Args));
				sb.Append('\n');
			}
			return sb.ToString();
		}

		public static BacktraceException Push(Exception inner, BacktraceFrame frame, Interpreter interpreter) {
			if (inner is BacktraceException) {
				((BacktraceException)inner).Frames.Add(frame);
				return (BacktraceException)inner;
			}
			return new BacktraceException(inner, frame, interpreter);
		}
		//.........................................................................
		#endregion
	}
}
