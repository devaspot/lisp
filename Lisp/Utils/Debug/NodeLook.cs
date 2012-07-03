using System;
using System.Collections.Generic;
using System.Text;
using Front.Lisp.Debug;

namespace Front.Lisp.Utils.Debug {

	public class QueryLispEnvironment {
	}

	public class LispEnvNodeDescriptor : NodeDescriptor {
		//дописать поля для хранения списка функций и переменных
/*		protected List<LispObjectNodeDescriptor> InnerListAllLispObject;
		protected List<LispObjectNodeDescriptor> InnerListFunctions;
		protected List<LispObjectNodeDescriptor> InnerListVars;
		*/
		protected List<string> InnerListNameFunctions;
		protected List<string> InnerListNameVars;
		/*
		public virtual List<LispObjectNodeDescriptor> ListAllLispObject {
			get { return InnerListAllLispObject; }
		}

		public virtual List<LispObjectNodeDescriptor> ListFunctions {
			get { return InnerListFunctions; }
		}

		public virtual List<LispObjectNodeDescriptor> ListVars {
			get { return InnerListVars; }
		}
		*/
		public virtual List<string> ListNameFunctions {
			get { return InnerListNameFunctions; }
		}

		public virtual List<string> ListNameVars {
			get { return InnerListNameVars; }
		}
	}
	
	//ХХХ: пока что не пригодился
	public class LispObjectNodeDescriptor : NodeDescriptor {
	}

	public class LispEnvironmentInspector : NodeHandler {

		public override ArrayListSerialized Trace(NodesCollection c, NodeDescriptor d, string arcName) {
			arcName = (arcName == null) ? null : arcName.ToLower();
			ArrayListSerialized res = new ArrayListSerialized();
			int x = arcName.IndexOf(":");
			string ns = "";
			if (x >= 0) {
				ns = arcName.Substring(0, x).Trim();
				arcName = arcName.Substring(x + 1).Trim();
			}

			if (d.SubNodes == null) d.SubNodes = new ArrayListSerialized();

			if (ns == "special") {
				if (arcName == "functions") {
					res = Functions(c, d);
				} else if (arcName == "vars") {
					res = Vars(c, d);
				}
				d.SubNodes.AddRange(res);
			}
			return res;
		}

		protected virtual ArrayListSerialized Functions(NodesCollection c, NodeDescriptor d) {
			ArrayListSerialized res = new ArrayListSerialized();
			ILisp l = Lisp.Current;
			foreach (Symbol s in l.Interpreter.CurrentPackage.ExternalTable.GetSymbols() ){
				if (s.IsDefined && s.GlobalValue is Front.Lisp.Closure) {
					NodeDescriptor n = c.GetDescriptor(s.GlobalValue);
					n.NodePath = s.Name;
					n.NodeName = s.Name;
					res.Add(n);
					c.Add(n);
				}
			}
			foreach (Symbol s in l.Interpreter.CurrentPackage.InternalTable.GetSymbols()) {
				if (s.IsDefined && s.GlobalValue is Front.Lisp.Closure ) {
					NodeDescriptor n = c.GetDescriptor(s.GlobalValue);
					n.NodePath = s.Name;
					n.NodeName = s.Name;
					res.Add(n);
					c.Add(n);
				}
			}
			return res;
		}

		protected virtual ArrayListSerialized Vars(NodesCollection c, NodeDescriptor d) {
			ArrayListSerialized res = new ArrayListSerialized();
			ILisp l = Lisp.Current;
			foreach (Symbol s in l.Interpreter.CurrentPackage.ExternalTable.GetSymbols()) {
				if (s.IsDefined && !(s.GlobalValue is Front.Lisp.Closure)) {
					NodeDescriptor n = c.GetDescriptor(s.GlobalValue);
					n.NodePath = s.Name;
					n.NodeName = s.Name;
					res.Add(n);
					c.Add(n);
				}
			}
			foreach (Symbol s in l.Interpreter.CurrentPackage.InternalTable.GetSymbols()) {
				if (s.IsDefined && !(s.GlobalValue is Front.Lisp.Closure)) {
					NodeDescriptor n = c.GetDescriptor(s.GlobalValue);
					n.NodePath = s.Name;
					n.NodeName = s.Name;
					res.Add(n);
					c.Add(n);
				}
			}
			return res;
		}

		/// <summary>Развертывание информации в LispEnvNodeDescriptor из полученного экземпляра обьекта</summary>
		public static LispEnvNodeDescriptor LispEnvNodeFactory(NodesCollection col, params object[] args) {
			//XXX:возможно нужно дописать
			ILisp l = Lisp.Current;
			LispEnvNodeDescriptor node = new LispEnvNodeDescriptor();
			node.NodeType = "LispEnvNodeDescriptor";
			node.MembersCount = l.Interpreter.CurrentPackage.ExternalTable.Count + l.Interpreter.CurrentPackage.InternalTable.Count;
			return node;
		}
		/*
		/// <summary>Развертывание информации в LispObjectNodeDescriptor из полученного экземпляра обьекта</summary>
		public static LispObjectNodeDescriptor LispObjectFactory(NodesCollection col, params object[] args) {
			LispObjectNodeDescriptor node = new LispObjectNodeDescriptor();
			node.NodeType = "LispObjectNodeDescriptor";
			Symbol s = args[0] as Symbol;
			node.NodeName = s.Name;
			node.Value = s.Name;
			
			return node;
		}*/
	}
}