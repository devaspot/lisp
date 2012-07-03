using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;


namespace Front.Lisp {

	public class Symbol {

		#region Protected Fields
		//.........................................................................
		protected static Symbol UNDEFINED = new Symbol(null, "#!undefined");

		protected bool InnerIsDynamic = false;
		protected Symbol InnerSetter = null;
		protected string InnerName;

		//every symbol maintains its global value
		protected object InnerGlobalValue = UNDEFINED;
		protected Package InnerPackage;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public Symbol(Package p, string name) {
			InnerName = name;
			InnerPackage = p;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public bool IsDynamic {
			get { return InnerIsDynamic; }
			set { InnerIsDynamic = value; }
		}

		public bool IsDefined {
			get { return (InnerGlobalValue != UNDEFINED); }
		}

		public Symbol Setter {
			get { return InnerSetter; }
			set { InnerSetter = value; }
		}

		public object GlobalValue {
			get { return GetGlobalValue(); }
			set { SetGlobalValue(value); }
		}

		public string Name {
			get { return InnerName; }
		}

		public Package Package {
			get { return InnerPackage; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override String ToString() {
			return string.Format("{0}@{1}", (Package != null) ? Package.Name :"#undefined", InnerName);
		}

		private static Int32 nextgen = 0;
		private static object _lock = new object();
		public static Symbol GenSym(Package p) {
			lock (_lock) {
				System.Threading.Interlocked.Increment(ref nextgen);
				return new Symbol(p, "#G" + nextgen);
			}
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual object GetGlobalValue() {
			if (InnerGlobalValue != UNDEFINED)
				return InnerGlobalValue;
			throw new LispException("ERROR: undefined variable " + InnerName);
		}

		protected virtual void SetGlobalValue(object newval) {
			InnerGlobalValue = newval;
		}
		//.........................................................................
		#endregion
	}

	public class SymbolTable {

		#region Protected Fields
		//.........................................................................
		protected Hashtable InnerTable = new Hashtable(1000);
		protected TypeResolver InnerTypeResolver;
		protected Package InnerPackage;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public SymbolTable(Package p) : this(p, null) { }
		public SymbolTable(Package p, TypeResolver tr) {
			InnerTypeResolver = tr ?? new TypeResolver();
			InnerPackage = p;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public int Count {
			get { return InnerTable.Count; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public Symbol InternConstant(string name, object val) {
			Symbol result = null;
			if (InnerTable.ContainsKey(name)) {
				throw new LispException("Constant: " + name + " already defined");
			}
			InnerTable[name] = result = new Constant(InnerPackage, name, val);
			return result;
		}

		public Symbol Intern(string name) {
			Symbol result = (Symbol)InnerTable[name];
			if (result == null) {
				if (name.StartsWith(":")) {
					InnerTable[name] = result = new Keyword(InnerPackage, name);
				} else {
					int firstdot = name.IndexOf('.');
					int lastdot = name.LastIndexOf('.');
					int lastcolon = name.LastIndexOf(':');
					int nameLength = name.Length;

					// .instance
					// .[namespace.]type:explicitinstance
					// [namespace.]type.
					// [namespace.]type:static
					// obj.member - transformed by reader
					if ((firstdot != -1 || lastcolon != -1) && nameLength > 1) {
						if (firstdot == 0)	//instance
						{
							if (lastcolon > 0)	  //explicitly qualified
							{
								String memberName = name.Substring(lastcolon + 1, nameLength - (lastcolon + 1));
								Type type = InnerTypeResolver.FindType(name.Substring(1, lastcolon - 1));
								InnerTable[name] = result = new CLSInstanceSymbol(InnerPackage, name, memberName, type);
							} else {
								String memberName = name.Substring(1, nameLength - 1);
								InnerTable[name] = result = new CLSInstanceSymbol(InnerPackage, name, memberName, null);
							}
						} else if (lastcolon > 0)	//static
						{
							String memberName = name.Substring(lastcolon + 1, nameLength - (lastcolon + 1));
							Type type = InnerTypeResolver.FindType(name.Substring(0, lastcolon));
							InnerTable[name] = result = new CLSStaticSymbol(InnerPackage, name, memberName, type);
						} else if (lastdot == nameLength - 1) //type
						{	
							Type type;
							InnerTypeResolver.FindType(name.Substring(0, lastdot), out type);
							// TODO: то же нужно сделать со статиками, методами и т.п.
							// получение типа должно производиться при выполнении, а не при парсинге!
							
							InnerTable[name] = result = new CLSTypeSymbol(InnerPackage, name, type, InnerTypeResolver);
						}
					} else {
						InnerTable[name] = result = new Symbol(InnerPackage, name);
						result.IsDynamic = (name[0] == '*');
					}
				}
			}
			return result;
		}

		public void AddSymbol(Symbol s) {
			InnerTable[s.Name] = s;
		}

		public Symbol FindSymbol(string name) {
			return (Symbol)InnerTable[name];
		}

		public Symbol UnIntern(string name) {
			Symbol s = null;
			if (name != null && InnerTable.Contains(name)) {
				s = InnerTable[name] as Symbol;
				InnerTable.Remove(name);
			}

			return s;
		}

		public virtual IEnumerable<Symbol> GetSymbols() {
			List<Symbol> symbols = new List<Symbol>();
			foreach (Symbol s in InnerTable.Values)
				symbols.Add(s);

			return symbols;
		}
		//.........................................................................
		#endregion
	}

	public class TypeResolver {

		#region Protected Fields
		//.........................................................................
		// String->Type
		protected Hashtable InnerFullNamesToTypes = new Hashtable(500);
		// String->ArrayList<Type>
		protected Hashtable InnerShortNamesToTypes = new Hashtable(500);
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public void InternType(System.Type t) {
			//add the name to both the full and shortNames
			//should be no dupes in fullNames
			if (InnerFullNamesToTypes[t.FullName] == null) {
				InnerFullNamesToTypes[t.FullName] = t;

				ArrayList arr = (ArrayList)InnerShortNamesToTypes[t.Name];

				if (arr == null) {
					arr = new ArrayList(5);
					InnerShortNamesToTypes[t.Name] = arr;
				}
				arr.Add(t);
			}
		}

		public System.Type FindType(string name) {
			Type res = null;
			name = name.Replace('~', '`');
			if (!FindType(name, out res))
				throw new LispException("Can't find type " + name);
			return res;
		}

		public bool FindType(string name, out Type res) {
			res = null;
			string generic = null;
			if (name.Contains("<")) {
				int i = name.IndexOf('<');
				generic = name.Substring(i + 1);
				generic = generic.Substring(0, generic.LastIndexOf('>'));
				name = name.Substring(0, i);
				if (name.LastIndexOf('`') < 0)
					name = string.Format("{0}`{1}", name, generic.Split(' ').Length.ToString());
			}

			if (name.IndexOf(".") > -1)	//namespace qualified
			{
				Type t = (Type)InnerFullNamesToTypes[name];
				if (t != null)
					res = t;
			} else	//short name
			{
				ArrayList tlist = (ArrayList)InnerShortNamesToTypes[name];
				if (tlist == null) {
					return false;
				} else {
					Namespace ns = Namespace.Current;
					if (ns != null && ns.Namespaces.Count > 0) {
						int max_index = -1;
						Type res1 = null;
						foreach (System.Type tp in tlist) {
							int x = ns.Namespaces.IndexOf(tp.Namespace);
							if (x > max_index) { // побеждает тип, чей Namespace указан последним
								max_index = x;
								res1 = tp;
							}
						}
						if (res1 != null)
							res = res1;							
					}
					
					if (res == null)
						// "кто первый - тот и папа..."
						res = (System.Type)tlist[0];
				}
			}

			if (res == null)
				return false;

			if (res.IsGenericType && generic != null) {
				// Ищем все типы
				List<Type> types = new List<Type>();
				string[] gentypes = generic.Split(' ');
				foreach (string tname in gentypes) {
					if (tname != null && tname != "") {
						Type t = null;
						if (FindType(tname, out t) && t != null)
							types.Add(t);
						else 
							return false;
					}
				}
				res = res.MakeGenericType(types.ToArray());
			}

			return true;
		}
		//.........................................................................
		#endregion

	}
}