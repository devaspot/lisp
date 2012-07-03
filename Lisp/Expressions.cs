using System;
using System.Diagnostics;

namespace Front.Lisp {

	public interface IExpression {
		object Eval(IEnvironment env);
	}

	public interface IVariable {
		void SetValue(object value, IEnvironment env);
		Symbol Symbol { get; }
	}

	public class QuoteExpr : IExpression {

		#region Protected Fields
		//.........................................................................
		protected object InnerValue;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public QuoteExpr(object val) {
			InnerValue = val;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public object Value {
			get { return InnerValue; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(IEnvironment env) {
			return InnerValue;
		}
		//.........................................................................
		#endregion
	}

	public class GlobalVar : IExpression, IVariable {

		#region Protected Fields
		//.........................................................................
		protected Symbol InnerSymbol;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public GlobalVar(Symbol s) {
			InnerSymbol = s;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Symbol Symbol {
			get { return GetSymbol(); }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(IEnvironment env) {
			return InnerSymbol.GlobalValue;
		}

		public virtual void SetValue(Object val, IEnvironment env) {
			InnerSymbol.GlobalValue = val;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual Symbol GetSymbol() {
			return InnerSymbol;
		}
		//.........................................................................
		#endregion
	}

	public class LocalVar : IExpression, IVariable {

		#region Protected Fields
		//.........................................................................
		protected LocalVariable InnerVar;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public LocalVar(LocalVariable var) {
			InnerVar = var;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Symbol Symbol {
			get { return GetSymbol(); }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(IEnvironment env) {
			return env[InnerVar];
		}

		public virtual void SetValue(Object val, IEnvironment env) {
			env[InnerVar] = val;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual Symbol GetSymbol() {
			return InnerVar.Symbol;
		}
		//.........................................................................
		#endregion

	}

	public class DynamicVar : IExpression, IVariable {

		#region Protected Fields
		//.........................................................................
		protected Symbol InnerSymbol;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public DynamicVar(Symbol s) {
			InnerSymbol = s;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Symbol Symbol {
			get { return GetSymbol(); }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(IEnvironment env) {
			DynamicEnvironment d = DynamicEnvironment.Find(InnerSymbol);
			if (d != null)
				return d.Value;
			return InnerSymbol.GlobalValue;
		}

		public virtual void SetValue(Object val, IEnvironment env) {
			DynamicEnvironment d = DynamicEnvironment.Find(InnerSymbol);
			if (d != null)
				d.Value = val;
			InnerSymbol.GlobalValue = val;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual Symbol GetSymbol() {
			return InnerSymbol;
		}
		//.........................................................................
		#endregion
	}

	public struct BindPair {
		public DynamicVar DynamicVar;
		public IExpression Expression;
	}

	public class DynamicLet : IExpression {

		#region Protected Fields
		//.........................................................................
		protected Interpreter InnerInterpreter;
		protected BindPair[] InnerBinds;
		protected IExpression InnerBody;
		protected Location InnerLocation;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public DynamicLet(Cons args, IEnvironment env, Interpreter interpreter, Location loc) {
			InnerLocation = loc;
			InnerInterpreter = interpreter;
			Cons bindlist = (Cons)args.First;
			Int32 blen = Cons.GetLength(bindlist);
			if ((blen % 2) != 0) {	//odd
				throw new LispException("Odd number of args in dynamic-let binding list");
			}
			InnerBinds = new BindPair[blen / 2];
			for (int i = 0; i < InnerBinds.Length; i++) {
				InnerBinds[i].DynamicVar = (DynamicVar)interpreter.Analyze(bindlist.First, env, loc);
				bindlist = bindlist.Rest;
				InnerBinds[i].Expression = interpreter.Analyze(bindlist.First, env, loc);
				bindlist = bindlist.Rest;
			}
			InnerBody = interpreter.Analyze(new Cons(interpreter.BLOCK, args.Rest), env, loc);
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Interpreter Interpreter {
			get { return InnerInterpreter; }
		}

		public BindPair[] Binds {
			get { return InnerBinds; }
		}

		public IExpression Body {
			get { return InnerBody; }
		}

		public Location Location {
			get { return InnerLocation; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(IEnvironment env) {
			DynamicEnvironment olddenv = DynamicEnvironment.Current;
			try {
				for (int i = 0; i < InnerBinds.Length; i++) {
					DynamicEnvironment.Extend(InnerBinds[i].DynamicVar.Symbol,
											InnerBinds[i].Expression.Eval(env));
				}
				return InnerBody.Eval(env);
			} catch (BacktraceException bex) {
				throw bex;
			} catch (Exception ex) {
				throw BacktraceException.Push(ex, new BacktraceFrame(InnerLocation, "set", null), InnerInterpreter);
			} finally {
				DynamicEnvironment.Restore(olddenv);
			}
		}
		//.........................................................................
		#endregion
	}

	public class WhileExpression : IExpression {

		#region Protected Fields
		//.........................................................................
		protected Interpreter InnerInterpreter;
		protected IExpression InnerTest;
		protected IExpression InnerBody;
		protected Location InnerLocation;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public WhileExpression(Cons args, IEnvironment env, Interpreter interpreter, Location loc) {
			InnerLocation = loc;
			InnerInterpreter = interpreter;
			InnerTest = interpreter.Analyze(args.First, env, loc);
			InnerBody = interpreter.Analyze(new Cons(interpreter.BLOCK, args.Rest), env, loc);
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Interpreter Interpreter {
			get { return InnerInterpreter; }
		}

		public IExpression Test {
			get { return InnerTest; }
		}

		public IExpression Body {
			get { return InnerBody; }
		}

		public Location Location {
			get { return InnerLocation; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(IEnvironment env) {
			try {
				while (Util.IsTrue(InnerTest.Eval(env))) {
					InnerBody.Eval(env);
				}

				return null;
			} catch (BacktraceException bex) {
				throw bex;
			} catch (Exception ex) {
				throw BacktraceException.Push(ex, new BacktraceFrame(InnerLocation, "set", null), InnerInterpreter);
			}
		}
		//.........................................................................
		#endregion

	}

	public class BlockExpression : IExpression {

		#region Protected Fields
		//.........................................................................
		protected Interpreter InnerInterpreter;
		protected IExpression[] InnerExpressions;
		protected Location InnerLocation;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public BlockExpression(Cons args, IEnvironment env, Interpreter interpreter, Location loc) {
			InnerLocation = loc;
			InnerInterpreter = interpreter;
			InnerExpressions = new IExpression[Cons.GetLength(args)];
			for (Int32 i = 0; i < InnerExpressions.Length; i++, args = args.Rest) {
				InnerExpressions[i] = interpreter.Analyze(args.First, env, loc);
			}
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Interpreter Interpreter {
			get { return InnerInterpreter; }
		}

		public IExpression[] Expressions {
			get { return InnerExpressions; }
		}

		public Location Location {
			get { return InnerLocation; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(IEnvironment env) {
			try {
				for (Int32 i = 0; i < InnerExpressions.Length - 1; i++)
					InnerExpressions[i].Eval(env);

				return InnerExpressions[InnerExpressions.Length - 1].Eval(env);
			} catch (BacktraceException bex) {
				throw bex;
			} catch (Exception ex) {
				throw BacktraceException.Push(ex, new BacktraceFrame(InnerLocation, "set", null), InnerInterpreter);
			}
		}
		//.........................................................................
		#endregion
	}

	public class OrExpression : IExpression {

		#region Protected Fields
		//.........................................................................
		protected Interpreter InnerInterpreter;
		protected IExpression[] InnerExpressions;
		protected Location InnerLocation;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public OrExpression(Cons args, IEnvironment env, Interpreter interpreter, Location loc) {
			InnerLocation = loc;
			InnerInterpreter = interpreter;
			InnerExpressions = new IExpression[Cons.GetLength(args)];
			for (Int32 i = 0; i < InnerExpressions.Length; i++, args = args.Rest) {
				InnerExpressions[i] = interpreter.Analyze(args.First, env, loc);
			}
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Interpreter Interpreter {
			get { return InnerInterpreter; }
		}

		public IExpression[] Expressions {
			get { return InnerExpressions; }
		}

		public Location Location {
			get { return InnerLocation; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(IEnvironment env) {
			try {
				for (Int32 i = 0; i < InnerExpressions.Length - 1; i++) {
					Object result = InnerExpressions[i].Eval(env);
					if (Util.IsTrue(result)) return result;
				}

				return InnerExpressions[InnerExpressions.Length - 1].Eval(env);
			} catch (BacktraceException bex) {
				throw bex;
			} catch (Exception ex) {
				throw BacktraceException.Push(ex, new BacktraceFrame(InnerLocation, "set", null), InnerInterpreter);
			}
		}
		//.........................................................................
		#endregion
	}

	public class IfExpression : IExpression {

		#region Protected Fields
		//.........................................................................
		protected Interpreter InnerInterpreter;
		protected IExpression InnerTestExpression;
		protected IExpression InnerTrueExpression;
		protected IExpression InnerFalseExpression;
		protected Location InnerLocation;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public IfExpression(Cons args, IEnvironment env, Interpreter interpreter, Location loc) {
			InnerLocation = loc;
			InnerInterpreter = interpreter;
			Int32 len = Cons.GetLength(args);
			if (len < 2 || len > 3)
				throw new LispException("Wrong number of args for if");
			InnerTestExpression = interpreter.Analyze(args.First, env, loc);
			InnerTrueExpression = interpreter.Analyze(Cons.GetSecond(args), env, loc);
			if (len == 3)
				InnerFalseExpression = interpreter.Analyze(Cons.GetThird(args), env, loc);
			else
				InnerFalseExpression = new QuoteExpr(null);
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Interpreter Interpreter {
			get { return InnerInterpreter; }
		}

		public IExpression TestExpression {
			get { return InnerTestExpression; }
		}

		public IExpression TrueExpression {
			get { return InnerTrueExpression; }
		}

		public IExpression FalseExpression {
			get { return InnerFalseExpression; }
		}

		public Location Location {
			get { return InnerLocation; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(IEnvironment env) {
			try {
				if (Util.IsTrue(InnerTestExpression.Eval(env)))
					return InnerTrueExpression.Eval(env);
				return InnerFalseExpression.Eval(env);
			} catch (BacktraceException bex) {
				throw bex;
			} catch (Exception ex) {
				throw BacktraceException.Push(ex, new BacktraceFrame(InnerLocation, "if", null), InnerInterpreter);
			}
		}
		//.........................................................................
		#endregion

	}

	public class SetExpression : IExpression {

		#region Protected Fields
		//.........................................................................
		protected IVariable InnerVar;
		protected IExpression InnerValue;
		protected Interpreter InnerInterpreter;
		protected Location InnerLocation;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public SetExpression(Cons args, IEnvironment env, Interpreter interpreter, Location loc) {
			InnerLocation = loc;
			InnerInterpreter = interpreter;
			Int32 len = Cons.GetLength(args);
			if (len != 2)
				throw new LispException("Wrong number of args for set");
			InnerVar = (IVariable)interpreter.Analyze(args.First, env, loc);
			InnerValue = interpreter.Analyze(Cons.GetSecond(args), env, loc);
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public IVariable Var {
			get { return InnerVar; }
		}

		public IExpression Value {
			get { return InnerValue; }
		}

		public Interpreter Interpreter {
			get { return InnerInterpreter; }
		}

		public Location Location {
			get { return InnerLocation; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(IEnvironment env) {
			try {
				Object retval = InnerValue.Eval(env);
				InnerVar.SetValue(retval, env);
				return retval;
			} catch (BacktraceException bex) {
				throw bex;
			} catch (Exception ex) {
				throw BacktraceException.Push(ex, new BacktraceFrame(InnerLocation, "set", null), InnerInterpreter);
			}
		}
		//.........................................................................
		#endregion

	}

	public class ApplyExpression : IExpression {

		#region Protected Fields
		//.........................................................................
		Symbol InnerFSymbol = null;
		IExpression InnerFExpression;
		IExpression[] InnerArgsExpressions;
		Interpreter InnerInterpreter;
		Location InnerLocation;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public ApplyExpression(Cons args, IEnvironment env, Interpreter interpreter, Location loc) {
			InnerLocation = loc;
			InnerInterpreter = interpreter;
			InnerFExpression = interpreter.Analyze(args.First, env, loc);
			if (InnerFExpression is IVariable) {
				InnerFSymbol = ((IVariable)InnerFExpression).Symbol;
			}
			Int32 len = Cons.GetLength(args.Rest);
			InnerArgsExpressions = new IExpression[len];
			args = args.Rest;
			for (Int32 i = 0; i < InnerArgsExpressions.Length; i++, args = args.Rest) {
				InnerArgsExpressions[i] = interpreter.Analyze(args.First, env, loc);
			}
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Symbol FSymbol {
			get { return InnerFSymbol; }
		}

		public IExpression FExpression {
			get { return InnerFExpression; }
		}

		public IExpression[] ArgsExpressions {
			get { return InnerArgsExpressions; }
		}

		public Interpreter Interpreter {
			get { return InnerInterpreter; }
		}

		public Location Location {
			get { return InnerLocation; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Eval(IEnvironment env) {
			Object f = InnerFExpression.Eval(env);
			Object[] args = new Object[InnerArgsExpressions.Length];
			for (Int32 i = 0; i < args.Length; i++)
				args[i] = InnerArgsExpressions[i].Eval(env);

			Boolean doTrace =  InnerFSymbol != null &&
									 InnerInterpreter.TraceList.Contains(InnerFSymbol);
			try {
				if (doTrace) {
					InnerInterpreter.Trace(InnerFSymbol != null ? InnerFSymbol.Name : f.ToString(), args);
					Trace.Indent();
					Object ret = Util.InvokeObject(f, args);
					Trace.Unindent();
					return ret;
				} else
					return Util.InvokeObject(f, args);
			} catch (Exception ex) {
				string str = ex.Message;
				if (InnerFSymbol != null && !InnerFSymbol.Name.Equals("throw")) {
					throw BacktraceException.Push(ex, new BacktraceFrame(InnerLocation, InnerFSymbol, args)
															, InnerInterpreter);
				} else {
					throw ex;
				}
			}
		}
		//.........................................................................
		#endregion

	}
}