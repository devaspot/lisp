using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Front.Common;

namespace Front.Lisp {

	// TODO Что еще?
	public interface ILisp {
		Interpreter Interpreter { get; }
		object Eval(object o);
		object Eval(string str);
		object EvalQ(string str);

		string Str(object x);

		event EventHandler<BeforeEvalEventArgs> BeforeEval;
		event EventHandler<AfterEvalEventArgs> AfterEval;
		event EventHandler<DebugOutputArgs> DebugOutput;
	}

	public class Lisp : InitializableBase, ILisp {

		#region Public Properties
		//.........................................................................
		protected Interpreter InnerInterpreter;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public Lisp() : this(null, true) { }
		public Lisp(IServiceProvider sp) : this(sp, true) { }
		public Lisp(IServiceProvider sp, bool init) : base(sp, init) { }
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Interpreter Interpreter {
			get { return InnerInterpreter; }
		}

		public static ILisp Current {
			get { return GetCurrent(); }
		}
		//.........................................................................
		#endregion


		#region Public Events
		//.........................................................................
		public event EventHandler<BeforeEvalEventArgs> BeforeEval;
		public event EventHandler<AfterEvalEventArgs> AfterEval;
		public event EventHandler<DebugOutputArgs> DebugOutput;
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(object o) {
			object result = null;
			BeforeEvalEventArgs args = new BeforeEvalEventArgs(o);
			OnBeforeEval(args);
			if (!args.Cancel && args.Eval != null) {
				Exception e = null;
				try {
					result = Interpreter.Eval(args.Eval);
					Debug(result);
				} catch (Exception ex) {
					e = ex;
					Debug(FormatException(ex));
					if (result == null)
						result = ex;
				}
				AfterEvalEventArgs args2 = new AfterEvalEventArgs(args.Eval, result, e);
				OnAfterEval(args2);
				/*if (e != null && !(e is LispException) && !args2.ExceptionHandled)
					throw e;*/
			}

			return result;
		}

		public virtual object Eval(string str) {
			object r = Interpreter.Read(str);
			if (Interpreter.Eof(r))
				return new Eof();
			return Eval(r);
		}

		public virtual object EvalQ(string str) {
			try {
				return Eval(str);
			} catch (LispException ex) {
				return ex;
			}
		}

		public string Str(object x) {
			return Interpreter.Str(x);
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected override bool OnInitialize(IServiceProvider sp) {
			bool result = base.OnInitialize(sp);
			if (result) {
				InnerInterpreter = new Interpreter();
				InitializeInterpreter(InnerInterpreter);
			}

			return result;
		}

		protected virtual void InitializeInterpreter(Interpreter intpr) {
			intpr.Load(new LocTextReader("inline", new StringReader(global::Front.Lisp.Resources.boot)));
		}

		protected virtual void OnBeforeEval(BeforeEvalEventArgs args) {
			EventHandler<BeforeEvalEventArgs> handler = BeforeEval;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void OnAfterEval(AfterEvalEventArgs args) {
			EventHandler<AfterEvalEventArgs> handler = AfterEval;
			if (handler != null)
				handler(this, args);
		}

		protected virtual void Debug(object result) {
			if (DebugOutput != null) {
				OnDebugOutput(new DebugOutputArgs(Interpreter.Str(result)));
			}
		}

		protected virtual void OnDebugOutput(DebugOutputArgs args) {
			EventHandler<DebugOutputArgs> handler = DebugOutput;
			if (handler != null)
				handler(this, args);
		}

		protected virtual string FormatException(Exception e) {
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("An exception occured of type {0}: {1}\r\n", e.GetType(), e.ToString());
			sb.AppendFormat("Exception message: {0}\r\n", e.Message);
			if (e.InnerException != null)
				sb.AppendFormat("With InnerException: \r\n\t{0}", FormatException(e.InnerException));
			return sb.ToString();
		}
		//.........................................................................
		#endregion


		#region Singleton
		//.........................................................................
		private static Lisp _currentInstance;
		public static ILisp GetCurrent() {
			ILisp lisp = ProviderPublisher.Provider != null
				? ProviderPublisher.Provider.GetService(typeof(ILisp)) as ILisp
				: null;
			if (lisp == null) {
				if (_currentInstance == null)
					_currentInstance = new Lisp();
				lisp = _currentInstance;
			}
			return lisp;
		}
		//.........................................................................
		#endregion
	}

	public class DebugOutputArgs : EventArgs {
		protected string InnerOutput;

		public DebugOutputArgs(string output) {
			InnerOutput = output;
		}

		public string Output {
			get { return InnerOutput; }
		}
	}

	public class BeforeEvalEventArgs : EventArgs {
		protected object InnerEval;
		protected bool InnerCancel = false;

		public BeforeEvalEventArgs(object eval) {
			InnerEval = eval;
		}

		public object Eval {
			get { return InnerEval; }
			set { InnerEval = value; }
		}

		public bool Cancel {
			get { return InnerCancel; }
			set { InnerCancel = value; }
		}
	}

	public class AfterEvalEventArgs : EventArgs {
		protected object InnerEval;
		protected Exception InnerException;
		protected object InnerResult;
		protected bool InnerExceptionHandled = false;

		public AfterEvalEventArgs(object eval, object result) : this(eval, result, null) { }
		public AfterEvalEventArgs(object eval, object result, Exception e) {
			InnerEval = eval;
			InnerException = e;
			InnerResult = result;
		}

		public object Eval {
			get { return InnerEval; }
		}

		public Exception Exception {
			get { return InnerException; }
		}

		public bool ExceptionHandled {
			get { return InnerExceptionHandled; }
			set { InnerExceptionHandled = value; }
		}
	}
}
