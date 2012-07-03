using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Front {

	// XXX Продумать

	public interface ITypeResolver {
		Type GetType(string name);
		IDictionary<string, Type> Types { get; }
		IDictionary<string, Type> FullNameTypes { get; }
	}

	// TODO довести до ума

	public class TypeResolver : ITypeResolver {
		protected Dictionary<string, Type> InnerTypes = new Dictionary<string, Type>();
		protected Dictionary<string, Type> InnerFullNameTypes = new Dictionary<string, Type>();
		public TypeResolver() { }

		public IDictionary<string, Type> Types { get { return InnerTypes; } }
		public IDictionary<string, Type> FullNameTypes { get { return InnerFullNameTypes; } }

		// TODO что делать если имена совподают в разных сборках?
		// Продумать алгоритм регистрации и поиска!
		public virtual Type GetType(string name) {
			Type result = null;
			InnerTypes.TryGetValue(name, out result);
			if (result == null)
				InnerFullNameTypes.TryGetValue(name, out result);

			return result;
		}

		public virtual void RegisterAssembly(Assembly asm) {
			RegisterAssembly(asm, null);
		}

		public virtual void RegisterAssembly(Assembly asm, Predicate<Type> filter) {
			if (asm == null)
				Error.Warning(new ArgumentNullException("asm"), typeof(ITypeResolver));
			else {
				foreach (Type t in asm.GetTypes()) {
					if (filter != null)
						if (!filter(t))
							continue;
					RegisterType(t);
				}
			}
		}

		public virtual void RegisterType(Type t) {
			InnerTypes[t.Name] = t;
			InnerTypes[t.FullName] = t;
		}

		public static ITypeResolver Create() {
			TypeResolver tr = new TypeResolver();
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
				tr.RegisterAssembly(asm);

			return tr;
		}
	}
}
