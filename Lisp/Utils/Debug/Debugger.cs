using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Front.Lisp.Debug {

	/// <summary>Класс реализующий интерфейс ILispIterop. Содержится на стороне 
	/// Lisp-машины и используется для проведения debug</summary>
	public class Debugger : InitializableBase, ILispIterop {

		// TODO: конструктор, и получение CurrentLisp'а...
		protected string InnerLastExpression;
		protected Exception InnerLastException;

		protected NodesCollection InnerResultNodes = new NodesCollection();

		protected long InnerLastResult;
		protected ILisp InnerCurrentLisp;

		#region Constructors
		//.........................................................................
		public Debugger() : this(null, true) { }
		public Debugger(IServiceProvider sp) : this(sp, true) { }
		public Debugger(IServiceProvider sp, bool init) : base(sp, init) { }
		//.........................................................................
		#endregion
		
		#region Public Properties
		//.........................................................................
		public virtual string LastExpression {
			get { return InnerLastExpression; }
		}

		public virtual object LastResult {
			get { return InnerLastResult; }
		}

		public virtual NodesCollection ResultNodes {
			get { return InnerResultNodes; }
		}

		public virtual bool CheckAvailability  {
			get { return true; }
		}
		/// <summary>Если не переопределен текущий Lisp, то возвращаем тот что сейчас 
		/// находится в Lisp.Current. Иначе тот Lisp, что был переопределен</summary>
		public ILisp CurrentLisp {
			get {
				if (InnerCurrentLisp == null) {
					return Lisp.Current;
				}
				return InnerCurrentLisp; }
			set { InnerCurrentLisp = value; }
		}

		public static ILispIterop Current
		{
			get { return GetCurrent(); }
		}
		//.........................................................................
		#endregion

		#region Singleton
		//.........................................................................
		private static Debugger _currentInstance = new Debugger();
		public static ILispIterop GetCurrent()
		{
			ILispIterop debugger = ProviderPublisher.Provider != null
				? ProviderPublisher.Provider.GetService(typeof(ILispIterop)) as ILispIterop
				: null;
			if (debugger == null)
				debugger = _currentInstance;

			return debugger;
		}
		//.........................................................................
		#endregion

		#region ILispIterop Members
		//.........................................................................
		public NodeDescriptor Eval(string str) {
			InnerLastExpression = str;
			object res = CurrentLisp.EvalQ(str);
			
			NodeDescriptor resultNode = InnerResultNodes.GetDescriptor(res);
			InnerLastResult = resultNode.Key;			
			return resultNode;
		}

		public NodeDescriptor EvalQ(string str) {
			try {
				return Eval(str);
			} catch (LispException ex) {
				InnerLastException = ex;
				NodeDescriptor resultNode = InnerResultNodes.GetDescriptor(ex);
				return resultNode;
			}
		}

		/// <summary>Получить лист по ключу</summary>
		public NodeDescriptor GetNodeByKey(long key) {
			if (InnerResultNodes.ContainsKey(key)) {
				return InnerResultNodes[key];
			} else {
				return null;
			}
		}

		public ArrayListSerialized GetAllChilds(long key) {
			if (InnerResultNodes.ContainsKey(key)) {
				ArrayListSerialized ar = InnerResultNodes.GetAllChilds(key);
				if (ar == null)
					return null;
				ArrayListSerialized arKeys = new ArrayListSerialized();
				foreach (object node in ar){
					arKeys.Add (((NodeDescriptor) node).Key);
				}
				return arKeys;
			} else {
				return null;
			}
		}

		public ArrayListSerialized TraceArc(long key, string arkName) {
			ArrayListSerialized ar = new ArrayListSerialized();
			ar = InnerResultNodes.TraceArc(key, arkName);
			return ar;
		}
		
		#region Not Implemented 

		public string SymbolValue(string name) {
			throw new Exception("The method or operation is not implemented.");
		}

		public string[] Files() {
			throw new Exception("The method or operation is not implemented.");
		}

		public string File(string name) {
			throw new Exception("The method or operation is not implemented.");
		}

		public void LoadFile(string path) {
			throw new Exception("The method or operation is not implemented.");
		}

		public void Intern(string symname, object symbol) {
			throw new Exception("The method or operation is not implemented.");
		}
		#endregion

		//.........................................................................
		#endregion
	}

}
