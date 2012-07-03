using System;
using System.Data;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Front.Collections;


namespace Front.ObjectModel {

	/// <summary>ƒиспетчер поведений и вызова поведенческих методов</summary>
	public class BehaviorDispatcher : ICloneable {

		#region Protected Properties
		//.........................................................................
		protected HybridDictionary InnerMethods = new HybridDictionary(); // name -> SortedList
		protected ClassDefinition InnerClass;
		protected BehaviorDispatcher InnerParent;
		protected IObject InnerInstance; // TODO: тут бы следовало хранить просто Object
										 // € всетаки оставл€ю надежду написать BeavioredObject,
										 // который бы небыл прив€зан к IObject
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		protected BehaviorDispatcher() { }

		public BehaviorDispatcher(ClassDefinition cls, BehaviorDispatcher parent) : this (cls, parent, null) { }
		public BehaviorDispatcher(ClassDefinition cls) : this(cls, null, null) { }
		public BehaviorDispatcher(ClassDefinition cls, BehaviorDispatcher parent, IObject instance) {
			AttachHierarchy(cls);
			InnerParent = parent;
			InnerInstance = instance;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		/// <summary>’ранит цепочки методов</summary>
		public HybridDictionary Methods {
			get { return InnerMethods; }
		}

		public ClassDefinition Class {
			get { return InnerClass; }
		}

		public BehaviorDispatcher Parent {
			get { return InnerParent; }
			set { InnerParent = value; }
		}

		public IObject Instance {
			get { return InnerInstance; }
		}

		public static bool NextMethodExists {
			get {
				MethodChainContext chainContext = MethodChainContext.CurrentSwitch as MethodChainContext;
				return (chainContext != null && chainContext.Chain != null && chainContext.Chain.Next != null);
			}
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Invoke(string name, params object[] args) {
			MethodChain chain = GetMethodChain(name);
			if (chain == null || chain.Method == null)
				Error.Critical(new MethodNotFoundException(name), typeof(BehaviorDispatcher));

			return InvokeChain(this, chain, args);
		}

		// TODO ѕровер€ть количество переданных параметров!
		public static object CallNextMethod(params object[] args) {
			object result = null;
			MethodChainContext chainContext = (MethodChainContext)MethodChainContext.CurrentSwitch;
			if (chainContext != null && chainContext.Chain.Next != null)
				result = InvokeChain(chainContext.Dispatcher, 
					chainContext.Chain.Next, 
					args ?? chainContext.Args);

			return result;
		}

		public static object InvokeChain(BehaviorDispatcher dispatcher, MethodChain chain, params object[] args) {
			object result = null;
			if (chain != null)
				using (new MethodChainContext(dispatcher, chain, args)) {
					result = dispatcher.InternalInvoke(chain.Method, args);
				}

			return result;
		}

		public virtual BehaviorDispatcher Clone(IObject instance) {
			BehaviorDispatcher result = (BehaviorDispatcher)Clone();
			result.InnerInstance = instance;
			return result;
		}

		public virtual object Clone() {
			return MemberwiseClone();
		}

		public virtual void AttachHierarchy(ClassDefinition cd) {
			if (cd == null)
				return;
			InnerClass = cd;
			FixedOrderDictionary il = cd.InheritanceList;
			for (int i = 0; i < il.Count; i++) {
				ClassDefinition c = (ClassDefinition)il[i];
				AttachClass(c);
			}
		}

		/// <summary>ƒобавл€ем в диспатчер "пр€мые" методы класса</summary>
		public virtual void AttachClass(ClassDefinition cd) {
			if (cd == null)
				return;

			// работаем с одним классом, без "наследовани€"
			foreach (MethodDefinition md in cd.GetMethods())
				AttachMethod(cd, md);
		}

		public virtual MethodDefinition AttachMethod(MethodDefinition md) {
			return AttachMethod(md.GetClass(), md);
		}

		public virtual MethodDefinition AttachMethod(ClassDefinition cd, MethodDefinition md) {
			if (md == null) return null;
			SortedList<int, MethodDefinition> methods = GetMethodList(md.Name);
			methods[GetClassIndex(cd)] = md;

			return GetMethod(md.Name); 
		}

		public virtual MethodDefinition GetMethod(string name) {
			MethodChain mc = GetMethodChain(name);
			return (mc != null) ? mc.Method : null;
		}

		public virtual MethodChain GetMethod(string className, string name) {
			MethodChain chain = null;
			IMetaInfo mi = (IMetaInfo)ProviderPublisher.Provider.GetService(typeof(IMetaInfo));
			ClassDefinition cd = mi.GetClass(className);
			chain = GetMethodChain(name);
			if (chain != null)
				chain.Index = GetClassIndex(cd);

			return chain;
		}

		public virtual bool CanInvoke(string name) {
			return GetMethodChain(name) != null;
		}

		public virtual IList<MethodDefinition> GetMethodList() {
			List<MethodDefinition> methods = new List<MethodDefinition>();
			List<string> list = new List<string>();
			if (list != null) {
				FillMethodList(list);
				list.Sort();

				foreach (string name in list)
					methods.Add(GetMethod(name));
			}

			return methods;
		}

		public virtual object GetBehavior(string cname) {
			return this;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual void FillMethodList(IList<string> methods) {
			if (Parent != null)
				Parent.FillMethodList(methods);
			if (methods != null) {
				foreach (string name in Methods.Keys)
					if (!methods.Contains(name))
						methods.Add(name);
			}
		}

		protected virtual object InternalInvoke(MethodDefinition method, params object[] args) {
			return method.Invoke(Instance, args);
		}

		protected virtual MethodChain GetMethodChain(string name) {
			MethodChain chain = null;
			List<IList<MethodDefinition>> methods = new List<IList<MethodDefinition>>();
			CombineAllMethods(name, methods);
			if (methods.Count > 0)
				chain = new MethodChain(new JointCollection<MethodDefinition>(methods.ToArray()));

			return chain;
		}

		protected virtual void CombineAllMethods(string name, List<IList<MethodDefinition>> list) {
			if (Parent != null)
				Parent.CombineAllMethods(name, list);

			SortedList<int, MethodDefinition> methods = Methods[name] as SortedList<int, MethodDefinition>;
			if (methods != null)
				list.Add(methods.Values);
		}

		protected virtual SortedList<int, MethodDefinition> GetMethodList(string name) {
			SortedList<int, MethodDefinition> list = (SortedList<int, MethodDefinition>)Methods[name];
			if (list == null)
				Methods[name] = list = new SortedList<int, MethodDefinition>();

			return list;
		}

		protected virtual int GetClassIndex(ClassDefinition cd) {
			int index = Class.InheritanceList.GetKeyIndex(cd.Name);
			// —читаем, что мы не будем работать с классами вне иерархии (конечно можно, но зачем?)
			if (index < 0)
				Error.Critical(new ApplicationException(string.Format("The class {0} is not in the hierarchy of {1}",
					cd.Name, Class.Name)), typeof(BehaviorDispatcher));
			return index;
		}
		//.........................................................................
		#endregion


		#region Nested Types
		//.........................................................................
		public class MethodChainContext : ContextSwitch<MethodChain> {

			protected object[] InnerArgs;
			protected MethodChain InnerChain;
			protected BehaviorDispatcher InnerDispatcher;

			public MethodChainContext(BehaviorDispatcher bd, MethodChain chain, params object[] args)
					: base(chain) {
				InnerArgs = args;
				InnerChain = chain;
				InnerDispatcher = bd;
			}

			public object[] Args {
				get { return InnerArgs; }
			}

			public MethodChain Chain {
				get { return InnerChain; }
			}

			public BehaviorDispatcher Dispatcher {
				get { return InnerDispatcher; }
			}
		}

		public class MethodChain : Wrapper<IList<MethodDefinition>> {
			protected int InnerIndex;

			public MethodChain(IList<MethodDefinition> methods) : this(methods, -1) { }
			public MethodChain(IList<MethodDefinition> methods, int index)
				: base(methods) {
				if (index == -1)
					index = methods.Count - 1;
				InnerIndex = index;
			}


			public MethodDefinition Method {
				get { return GetMethod(); }
			}

			public MethodChain Next {
				get { return GetNext(); }
			}

			public int Index {
				get { return InnerIndex; }
				set { InnerIndex = value; }
			}

			protected virtual MethodDefinition GetMethod() {
				return Wrapped[InnerIndex];
			}

			protected virtual MethodChain GetNext() {
				if (InnerIndex == 0)
					return null;
				return new MethodChain(Wrapped, InnerIndex - 1);
			}
		}
		//.........................................................................
		#endregion

	}

	public class MethodNotFoundException : Exception {
		protected string InnerName;

		public MethodNotFoundException(string name)
				: base(string.Format("Method is not found {0}", name)) {
			InnerName = name;
		}

		public string Name {
			get { return InnerName; }
		}
	}
}
