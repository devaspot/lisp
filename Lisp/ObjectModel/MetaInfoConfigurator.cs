using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Front.ObjectModel {

	
	public class MetaInfoConfigurator : Wrapper<IMetaInfo>, IMetaInfo, IDisposable {

		#region Protected Fields
		//.........................................................................
		protected List<ExtendInfo> InnerClassExtends = new List<ExtendInfo>();
		//.........................................................................
		#endregion


		#region De/Constructors
		//.........................................................................
		public MetaInfoConfigurator(IMetaInfo mi) : base(mi) { }
		
		~MetaInfoConfigurator() {
			Dispose();
		}
		//.........................................................................
		#endregion


		#region Events
		//.........................................................................
		public event EventHandler<BuildEmptyClassEventArgs> BeforeBuildEmptyClass;
		public event EventHandler<BuildEmptyClassEventArgs> AfterBuildEmptyClass;
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual IMetaInfo Commit() {
			foreach (ExtendInfo ei in InnerClassExtends) {
				if (ei != null)
					Wrapped.Extend(ei.ClassName, ei.ExtenderName, ei.Args);
			}

			InnerClassExtends.Clear();
			MetaInfoConfigurator mci = Wrapped as MetaInfoConfigurator;
			if (mci != null)
				return mci.Commit();				

			return Wrapped;
		}

		public virtual void Dispose() {
			Commit();
		}
		//.........................................................................
		#endregion
		
		
		#region IMetaInfo Members
		//.........................................................................
		public SchemeNode SchemeNode {
			get { return Wrapped.SchemeNode; }
		}

		public ClassDefinition GetClass(string cname) {
			ClassDefinition cd = Wrapped.GetClass(cname);
			if (cd == null) {
				cd = BuildEmptyClass(cname);
				Wrapped.RegisterClass(cd);
			}

			return cd;
		}

		public ClassDefinition GetClassVersion(string cname, Guid version) {
			return Wrapped.GetClassVersion(cname, version);
		}

		public ClassDefinition RegisterClass(ClassDefinition cls) {
			return Wrapped.RegisterClass(cls);
		}

		public void RemoveClass(string className) {
			RemoveClass(className);
		}

		public IList<string> GetClassNames() {
			return Wrapped.GetClassNames();
		}

		public IExtender GetExtender(string name) {
			return Wrapped.GetExtender(name);
		}

		public IExtender RegisterExtender(IExtender extender) {
			return Wrapped.RegisterExtender(extender);
		}

		public bool RemoveExtender(string name) {
			return Wrapped.RemoveExtender(name);
		}

		public virtual void Extend(string className, string extenderName, params object[] args) {
			if (extenderName != null)
				InnerClassExtends.Add(new ExtendInfo(className, extenderName, args));
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual ClassDefinition BuildEmptyClass(string name) {
			// TODO ј нах тут before/after. можно обойтись и одним событием.
			ClassDefinition result = null;
			BuildEmptyClassEventArgs args = new BuildEmptyClassEventArgs(name, null);
			OnBeforeBuildEmptyClass(args);
			result = args.Class ?? new ClassDefinition(name);
			args.Class = result;
			OnAfterBuildEmptyClass(args);
			return args.Class;
		}

		protected virtual void OnBeforeBuildEmptyClass(BuildEmptyClassEventArgs args) {
			EventHandler<BuildEmptyClassEventArgs> h = BeforeBuildEmptyClass;
			if (h != null)
				h(this, args);
		}

		protected virtual void OnAfterBuildEmptyClass(BuildEmptyClassEventArgs args) {
			EventHandler<BuildEmptyClassEventArgs> h = AfterBuildEmptyClass;
			if (h != null)
				h(this, args);
		}
		//.........................................................................
		#endregion


		#region Nested Types
		//.........................................................................
		public class ExtendInfo {

			protected string InnerClassName;
			protected string InnerExtenderName;
			protected object[] InnerArgs;

			public ExtendInfo(string className, string extName, params object[] args) {
				InnerExtenderName = extName;
				InnerArgs = args;
				InnerClassName = className;
			}

			public string ExtenderName {
				get { return InnerExtenderName; }
			}

			public string ClassName {
				get { return InnerClassName; }
			}

			public object[] Args {
				get { return InnerArgs; }
			}
		}
		//.........................................................................
		#endregion
	}

	public class BuildEmptyClassEventArgs : EventArgs {
		protected string InnerName;
		protected ClassDefinition InnerClass;

		public BuildEmptyClassEventArgs(string name, ClassDefinition cd) {
			InnerName = name;
			InnerClass = cd;
		}

		public string Name {
			get { return InnerName; }
		}

		public ClassDefinition Class {
			get { return InnerClass; }
			set { InnerClass = value; }
		}
	}
}
