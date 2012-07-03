using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace Front.Common {

	public interface IAssemblyLoadStrategy {
		Assembly Load(string assemblyPath);
	}

	class DefaultAssemblyLoadStrategy : IAssemblyLoadStrategy {
		public Assembly Load(string assemblyPath) {
			return Assembly.LoadFile(assemblyPath);
		}
	}

	class ReflectionOnlyAssemblyLoadStrategy : IAssemblyLoadStrategy {
		public Assembly Load(string assemblyPath) {
			return Assembly.ReflectionOnlyLoadFrom(assemblyPath);
		}
	}

	public class AssemblyResolver {

		#region Singleton
		//.........................................................................
		static protected AssemblyResolver InnerCurrent;
		static public AssemblyResolver Current {
			get {
				if (InnerCurrent == null) InnerCurrent = new AssemblyResolver();
				return InnerCurrent;
			}
		}
		//.........................................................................
		#endregion

		public AssemblyResolver() {
			InnerPathes = new List<string>();
			InnerPathes.Add(AppDomain.CurrentDomain.BaseDirectory);
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(CurrentDomain_ReflectionOnlyAssemblyResolve);
		}

		public void AppendPath(string path) {
			if (!InnerPathes.Contains(path)) 
				InnerPathes.Add(path);
		}

		protected virtual Assembly LoadAssembly(string assemblyName, IAssemblyLoadStrategy loadStrategy) {
			Assembly a = System.Reflection.Assembly.ReflectionOnlyLoad(assemblyName);
			if (a != null) return a;
			System.Reflection.AssemblyName an = new System.Reflection.AssemblyName(assemblyName);
			string asmNameWithExt = an.Name + ".dll";
			foreach (string path in InnerPathes) {
				string assemblyPath = Path.Combine(path, asmNameWithExt);//Path.ChangeExtension(Path.Combine(path, an.Name), "dll");
				if (!File.Exists(assemblyPath)) continue;
				return loadStrategy.Load(assemblyPath);
			}
			return null;
		}

		protected virtual System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
			return LoadAssembly(args.Name, new DefaultAssemblyLoadStrategy());
		}

		protected virtual Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args) {
			return LoadAssembly(args.Name, new ReflectionOnlyAssemblyLoadStrategy());
		}

		protected List<string> InnerPathes;
	}
}
