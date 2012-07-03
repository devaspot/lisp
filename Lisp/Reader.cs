using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Globalization;

namespace Front.Lisp {

	public class LocTextReader {

		#region Protected Fields
		//.........................................................................
		protected String InnerFile;
		protected Int32 InnerLine = 1;
		protected TextReader InnerReader;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public LocTextReader(String file, TextReader t) {
			InnerFile = file;
			InnerReader = t;
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

		public TextReader Reader {
			get { return InnerReader; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual Int32 Read() {
			Int32 val = Reader.Read();
			if (val == '\n')
				++InnerLine;
			return val;
		}

		public virtual Int32 Peek() { 
			return Reader.Peek(); 
		}
		//.........................................................................
		#endregion

	}

	public class Location {

		#region Protected Fields
		//.........................................................................
		protected String InnerExpressionString;
		protected String InnerFile;
		protected Int32 InnerLine;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		internal Location(String file, Int32 line, String expr) {
			InnerFile = file;
			InnerLine = line;
			InnerExpressionString = expr;
		}
		internal Location(String file, Int32 line) : this (file, line, null) {			
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

		public String ExpressionString {
			get { return InnerExpressionString; }
		}
		//.........................................................................
		#endregion

	}

	public class CompositeSymbol {

		#region Protected Fields
		//.........................................................................
		protected Cons InnerSymbolAsList;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public CompositeSymbol(Cons symAsList) { 
			InnerSymbolAsList = symAsList; 
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Cons SymbolAsList {
			get { return InnerSymbolAsList; }
		}
		//.........................................................................
		#endregion

	}

	public class ReaderMacro {

		#region Protected Fields
		//.........................................................................
		protected Function InnerFunc;
		protected Boolean InnerIsTerminating = true;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public ReaderMacro(Function func, Boolean isTerminating) {
			InnerFunc = func;
			InnerIsTerminating = isTerminating;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Function Func {
			get { return InnerFunc; }
		}

		public Boolean IsTerminating {
			get { return InnerIsTerminating; }
		}
		//.........................................................................
		#endregion

	}

	public class EndDelimiter {
		public Char Delim;
	}

	public class Eof { }

	public class Reader {

		#region Protected Fields
		//.........................................................................
		protected static Eof EOF_MARKER = new Eof();
		protected Hashtable InnerMacroTable = new Hashtable();	// TODO Это надо вынести куда-то, чтобы лиспом можно было рулить!
		protected Hashtable InnerLocationTable = new Hashtable();	//Object->Location
		protected Interpreter InnerInterpreter;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public Reader(Interpreter interpreter) {
			InnerInterpreter = interpreter;
			MacroTable.Add('(', new ReaderMacro(new Function(ReadList), true));
			MacroTable.Add(')', new ReaderMacro(new Function(ReadEndDelimiter), true));
			MacroTable.Add('[', new ReaderMacro(new Function(ReadVector), true));
			MacroTable.Add(']', new ReaderMacro(new Function(ReadEndDelimiter), true));
			MacroTable.Add('"', new ReaderMacro(new Function(ReadString), true));
			MacroTable.Add('\'', new ReaderMacro(new Function(ReadQuote), true));
			MacroTable.Add('`', new ReaderMacro(new Function(ReadBackQuote), true));
			MacroTable.Add(',', new ReaderMacro(new Function(ReadUnquote), true));
			//no func since read directly implements
			MacroTable.Add(';', new ReaderMacro(null, true));
			MacroTable.Add('#', new ReaderMacro(null, true));
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public IDictionary MacroTable {
			get { return InnerMacroTable; }
		}

		public IDictionary LocationTable {
			get { return InnerLocationTable; }
		}

		public Interpreter Interpreter {
			get { return InnerInterpreter; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public static Boolean Eof(Object o) {
			return o == EOF_MARKER;
		}

		public object Read(LocTextReader t) {
			Object result = DoRead(t, false);
			if (result is EndDelimiter)
				throw new LispException("Read error - read unmatched: " + ((EndDelimiter)result).Delim);
			return result;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected Object DoRead(LocTextReader t, Boolean firstInForm) {
			Int32 ch = t.Read();
			while (Char.IsWhiteSpace((Char)ch)) ch = t.Read();
			if (ch == -1) {
				//return null;
				//return Symbol.EOF;
				return EOF_MARKER;
			}
			if (ch == '#') {
				EatMultiComment(t);
				return DoRead(t, firstInForm);
			}
			if (ch == ';') {
				EatComment(t);
				return DoRead(t, firstInForm);
			}
			ReaderMacro rm = (ReaderMacro)MacroTable[(Char)ch];
			if (rm != null) {
				return rm.Func(t, (Char)ch);
			} else {
				return ReadSymbolOrNumber(t, ch, firstInForm);
			}
		}

		protected void EatComment(LocTextReader t) {
			Int32 ch = t.Peek();
			while (ch != -1 && ch != '\n' && ch != '\r') {
				t.Read();
				ch = t.Peek();
			}
			if (ch != -1 && ch != '\n' && ch != '\r')
				t.Read();
		}

		protected void EatMultiComment(LocTextReader t) {
			Int32 ch = t.Peek();
			while (ch != -1 && ch != '#') {
				t.Read();
				ch = t.Peek();
			}
			if (ch == '#')
				t.Read();
		}

		protected Object ReadSymbolOrNumber(LocTextReader t, Int32 ch, Boolean firstInForm) {
			StringBuilder b = new StringBuilder();
			b.Append((Char)ch);
			Boolean complete = false;
			while (!complete) {
				ch = t.Peek();
				if (ch == -1)
					complete = true;
				else if (Char.IsWhiteSpace((Char)ch))
					complete = true;
				else {
					ReaderMacro rm = (ReaderMacro)MacroTable[(Char)ch];
					if (rm != null && rm.IsTerminating)
						complete = true;
					else {
						t.Read();
						b.Append((Char)ch);
					}
				}
			}
			return ParseSymbolOrNumber(b.ToString(), firstInForm);
		}

		protected Object ParseSymbolOrNumber(String s, Boolean firstInForm) {
			//treat nil as a constant
			if (s.Equals("nil"))
				return null;
			Double d;
			if (Double.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out d)) {
				return (Int32)d;
			} else if (Double.TryParse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out d)) {
				return d;
			} else {
				Object o = SplitSymbol(s);
				if (o is Cons && firstInForm) {
					return new CompositeSymbol((Cons)o);
				}
				return o;
			}
		}

		protected Object SplitSymbol(String s) {
			//turn x[y] into ([y] x) - can we with readvector in place?
			//Int32 bridx = s.LastIndexOf("[");


			Int32 dotidx = s.LastIndexOf(".");
			Int32 colonidx = s.LastIndexOf(":");
			//dot in the middle and not member(dot at start) or type(dot at end)
			if (dotidx >= 0 && dotidx > colonidx && dotidx < (s.Length - 1) && s[0] != '.') {
				//turn x.y into (.y x)

				return Cons.MakeList(Interpreter.Intern(s.Substring(dotidx)),
											SplitSymbol(s.Substring(0, dotidx)));
			}
			return Interpreter.Intern(s);
		}

		protected Cons ReadDelimitedList(LocTextReader t, Int32 delim) {
			Cons ret = null;
			Cons tail = null;

			Int32 ch = t.Peek();
			while (Char.IsWhiteSpace((Char)ch)) {
				t.Read();
				ch = t.Peek();
			}
			while (ch != delim) {
				Object o = DoRead(t, delim == ')' && ret == null);
				if (Eof(o)) {
					throw new LispException("Read error - eof found before matching: "
											  + (Char)delim + "\n File: " + t.File + ", line: " + t.Line);
				}
				EndDelimiter ed = o as EndDelimiter;
				if (ed != null) {
					if (ed.Delim == delim) {
						return ret;
					} else
						throw new LispException("Read error - read unmatched: " + ed.Delim
												  + "\n File: " + t.File + ", line: " + t.Line);
				}
				Cons link = new Cons(o, null);
				if (delim == ')' && ret == null && o is CompositeSymbol) {
					ret = ((CompositeSymbol)o).SymbolAsList;
					tail = ret.Rest;
				} else if (ret == null) {
					ret = tail = link;
				} else {
					tail.Rest = link;
					tail = link;
				}
				ch = t.Peek();
				while (Char.IsWhiteSpace((Char)ch)) {
					t.Read();
					ch = t.Peek();
				}
			}

			//eat delim
			t.Read();
			return ret;
		}

		protected Object ReadQuote(params Object[] args) {
			LocTextReader t = (LocTextReader)args[0];
			Int32 line = t.Line;
			Object ret = Cons.MakeList(Interpreter.QUOTE, DoRead(t, false));
			//record the location			
			LocationTable[ret] = new Location(t.File, line);
			return ret;
		}

		protected Object ReadBackQuote(params Object[] args) {
			LocTextReader t = (LocTextReader)args[0];
			Int32 line = t.Line;
			Object ret = Cons.MakeList(Interpreter.BACKQUOTE, DoRead(t, false));
			//record the location
			LocationTable[ret] = new Location(t.File, line);
			return ret;
		}

		protected Object ReadUnquote(params Object[] args) {
			LocTextReader t = (LocTextReader)args[0];
			Int32 line = t.Line;
			Int32 ch = t.Peek();
			Object ret = null;
			if (ch == '@') {
				t.Read();
				ret = Cons.MakeList(Interpreter.UNQUOTE_SPLICING, DoRead(t, false));
			} else
				ret = Cons.MakeList(Interpreter.UNQUOTE, DoRead(t, false));
			//record the location
			LocationTable[ret] = new Location(t.File, line);
			return ret;
		}

		protected Object ReadList(params Object[] args) {
			LocTextReader t = (LocTextReader)args[0];
			Int32 line = t.Line;
			Object ret = ReadDelimitedList(t, ')');
			//record the location
			if (ret != null)
				LocationTable[ret] = new Location(t.File, line);
			return ret;
		}

		protected Object ReadVector(params Object[] args) {
			LocTextReader t = (LocTextReader)args[0];
			Int32 line = t.Line;
			Cons largs = ReadDelimitedList(t, ']');
			Object ret = new Cons(Interpreter.VECTOR, largs);
			//record the location
			LocationTable[ret] = new Location(t.File, line);
			return ret;
		}

		protected Object ReadEndDelimiter(params Object[] args) {
			LocTextReader t = (LocTextReader)args[0];
			//so we can complain
			EndDelimiter ed = new EndDelimiter();
			//Char c = (Char)args[1];//t.Read();
			ed.Delim = (Char)args[1];//t.Read();
			return ed;
			//throw new LispException("Read error - read unmatched: " + c);
		}

		protected Object ReadString(params Object[] args) {
			StringBuilder b = new StringBuilder();
			LocTextReader t = (LocTextReader)args[0];
			Int32 line = t.Line;
			//eat the double-quote
			//t.Read();
			//Int32 ch = t.Peek();
			Int32 ch = t.Read();
			while (ch != '"') {
				if (ch == -1) {
					throw new LispException("Read error - eof found before matching: \""
											  + "\n File: " + t.File + ", line: " + t.Line);
				}
				//eat it
				//t.Read();
				if (ch == '\\')	//escape
				{
					ch = t.Read();
					if (ch == -1) {
						throw new LispException("Read error - eof found before matching: \""
												  + "\n File: " + t.File + ", line: " + t.Line);
					}
					switch (ch) {
						case 't':
							ch = '\t';
							break;
						case 'r':
							ch = '\r';
							break;
						case 'n':
							ch = '\n';
							break;
						case '\\':
							break;
						case '"':
							break;
						default:
							throw new LispException("Unsupported escape character: \\" + (Char)ch
													  + "\n File: " + t.File + ", line: " + t.Line);
					}
				}
				b.Append((Char)ch);
				//ch = t.Peek();
				ch = t.Read();
			}
			//eat the trailing quote
			//t.Read();
			Object ret = b.ToString();
			//record the location
			LocationTable[ret] = new Location(t.File, line);
			return ret;
		}
		//.........................................................................
		#endregion
	}


}