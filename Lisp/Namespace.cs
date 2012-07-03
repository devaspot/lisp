using System;
using System.Collections.Generic;
using System.Text;

namespace Front.Lisp {

	public class Namespace {
		protected Namespace InnerParent;
		protected IList<string> InnerNS = new List<string>();

		#region Constructors
		//...................................................................
		public Namespace() {
		}

		public Namespace(params string[] ns) {
			if (ns != null)
				foreach(string n in ns)
					InnerNS.Add(n);
		}

		public Namespace(Namespace parent) {
			InnerParent = parent;
			if (parent != null) {
				foreach(string n in parent.Namespaces)
					InnerNS.Add(n);
			}
		}
		//...................................................................
		#endregion

		public Namespace Parent {
			get { return InnerParent; }
		}

		public IList<string> Namespaces {
			get { return InnerNS; }
		}

		public void AddNamespace(params string[] ns) {
			if (ns != null)
				foreach (string n in ns)
					AddNamespace(n);
		}

		public void AddNamespace(string ns) {
			if (ns == null || ns.Trim() == "") return;
			if (InnerNS.Contains(ns))
				InnerNS.Remove(ns);
			InnerNS.Add(ns);
		}

		public Namespace Rollback() {
			Namespace res = ContextSwitch<Namespace>.Current;
			ContextSwitch<Namespace>.CurrentSwitch.Dispose();
			return res;
		}

		public static Namespace Current {
		    get {
				return ContextSwitch<Namespace>.Current;
				// TODO: как из динамического окружения выковыривать эту штуку?
		    }
		}

		public static Namespace Fork() {
			Namespace res = new Namespace(Current);			
			ContextSwitch<Namespace> cx = new ContextSwitch<Namespace>(res);
			return res;
		}		
	}
}
