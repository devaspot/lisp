using System;
using System.Reflection;

namespace Front.Lisp {

	public class Closure : IFunction, IExpression {

		#region Protected Fields
		//.........................................................................
		protected IExpression InnerBody;
		protected IEnvironment InnerEnvironment;
		protected ArgSpecs InnerArgsSpecs;
		protected Interpreter InnerInterpreter;
		protected Location InnerLocation;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public Closure(Cons args, IEnvironment env, Interpreter interpreter, Location loc) {
			InnerInterpreter = interpreter;
			InnerLocation = loc;
			ArgSpecs specs = AnalyzeArgSpec((Cons)args.First, env, loc);
			//create an env expanded by params in which to analyze body
			IEnvironment env2 = new Environment(specs.Parameters, null, env);

			InnerArgsSpecs = specs;
			InnerBody = interpreter.Analyze(new Cons(interpreter.BLOCK, args.Rest), env2, loc);
			InnerEnvironment = env;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public IEnvironment Environment {
			get { return InnerEnvironment; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual Object Eval(IEnvironment env) {
			return Copy(env);
		}

		public virtual Object Invoke(params Object[] args) {
			try {
				return InnerBody.Eval(new Environment(InnerArgsSpecs.Parameters, 
						BuildParamArray(args, 0, InnerEnvironment), 
					InnerEnvironment));
			} catch (Exception ex) {
				Exception ext = ex;
				string str = ex.Message;
				if (!(ex is BacktraceException)) {
					throw BacktraceException.Push(ex, 
						new BacktraceFrame(InnerLocation, this, args), 
						InnerInterpreter);
				}
				throw ex;
			}
		}

		public virtual Closure Copy(IEnvironment env) {
			Closure c = null;
			try {
				c = (Closure)this.MemberwiseClone();
			} catch (Exception e) {
				throw new LispException("internal error: no clone " + e.Message);
			}
			c.InnerEnvironment = env;
			c.InnerArgsSpecs = InnerArgsSpecs.Copy(env);
			return c;
		}

		public override String ToString() {
			return "{" + this.GetType().Name + " "// + /*this.name + toStringArgs() + */
			+ InnerArgsSpecs.ToString()
			+ "}";
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected int GetParamCount() {
			return InnerArgsSpecs.GetParamCount();
		}

		//parse out params from spec (which may contain &optional, &key, &rest, initforms etc
		protected ArgSpecs AnalyzeArgSpec(Cons arglist, IEnvironment env, Location loc) {
			//count the params
			int nParams = 0;
			Cons a = arglist;
			while (a != null) {
				Object p = a.First;
				if (p != InnerInterpreter.AMPOPT && p != InnerInterpreter.AMPKEY && p != InnerInterpreter.AMPREST)
					++nParams;
				a = a.Rest;
			}

			ArgSpecs ret = new ArgSpecs(env);
			ret.Parameters = new Parameter[nParams];
			Parameter.Specification state = Parameter.Specification.REQ;

			int param = 0;
			a = arglist;
			while (a != null) {
				Object p = a.First;
				switch (state) {
					case Parameter.Specification.REQ:
						if (p == InnerInterpreter.AMPOPT)
							state = Parameter.Specification.OPT;
						else if (p == InnerInterpreter.AMPKEY)
							state = Parameter.Specification.KEY;
						else if (p == InnerInterpreter.AMPREST)
							state = Parameter.Specification.REST;
						else {
							if (p is Symbol) {
								ret.Parameters[param++] =
								new Parameter((Symbol)p, Parameter.Specification.REQ, null);
								++ret.NumReq;
							} else if (p is Cons) {
								ret.Parameters[param] =
								new Parameter((Symbol)((Cons)p).First, Parameter.Specification.REQ, null);
								ret.Parameters[param].TypeSpec = InnerInterpreter.Eval(Cons.GetSecond((Cons)p), env);
								++param;
								++ret.NumReq;
							}
						}
						break;
					case Parameter.Specification.OPT:
						if (p == InnerInterpreter.AMPOPT)
							throw new LispException("&optional can appear only once in arg list");
						else if (p == InnerInterpreter.AMPKEY)
							state = Parameter.Specification.KEY;
						else if (p == InnerInterpreter.AMPREST)
							state = Parameter.Specification.REST;
						else {
							if (p is Symbol) {
								ret.Parameters[param++] =
								new Parameter((Symbol)p, Parameter.Specification.OPT, null);
								++ret.NumOpt;
							} else if (p is Cons) {
								ret.Parameters[param++] =
								new Parameter((Symbol)((Cons)p).First, Parameter.Specification.OPT,
												  InnerInterpreter.Analyze(Cons.GetSecond((Cons)p), env, loc));
								++ret.NumOpt;
							} else
								throw new LispException("&optional parameters must be symbols or (symbol init-form)");
						}
						break;
					case Parameter.Specification.KEY:
						if (p == InnerInterpreter.AMPOPT)
							throw new LispException("&optional must appear before &key in arg list");
						else if (p == InnerInterpreter.AMPKEY)
							throw new LispException("&key can appear only once in arg list");
						else if (p == InnerInterpreter.AMPREST)
							state = Parameter.Specification.REST;
						else {
							if (p is Symbol) {
								ret.Parameters[param] =
								new Parameter((Symbol)p, Parameter.Specification.KEY, null);
								ret.Parameters[param].Key = InnerInterpreter.Intern(":" + ((Symbol)p).Name);
								++param;
								++ret.NumKey;
							} else if (p is Cons) {
								ret.Parameters[param] =
								new Parameter((Symbol)((Cons)p).First, Parameter.Specification.KEY,
												  InnerInterpreter.Analyze(Cons.GetSecond((Cons)p), env, loc));
								ret.Parameters[param].Key =
								InnerInterpreter.Intern(":" + ((Symbol)((Cons)p).First).Name);
								++param;
								++ret.NumKey;
							} else
								throw new LispException("&key parameters must be symbols or (symbol init-form)");
						}
						break;
					case Parameter.Specification.REST:
						if (p == InnerInterpreter.AMPOPT)
							throw new LispException("&optional must appear before &rest in arg list");
						else if (p == InnerInterpreter.AMPKEY)
							throw new LispException("&key must appear before &rest in arg list");
						else if (p == InnerInterpreter.AMPREST)
							throw new LispException("&rest can appear only once in arg list");
						else {
							if (!(p is Symbol))
								throw new LispException("&rest parameter must be a symbol");
							else {
								if (ret.NumRest > 0) //already got a rest param
									throw new LispException("Only one &rest arg can be specified");
								ret.Parameters[param++] =
								new Parameter((Symbol)p, Parameter.Specification.REST, null);
								++ret.NumRest;
							}
						}
						break;
				}

				a = a.Rest;
			}

			return ret;
		}

		//returns an array of evaluated args corresponding to params of argspec,
		//including substitution of default values where none provided, construction of
		//rest list etc
		//suitable for extending the environment prior to evaluating body of closure
		protected virtual object[] BuildParamArray(object[] code, int offset, IEnvironment env) {
			//do nothing if fixed params and matching number
			if (InnerArgsSpecs.GetParamCount() == InnerArgsSpecs.NumReq && InnerArgsSpecs.NumReq == code.Length)
				return code;

			Object[] argArray = new Object[GetParamCount()];
			int nargs = code.Length - offset;
			if (nargs < InnerArgsSpecs.NumReq)
				throw new LispException("Too few arguments to procedure, expected at least " + InnerArgsSpecs.NumReq
										  + ", but found " + nargs + " arguments");

			int i;
			// Fill in the required parameters
			for (i = 0; i < InnerArgsSpecs.NumReq; i++) {
				argArray[i] = //evalArgs?Interpreter.execute(code[i+offset], env):
								  code[i + offset];
			}

			//now grab args to satisfy optionals
			if (InnerArgsSpecs.NumOpt > 0) {
				for (i = InnerArgsSpecs.NumReq; i < InnerArgsSpecs.NumReq + InnerArgsSpecs.NumOpt; i++) {
					if (i < nargs) {
						argArray[i] = //evalArgs?Interpreter.execute(code[i+offset], env):
										  code[i + offset];
						//if missing passed to optional, get default
						if (argArray[i] == Missing.Value) {
							argArray[i] = GetDefaultParamValue(InnerArgsSpecs.Parameters[i]);
						}
					} else //ran out of args, default the rest
					{
						argArray[i] = GetDefaultParamValue(InnerArgsSpecs.Parameters[i]);
					}
				}
			}

			//build a rest list
			Cons rest = null;
			for (int x = code.Length - 1; x - offset >= i; --x) {
				Object val = //evalArgs?Interpreter.execute(code[x], env):
								 code[x];
				rest = new Cons(val, rest);
			}

			//search for key args in rest
			if (InnerArgsSpecs.NumKey > 0) {
				for (i = InnerArgsSpecs.NumReq + InnerArgsSpecs.NumOpt;
					i < InnerArgsSpecs.NumReq + InnerArgsSpecs.NumOpt + InnerArgsSpecs.NumKey; i++) {
					argArray[i] = FindKeyParamValue(InnerArgsSpecs.Parameters[i], rest);
				}
			}

			// Add the rest parameter (if there is one)
			if (InnerArgsSpecs.NumRest == 1) {
				argArray[i] = rest;
			}

			return argArray;
		}

		protected virtual object GetDefaultParamValue(Parameter p) {
			if (p.InitCode != null)
				return p.InitCode.Eval(InnerArgsSpecs.Environment);
			else
				return Missing.Value; //hmmm... could return null
		}

		protected object FindKeyParamValue(Parameter p, Cons args) {
			for (; args != null; args = args.Rest) {
				Symbol first = args.First as Symbol;
				if (args.First == p.Key) {
					if (args.Rest != null) {
						Object ret = Cons.GetSecond(args);
						if (ret == Missing.Value) {
							ret = GetDefaultParamValue(p);
						}
						return ret;
					} else
						throw new LispException("Key args must be provided in pairs of [:key value]");
				}
			}
			return GetDefaultParamValue(p);
		}
		//.........................................................................
		#endregion
	}
}
