using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;

using Front;
using Front.Collections.Generic;


namespace Front.Lisp {

	// TODO GlobalEvironment в лиспе должен использовать EnvironmentSwitcher. Нужны макросы/функции для управления средами

	// TODO Можно заоптимизировать семейные отношения в "среде"
	

	public interface IEnvironment {
		IEnvironment Parent { get; }

		object this[LocalVariable var] { get; set; }
		
		object[] Values { get; }
		Parameter[] Vars { get; }

		object Lookup(Symbol var);
		IEnvironment Rib(int level);
	}

	/// <summary>Определяет окружение</summary>
	public class Environment : IEnvironment {

		#region Protected Fields
		//.........................................................................
		protected IEnvironment InnerParent;
		protected Parameter[] InnerVars;
		protected object[] InnerValues;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public Environment(Parameter[] vars, Object[] vals, IEnvironment parent) {
			InnerVars = vars;
			InnerValues = vals;
			InnerParent = parent;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public object this[LocalVariable var] {
			get { return GetValue(var); }
			set { SetValue(var, value); }
		}

		public object[] Values {
			get { return InnerValues; }
		}

		public IEnvironment Parent {
			get { return InnerParent; }
		}

		public Parameter[] Vars {
			get { return InnerVars; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual Object Lookup(Symbol var) {
			Int32 level = 0;
			for (IEnvironment e = this; e.Parent != null; e = e.Parent, ++level) {
				for (Int32 i = 0; i < e.Vars.Length; i++) {
					if (e.Vars[i].Symbol == var)
						return new LocalVariable(level, i, var);
				}
			}
			return var;
		}

		public IEnvironment Rib(int level) {
			IEnvironment ret = this;
			while (level > 0) {
				ret = ret.Parent;
				--level;
			}
			return ret;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual Object GetValue(LocalVariable var) {
			return Rib(var.Level).Values[var.Index];
		}

		// TODO Подумать над тем, чтобы не давать изменять если сейчас мы не в своем environment-е
		protected virtual void SetValue(LocalVariable var, Object newVal) {
			Rib(var.Level).Values[var.Index] = newVal;
		}
		//.........................................................................
		#endregion
	}

	public class EnvironmentWrapper : Wrapper<IEnvironment>, IEnvironment {

		#region Constructors
		//.........................................................................
		public EnvironmentWrapper(IEnvironment env) : base(env) { }
		//.........................................................................
		#endregion


		#region IEnvironment Members
		//.........................................................................
		public IEnvironment Parent {
			get { return GetParent(); }
		}

		public object this[LocalVariable var] {
			get { return GetValue(var); }
			set { SetValue(var, value);	}
		}

		public object[] Values {
			get { return GetValues(); }
		}

		public Parameter[] Vars {
			get { return GetVars(); }
		}

		public virtual object Lookup(Symbol var) {
			return Wrapped.Lookup(var);
		}

		public virtual IEnvironment Rib(int level) {
			return Wrapped.Rib(level);
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual object GetValue(LocalVariable var) {
			return Wrapped[var];
		}

		protected virtual void SetValue(LocalVariable var, object value) {
			Wrapped[var] = value;
		}

		protected virtual object[] GetValues() {
			return Wrapped.Values;
		}

		protected virtual Parameter[] GetVars() {
			return Wrapped.Vars;
		}

		protected virtual IEnvironment GetParent() {
			return Wrapped.Parent;
		}
		//.........................................................................
		#endregion
	}

	public class ContextEnvironment : Environment {

		#region Protected Fields
		//.........................................................................
		protected string InnerName;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public ContextEnvironment(string name, Parameter[] vars, Object[] vals, IEnvironment parent)
				: base(vars, vals, parent) {
			InnerName = name;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public string Name {
			get { return InnerName; }
		}
		//.........................................................................
		#endregion
	}

	public class EnvironmentGlue : EnvironmentWrapper {

		#region Protected Fields
		//.........................................................................
		protected IEnvironment InnerParent;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public EnvironmentGlue(IEnvironment parent, IEnvironment env) : base (env) {
			InnerParent = parent;
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override object Lookup(Symbol var) {
			int level = 0;
			for (IEnvironment e = this; e.Parent != null; e = e.Parent, ++level) {
				for (int i = 0; i < e.Vars.Length; i++) {
					if (e.Vars[i].Symbol == var)
						return new LocalVariable(level, i, var);
				}
			}

			return var;
		}

		public override IEnvironment Rib(int level) {
			IEnvironment result = this;
			while (level > 0) {
				result = result.Parent;
				--level;
			}

			return result;
		}
		//.........................................................................
		#endregion


		#region Protected Fields
		//.........................................................................
		protected override object GetValue(LocalVariable var) {
			return Rib(var.Level).Values[var.Index];
		}

		protected override void SetValue(LocalVariable var, object value) {
			Rib(var.Level).Values[var.Index] = value;
		}

		protected override IEnvironment GetParent() {
			return InnerParent;
		}
		//.........................................................................
		#endregion
	}

	// Переключает окружения.
	// Тут должны храниться списки окружений
	public class EnvironmentSwitcher : EnvironmentWrapper {

		#region Protected Fields
		//.........................................................................
		protected HybridDictionary InnerEnvironments = new HybridDictionary(); // string-> ContextEnvironment
		protected IEnvironment InnerGlobalEnvironment;
		//.........................................................................
		#endregion

		public EnvironmentSwitcher() : base(null) { }

		#region Public Properties
		//.........................................................................
		public IEnvironment GlobalEnvironment {
			get { return GetGlobalEnvironment(); }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual IEnvironment UseEnvironment(params string[] envs) {
			IEnvironment result = GlobalEnvironment;
			if (envs != null) {
				foreach (string env in envs) {
					IEnvironment e = InnerEnvironments[env.ToLower().Trim()] as IEnvironment;
					if (e != null) {
						result = new EnvironmentGlue(result, e);
					}
				}
			}

			UseEnvironment(result);

			return result;
		}

		public virtual void UseEnvironment(IEnvironment e) {
			InnerWrapped = e;
		}

		public virtual void RegisterEnvironment(ContextEnvironment e) {
			if (e != null)
				RegisterEnvironment(e.Name, e);
		}

		public virtual void RegisterEnvironment(string name, IEnvironment e) {
			if (name != null && e != null)
				InnerEnvironments[name.ToLower().Trim()] = e;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual IEnvironment GetGlobalEnvironment() {
			if (InnerGlobalEnvironment == null)
				InnerGlobalEnvironment = new Environment(null, null, null);

			return InnerGlobalEnvironment;
		}
		//.........................................................................
		#endregion
	}

}