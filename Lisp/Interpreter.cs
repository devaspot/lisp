using System;
using System.IO;
using System.Reflection;
using System.Collections.Specialized;
using System.Collections;

namespace Front.Lisp {

	// TODO Можно заоптимизать создание Environment и других часто используемых конструкций через MemberwiseClone!

	public class Interpreter : MarshalByRefObject {

		#region Symbols
		//.........................................................................
		public Symbol
			BLOCK,
			SET,
			FIRST,
			REST,
			DEF,
			IF,
			FN,
			MACRO,
			OR,
			WHILE,
			BREAK,
			AMPOPT,
			AMPKEY,
			AMPREST,
			QUOTE,
			DLET,
			STR,
			VECTOR,
			COMPARE,
			BACKQUOTE,
			UNQUOTE,
			UNQUOTE_SPLICING,
			NIL,
			TRUE,
			T,
			FALSE,
			EX,
			LASTVAL,
			NEXTLASTVAL,
			THIRDLASTVAL,
			EOF;
		//.........................................................................
		#endregion


		#region Protected Fields
		//.........................................................................
		protected IEnvironment InnerGlobalEnvironment;
		protected Reader InnerReader;
		protected SimpleGenericFunction InnerGetEnumGF;
		protected BinOp InnerCompareGF;
		protected SimpleGenericFunction InnerStrGF;
		protected HybridDictionary InnerTraceList = new HybridDictionary();
		[ThreadStatic]
		protected static bool InnerInTrace = false;
		protected PackageProvider InnerPackageProvider;
		protected Package InnerCurrentPackage;
		protected Package InnerKeywordPackage;
		protected Package InnerGlobalPackage;
		protected TypeResolver InnerTypeResolver = new TypeResolver();
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public Interpreter() : this(null) { }
		public Interpreter(string bootfile) {
			Initialize();

			if (bootfile != null)
				LoadFile(null, bootfile);
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public IEnvironment GlobalEnvironment {
			get { return GetGlobalEnvironment(); }
		}

		public Reader Reader {
			get { return InnerReader; }
		}

		public IFunction GetEnumGF {
			get { return InnerGetEnumGF; }
		}

		public IFunction CompareGF {
			get { return InnerCompareGF; }
		}

		public IFunction StrGF {
			get { return InnerStrGF; }
		}

		public IDictionary TraceList {
			get { return InnerTraceList; }
		}

		public static bool InTrace {
			get { return InnerInTrace; }
		}

		public ICollection Traces { 
			get { return InnerTraceList.Keys; } 
		}

		public PackageProvider PackageProvider {
			get { return InnerPackageProvider; }
		}

		public Package CurrentPackage {
			get { return InnerCurrentPackage; }
			set { SetCurrentPackage(value); }
		}

		public TypeResolver TypeResolver {
			get { return InnerTypeResolver; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public void Intern(String sym, Object f) {
			Intern(sym).GlobalValue = f;
		}

		public void InternAndExport(String sym, object f) {
			InternAndExport(sym).GlobalValue = f;
		}

		public void UnIntern(String sym) {
			UnInternF(sym);
		}

		public void InternType(Type t) {
			TypeResolver.InternType(t);
		}

		public void InternTypesFrom(Assembly a) {
			//Assembly a = Assembly.LoadWithPartialName(assemblyName);
			AddTypesFrom(a);
		}

		public object ValueOf(string sym) {
			Symbol s = CurrentPackage.Intern(sym);
			if (s != null)
				try {
					return s.GlobalValue;
				} catch {
					// TODO: Плохо!
					return null;
				}
			return null;
		}

		public String Str(Object x) {
			return x != null ? Util.InvokeObject(InnerStrGF, x).ToString() : "null";
		}

		public Object Read(String streamname, TextReader t) {
			return InnerReader.Read(new LocTextReader(streamname, t));
		}

		public Object Read(LocTextReader t) {
			return InnerReader.Read(t);
		}

		public Object Read(string str) {
			return Read(str, new StringReader(str));
		}

		public Boolean Eof(Object o) {
			return Reader.Eof(o);
		}

		public Object LoadFile(string directory, string fileName) {
			if (directory == null)
				directory = AppDomain.CurrentDomain.BaseDirectory;
			return LoadFile(Path.Combine(directory, fileName));
		}

		public Object LoadFile(String path) {
			TextReader tr = new StreamReader(path);
			try {
				//InnerCurrentPackage = PackageProvider.GetPackage("user"); - пока что это не надо. не прямым лиспом единиым! (пример: SchemeLoader)
				Load(new LocTextReader(Path.GetFullPath(path), tr));
			} finally {
				tr.Close();
			}
			//InnerCurrentPackage = PackageProvider.GetPackage("user");

			return "loaded: " + path;
		}

		public Object Load(LocTextReader t) {
			Object expr = null;
			do {
				Int32 line = t.Line;
				expr = Read(t);

				if (!Eof(expr)) {
					try {
						Eval(expr, GlobalEnvironment);
					} catch (Exception ex) {
						throw BacktraceException.Push(ex, new BacktraceFrame(new Location(t.File, line), "when evaluating ", expr), this);
					}
				}
			} while (!Eof(expr));
			return true;
		}

		public Object Eval(Object x) {
			Object ret = null;
			try {
				ret = Eval(x, GlobalEnvironment);
				THIRDLASTVAL.GlobalValue = NEXTLASTVAL.GlobalValue;
				NEXTLASTVAL.GlobalValue = LASTVAL.GlobalValue;
				LASTVAL.GlobalValue = ret;
			} catch (Exception e) {
				EX.GlobalValue = e;
				throw;
			}
			return ret;
		}

		public Object Call(string fname, params object[] args) {
			IFunction f = null;
			try {
				Symbol s = CurrentPackage.Intern(fname);
				if (s != null) f = s.GlobalValue as IFunction;
			} catch (Exception) {
			}

			if (f == null)
				f = Analyze(Read("inner", new StringReader(fname)), GlobalEnvironment) as IFunction;
			if (f == null)
				throw new LispException(String.Format("Can't invoke '{0}'", fname));
			return f.Invoke(args);
		}

		public Object Eval(Object x, IEnvironment env) {
			IExpression analyzedCode = Analyze(x, env);
			return analyzedCode.Eval(env);
		}

		public void Trace(Symbol s) {
			InnerTraceList[s] = null;
		}

		public void UnTrace(Symbol s) {
			InnerTraceList.Remove(s);
		}

		public void UnTraceAll() {
			InnerTraceList.Clear();
		}

		public System.Type FindType(String type) {
			if (type == null) return null;
			return TypeResolver.FindType(type);
		}

		public IExpression Analyze(Object expr, IEnvironment env) {
			return Analyze(expr, env, null);
		}

		public IExpression Analyze(Object expr, IEnvironment env, Location enclosingLoc) {
			Symbol symbol = expr as Symbol;
			if (symbol != null) {
				if (symbol.IsDynamic)
					return new DynamicVar(symbol);
				Object var = env.Lookup(symbol);
				if (var is LocalVariable)
					return new LocalVar((LocalVariable)var);
				else
					return new GlobalVar((Symbol)var);
			}
			if (!(expr is Cons))	 //must be literal
				return new QuoteExpr(expr);

			Cons exprs = (Cons)expr;

			Location loc = (Location)InnerReader.LocationTable[expr];
			if (loc != null)
				InnerReader.LocationTable.Remove(expr);
			else
				loc = enclosingLoc;

			Object f = exprs.First;
			Cons args = exprs.Rest;

			//see if it's a macro
			Symbol s = f as Symbol;
			if (s != null && s.IsDefined && s.GlobalValue is Macro) {
				Macro m = (Macro)s.GlobalValue;
				Object[] argarray = Cons.ToVector(args);
				Object mexprs = null;
				try {
					mexprs = m.Invoke(argarray);
				} catch (Exception ex) {
					BacktraceFrame frame = new BacktraceFrame(loc, s, args);
					throw BacktraceException.Push(ex, frame, this);
				}
				try {
					return Analyze(mexprs, env, loc);
				} catch (Exception ex) {
					BacktraceFrame frame = new BacktraceFrame(loc, "when expanding ", exprs);
					throw BacktraceException.Push(ex, frame, this);
				}
			}
			Int32 numargs = Cons.GetLength(args);

			if (f == DLET)
				return new DynamicLet(args, env, this, loc);
			else if (f == FN)
				return new Closure(args, env, this, loc);
			else if (f == MACRO)
				return new Macro(args, env, this, loc);
			else if (f == WHILE)
				return new WhileExpression(args, env, this, loc);
			else if (f == BLOCK) {
				if (numargs == 0)
					return new QuoteExpr(null);
				//remove block from block of one
				else if (numargs == 1)
					return Analyze(args.First, env, loc);
				else
					return new BlockExpression(args, env, this, loc);
			} else if (f == OR) {
				if (numargs == 0)
					return new QuoteExpr(null);
				else
					return new OrExpression(args, env, this, loc);
			} else if (f == IF)
				return new IfExpression(args, env, this, loc);
			else if (f == QUOTE)
				return new QuoteExpr(args.First);
			else if (f == SET)
				return new SetExpression(args, env, this, loc);
			else	//presume funcall
				return new ApplyExpression(exprs, env, this, loc);
		}

		public void Trace(String fname, Object[] args) {
			if (!InnerInTrace) {
				InnerInTrace = true;
				System.Diagnostics.Trace.WriteLine(fname + InnerStrGF.Invoke((Object)args));
				InnerInTrace = false;
			}
		}

		public Symbol Intern(String sym) {
			if (sym.StartsWith(":"))
				return InternKeyword(sym);
			else if (sym.StartsWith("*"))
				return InternGlobal(sym);
			else
				return CurrentPackage.Intern(sym);
		}

		public Symbol InternAndExport(string sym) {
			Symbol s = CurrentPackage.Intern(sym);
			CurrentPackage.Export(sym);
			return s;
		}

		public Symbol InternConstant(String sym, Object val) {
			return CurrentPackage.InternConstant(sym, val);
		}

		public Symbol InternKeyword(string name) {
			return InnerKeywordPackage.Intern(name);
		}

		public Symbol InternGlobal(string name) {
			return InnerGlobalPackage.Intern(name);
		}

		public Symbol InternAndExportConstant(String sym, Object val) {
			Symbol s = CurrentPackage.InternConstant(sym, val);
			CurrentPackage.Export(sym);
			return s;
		}

		//last arg must be seq
		public Object Apply(params Object[] args) {
			Object f = Primitives.Arg(0, args);
			IEnumerator tail = (IEnumerator)InnerGetEnumGF.Invoke(args[args.Length - 1]);
			ArrayList fargs = new ArrayList();
			for (int i = 1; i < args.Length - 1; i++)
				fargs.Add(args[i]);
			while (tail.MoveNext()) {
				fargs.Add(tail.Current);
			}
			return Util.InvokeObject(f, fargs.ToArray());
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual IEnvironment GetGlobalEnvironment() {
			return InnerGlobalEnvironment;
		}

		protected void Initialize() {
			InnerGlobalEnvironment = new Environment(null, null, null);
			InnerReader = new Reader(this);
			InnerPackageProvider = new PackageProvider();
			InnerGlobalPackage = Package.DefinePackage("global", this);
			CurrentPackage = Package.DefinePackage("internal", this);
			CurrentPackage.Export("*package*");
			InnerKeywordPackage = Package.DefinePackage("keyword", this);

			CurrentPackage.UsePackage(InnerKeywordPackage);
			CurrentPackage.UsePackage(InnerGlobalPackage);

			Assembly[] asm = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly a in asm) {
				AddTypesFrom(a);
			}

			InternBuiltins();
			Primitives.InternAll(this);
			LASTVAL.GlobalValue = null;
			NEXTLASTVAL.GlobalValue = null;
			THIRDLASTVAL.GlobalValue = null;

			//these primitives are here, rather than in Primitives, because their implementation
			//requires calls to gfs bound to symbols		
			InternAndExport("intern", new Function(InternF));
			InternAndExport("unintern", new Function(UnInternF));
			InternAndExport("eval", new Function(EvalF));
			InternAndExport("load", new Function(LoadF));
			InternAndExport("map->list", new Function(MapToList));
			InternAndExport("apply", new Function(Apply));
			InternAndExport("<", new Function(Lt));
			InternAndExport("<=", new Function(LtEq));
			InternAndExport(">", new Function(Gt));
			InternAndExport(">=", new Function(GtEq));
			InternAndExport("==", new Function(EqEq));
			InternAndExport("!=", new Function(NotEq));
			InternAndExport("typeof", new Function(TypeOf));
			InternAndExport("null", null);
			InternAndExport("defpackage", new Function(DefPackage));
			InternAndExport("use-package", new Function(UsePackage));
			InternAndExport("in-package", new Function(InPackage));
			InternAndExport("gensym", new Function(GenSym));
			InternAndExport("export", new Function(Export));
			InternAndExport("cast", new Function(Cast));

			InnerStrGF = (SimpleGenericFunction)InternAndExport("str").GlobalValue;
			InnerGetEnumGF = (SimpleGenericFunction)InternAndExport("get-enum").GlobalValue;
			InnerCompareGF = (BinOp)InternAndExport("compare").GlobalValue;

			Intern("interpreter", this);
		}

		protected void AddTypesFrom(Assembly a) {
			if (a == null) {
				System.Diagnostics.Trace.WriteLine(String.Format("Warning: Assembly '{0}' was not loaded", a));
				return;
			}
			Type[] types = a.GetTypes();
			foreach (Type t in types) {
				TypeResolver.InternType(t);
			}
		}

		protected Object EvalF(params Object[] args) {
			Object x = Primitives.Arg(0, args);

			return Eval(x, GlobalEnvironment);
		}

		protected Object LoadF(params Object[] args) {
			String path = (String)Primitives.Arg(0, args);
			return LoadFile(path);
		}

		protected Object DefPackage(params Object[] args) {
			object s = Primitives.Arg(0, args);
			Package p = Package.DefinePackage(Package.GetName(s), this);
			p.UsePackage(PackageProvider.GetPackage("internal"));
			p.UsePackage(PackageProvider.GetPackage("global"));

			return p;
		}

		protected Object UsePackage(params Object[] args) {
			Symbol s = (Symbol)Primitives.Arg(0, args);
			Package p = PackageProvider.GetPackage(Package.GetName(s));
			if (p == null)
				throw new LispException("Can not find package: " + Package.GetName(s));
			CurrentPackage.UsePackage(p);

			return p;
		}

		protected Object InPackage(params Object[] args) {
			Symbol s = (Symbol)Primitives.Arg(0, args);
			Package p = PackageProvider.GetPackage(Package.GetName(s));
			if (p == null)
				throw new LispException("Can not find package: " + Package.GetName(s));
			CurrentPackage = p;

			return p;
		}

		protected Object Export(params Object[] args) {
			if (args != null)
				foreach (object a in args) {
					Cons c = a as Cons;
					if (c != null) {
						while (c != null) {
							CurrentPackage.Export(c.First);
							c = c.Rest;
						}
					} else
						CurrentPackage.Export(a);
				}
			return args;
		}

		protected Object Cast(params Object[] args) {
			if (args != null) {
				Type t = (Type)Primitives.Arg(0, args);
				object value = Primitives.Arg(1, args);

				return new CastInfo(t, value);
			}

			return null;
		}

		protected Object MapToList(params Object[] args) {
			Object f = Primitives.Arg(0, args);
			IEnumerator[] enums = new IEnumerator[args.Length - 1];
			for (int i = 0; i < enums.Length; i++) {
				enums[i] = (IEnumerator)InnerGetEnumGF.Invoke(Primitives.Arg(i + 1, args));
			}
			//n.b. setting up arg array which will be reused
			//mean functions cannot assume ownership of args w/o copying them
			Object[] fargs = new Object[enums.Length];
			Cons ret = null;
			Cons tail = null;
			while (true) {
				for (int i = 0; i < enums.Length; i++) {
					if (enums[i].MoveNext())
						fargs[i] = enums[i].Current;
					else //bail on shortest
						return ret;
				}

				Object x = Util.InvokeObject(f, fargs);
				Cons node = new Cons(x, null);
				if (ret == null)
					ret = tail = node;
				else
					tail = tail.Rest = node;
			}
		}

		protected Object Lt(params Object[] args) {
			return (Int32)Util.InvokeObject(InnerCompareGF, args) < 0;
		}

		protected Object LtEq(params Object[] args) {
			return (Int32)Util.InvokeObject(InnerCompareGF, args) <= 0;
		}

		protected Object Gt(params Object[] args) {
			return (Int32)Util.InvokeObject(InnerCompareGF, args) > 0;
		}

		protected Object GtEq(params Object[] args) {
			return (Int32)Util.InvokeObject(InnerCompareGF, args) >= 0;
		}

		protected Object TypeOf(params Object[] args) {
			if (args.Length > 1) {
				Type[] res = new Type[args.Length];
				for (int i = 0; i < args.Length; i++)
					res[i] = FindType(args[i] as String);
				return res;
			} else if (args.Length > 0)
				return FindType(args[0] as String);
			return null;
		}

		protected Object EqEq(params Object[] args) {
			return (Int32)Util.InvokeObject(InnerCompareGF, args) == 0;
		}

		protected Object NotEq(params Object[] args) {
			return (Int32)Util.InvokeObject(InnerCompareGF, args) != 0;
		}

		protected Object InternF(params Object[] args) {
			String sym = (String)Primitives.Arg(0, args);

			return CurrentPackage.Intern(sym);
		}

		protected Object UnInternF(params Object[] args) {
			String sym = (String)Primitives.Arg(0, args);

			return CurrentPackage.UnIntern(sym);
		}

		protected Object GenSym(params Object[] args) {
			return Symbol.GenSym(CurrentPackage);
		}

		protected virtual void InternBuiltins() {
			BLOCK = InternAndExport("block");
			SET = InternAndExport("__set");
			FIRST = InternAndExport("first");
			REST = InternAndExport("rest");
			DEF = InternAndExport("def");
			IF = InternAndExport("if");
			FN = InternAndExport("fn");
			MACRO = InternAndExport("macro");
			OR = InternAndExport("or");
			WHILE = InternAndExport("while");
			BREAK = InternAndExport(":break");
			AMPOPT = InternAndExport("&optional");
			AMPKEY = InternAndExport("&key");
			AMPREST = InternAndExport("&rest");
			QUOTE = InternAndExport("quote");
			DLET = InternAndExport("dynamic-let");
			STR = InternAndExport("str");
			VECTOR = InternAndExport("vector");
			COMPARE = InternAndExport("compare");
			BACKQUOTE = InternAndExport("backquote");
			UNQUOTE = InternAndExport("unquote");
			UNQUOTE_SPLICING = InternAndExport("unquote-splicing");
			NIL = InternAndExportConstant("nil", null);
			TRUE = InternAndExportConstant("true", true);
			T = InternAndExportConstant("t", true);
			FALSE = InternAndExportConstant("false", false);
			EX = InternAndExport("!");
			LASTVAL = InternAndExport("$");
			NEXTLASTVAL = InternAndExport("$$");
			THIRDLASTVAL = InternAndExport("$$$");
			EOF = InternAndExport(":eof");
		}

		protected virtual void SetCurrentPackage(Package p) {
			if (p != null) {
				InnerCurrentPackage = p;
				Intern("*package*").GlobalValue = p;
			}
		}

		//.........................................................................
		#endregion

	}
}
