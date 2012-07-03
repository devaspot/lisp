using System;
using System.Collections.Generic;
using System.Text;

namespace Front {

	public class NamedTypeDispatcher<T> {
		protected TypeDispatcher<IDictionary<Name, T>> InnerDispatcher = new TypeDispatcher<IDictionary<Name, T>>();

		public T this[Name name, Type type] {
			get { return InternalGet(name, type); }
			set { InternalSet(name, type, value); }
		}

		public virtual T Base(Name name, Type type) {
			T result = default(T);
			if (type.BaseType != null)
				result = this[name, type.BaseType];

			return result;
		}

		protected virtual T InternalGet(Name name, Type type) {
			T result = default(T);
			IDictionary<Name, T> dict = InnerDispatcher[type];
			if (dict != null)
				dict.TryGetValue(name, out result);

			return result;
		}

		protected virtual void InternalSet(Name name, Type type, T value) {
			Type t;
			IDictionary<Name, T> dict = InnerDispatcher.TryGetValue(type, out t);
			if (dict == null || !t.Equals(type)) {
				dict = new Dictionary<Name, T>();
				InnerDispatcher[type] = dict;
			}

			dict[name] = value;
		}		
	}
}
