using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Front.Lisp {

	// TODO Ќужно раздел€ть external и internal symbols!
	// TODO —делать export конструкцию
	// defun и defmacro должны делать автоматом export!
	// defconstant и defvar  - должны делать аналогично
	
	public class Package {

		#region Protected Fields
		//.........................................................................
		protected string InnerName;
		protected Interpreter InnerInterpreter;
		protected SymbolTable InnerExternalTable;
		protected SymbolTable InnerInternalTable;
		protected List<Package> InnerUseList = new List<Package>();
		protected PackageProvider InnerPackageProvider;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public Package(Interpreter interpreter, string name) : this(interpreter, name, null) { }
		public Package(Interpreter interpreter, string name, PackageProvider pp) {
			if (interpreter == null)
				Error.Critical(new ArgumentNullException("interpreter"), typeof(Package));
			if (name == null)
				Error.Critical(new ArgumentNullException("name"), typeof(Package));

			InnerName = name;
			InnerPackageProvider = pp ?? new PackageProvider();
			InnerInterpreter = interpreter;
			InnerExternalTable = new SymbolTable(this, InnerInterpreter.TypeResolver);
			InnerInternalTable = new SymbolTable(this, InnerInterpreter.TypeResolver);
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public string Name {
			get { return InnerName; }
		}

		public SymbolTable ExternalTable {
			get { return InnerExternalTable; }
		}

		public SymbolTable InternalTable {
			get { return InnerInternalTable; }
		}

		public IList<Package> UseList {
			get { return InnerUseList; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual Symbol Intern(string name) {
			Symbol result = FindSymbol(name);
			if (result == null)
				result = InternalTable.Intern(name);

			return result;
		}

		public virtual Symbol FindSymbol(string name) {
			Symbol result = null;
			int i = name.IndexOf("@");
			if (i > 0) { // специфицирован пакет - лезем к нему напр€мую
				string pname = name.Substring(0, i);
				Package p = InnerPackageProvider.GetPackage(pname);
				if (p == null)
					throw new LispException("Can not find package: " + pname);
				result = p.Intern(name.Substring(i + 1));
			} else {
				result = InternalTable.FindSymbol(name);
				if (result == null)
					result = ExternalTable.FindSymbol(name);
				if (result == null) { // у нас нету, посмотрим, а что есть в списке используемых пакетов
					foreach (Package p in UseList) {
						if (p != null) {
							result = p.FindAccessibleSymbol(name);
							if (result != null)
								return result;
						}
					}

					
				}
			}

			return result;
		}

		public virtual Symbol FindAccessibleSymbol(string name) {
			Symbol result = ExternalTable.FindSymbol(name);
			if (result == null)
				foreach (Package p in UseList) {
					result = p.FindAccessibleSymbol(name);
					if (result != null)
						return result;
				}

			return result;
		}

		public virtual Symbol InternConstant(string sym, object value) {
			Symbol s = FindSymbol(sym);
			if (s != null)
				throw new LispException("Constant: " + sym + " already defined");
			InternalTable.InternConstant(sym, value);
			return s;
		}

		public virtual Symbol UnIntern(string sym) {
			Symbol s1, s2;
			s1 = InternalTable.UnIntern(sym);
			s2 = ExternalTable.UnIntern(sym);
			return s1 ?? s2;
		}

		public virtual void Export(params object[] names) {
			if (names != null)
				foreach (object n in names) {
					if (n != null) {
						string name;
						if (n is string)
							name = (string)n;
						else if (n is Symbol)
							name = GetName((Symbol)n);
						else
							name = n.ToString();
						Symbol s = InternalTable.FindSymbol(name);
						if (s != null) {
							InternalTable.UnIntern(name);
							ExternalTable.AddSymbol(s);
						}
					}
				}
		}

		// XXX ј если зациклимс€! ÷иклических ссылок не должно быть!
		public virtual void UsePackage(Package p) {
			if (p != null && !UseList.Contains(p))
				UseList.Add(p);
		}

		public static Package DefinePackage(string name, Interpreter interpreter) {
			if (interpreter == null)
				Error.Fatal(new ArgumentNullException("interpreter"), typeof(Package));
			PackageProvider pp = interpreter.PackageProvider;
			Package p = pp.GetPackage(name);
			if (p == null) {
				p = new Package(interpreter, name, pp);
				pp.RegisterPackage(p);
			}

			return p;
		}

		public static string GetName(object o) {
			if (o == null)
				return null;
			if (o is string)
				return (string)o;
			Symbol s = o as Symbol;
			if (s != null)
				return s.Name.Trim().TrimStart(':');
			return o.ToString();
		}

		public override string ToString() {
			return string.Format("@{0}: Internals {1}, Externals {2}", Name, InternalTable.Count, ExternalTable.Count);
		}
		//.........................................................................
		#endregion
	}

	public class PackageProvider {

		#region Protected Fields
		//.........................................................................
		protected Hashtable InnerPackages = new Hashtable();
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual void RegisterPackage(Package p) {
			if (p != null) {
				InnerPackages[p.Name] = p;
			}
		}

		public virtual Package GetPackage(string name) {
			return (Package)InnerPackages[name];
		}

		public virtual IEnumerable<Package> GetPackages() {
			List<Package> packages = new List<Package>();
			foreach (Package p in InnerPackages.Values)
				packages.Add(p);

			return packages;
		}
		//.........................................................................
		#endregion

	}
}
