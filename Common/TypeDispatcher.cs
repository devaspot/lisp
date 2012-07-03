using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

using Front.Collections;

namespace Front {

	public class TypeDispatcher<T> {
		protected Dictionary<Type, T> InnerDispatchTable = new Dictionary<Type, T>();
		public T NullValue; // если тип узнать невозможно (например пришел null)

		public TypeDispatcher() {
		}

		public virtual T this[Type t] {
			get { return TryGetValue(t, out t); }
			set {
				if (t == null) 
					NullValue = value;
				else
					InnerDispatchTable[t] = value; 
			}
		}

		public virtual T this[string t] {
			get {
				if (t == null) return NullValue;
				try {
					return this[Type.GetType(t)];
				} catch (Exception ex) {
					Error.Warning(ex, typeof(TypeDispatcher<object>));
				}
				return default(T);
			}
			set {
				if (t == null)
					NullValue = value;
				else
					try {
						this[Type.GetType(t)] = value;
					} catch (Exception ex) {
						Error.Warning(ex, typeof(TypeDispatcher<object>));
					}
			}
		}

		public virtual T TryGetValue(Type type, out Type real_type) {
			real_type = null;
			if (type == null) return NullValue;
			if (InnerDispatchTable.Count == 0) return default(T);

			T res;
			// TODO: расширить для работы со списками и generic'ами
			FixedOrderDictionary pl = InheritanceList.GetInheritanceList(type);
			if (pl != null)
				foreach (Type tp in pl.Keys)
					if (InnerDispatchTable.TryGetValue(tp, out res)) {
						real_type = tp;
						return res;
					}
			// в списке наследования интерфейча нет object, по этому мы его проверяем явно
			if (type.IsInterface)
				if (InnerDispatchTable.TryGetValue(typeof(object), out res))
					return res;

			return default(T);
		}

		public virtual T Remove(Type type) {
			return Remove(type, true);
		}

		public virtual T Remove(Type type, bool strict) {
			T res;
			if (type == null) {
				res = NullValue;
				NullValue = default(T);
			} else if (strict) {
				if (InnerDispatchTable.TryGetValue(type, out res))
					InnerDispatchTable.Remove(type);
			} else {
				res = TryGetValue(type, out type);
				if (type != null)
					InnerDispatchTable.Remove(type);
			}
			return res;
		}

		public virtual T Remove(string typeName) {
			return Remove(typeName, true);
		}

		public virtual T Remove(string typeName, bool strict) {
			Type t = null;

			if (typeName != null)
				try {
					t = Type.GetType(typeName);
				} catch (Exception ex) {
					Error.Warning(ex, typeof(TypeDispatcher<object>));
					return default(T);
				}

			return Remove(t, strict);
		}

		public List<T> GetValues() {
			List<T> list = new List<T>();
			list.AddRange(InnerDispatchTable.Values);
			if (NullValue != null)
				list.Add(NullValue);

			return list;
		}
	}

	//..............................................................................
	// TODO: нужно совместить TypeResolver и InheritanceListCache
	public class InheritanceList {
		protected static IDictionary<string, FixedOrderDictionary> InheritanceCache = new Dictionary<string, FixedOrderDictionary>();

		/// <summary>Составляет список наследования для указанного типа.</summary>
		public static FixedOrderDictionary GetInheritanceList(Type t) {
			if (t == null)
				return null;
			FixedOrderDictionary res;
			if (!InheritanceCache.TryGetValue(t.FullName, out res)) {
				res = PrepareInheritanceList(t);
				InheritanceCache.Add(t.FullName, res);
			}
			return res;
		}

		protected static FixedOrderDictionary PrepareInheritanceList(Type t) {
			IDictionary<Type, int> c = PrepareInheritanceList(t, new Dictionary<Type, int>(), 0);

			IList<Type> tmp = new List<Type>(c.Keys);
			FixedOrderDictionary res = new FixedOrderDictionary();
			int level = 0;
			while (tmp.Count > 0) {
				for (int i = 0; i < tmp.Count; ) {
					Type tp = tmp[i];
					if (c[tp] > level) {
						i++; continue;
					}
					res.Add(tp, c[tp]);
					tmp.RemoveAt(i);
				} level++;
			}
			return res;
		}

		protected static IDictionary<Type, int> PrepareInheritanceList(Type t, IDictionary<Type, int> tl, int depth) {
			if (tl == null)
				tl = new Dictionary<Type, int>();

			// мы еще не заходили в этот узел или узел опустидся ниже.
			if (t != null && (!tl.ContainsKey(t) || tl[t] < depth)) {
				tl[t] = depth;

				PrepareInheritanceList(t.BaseType, tl, depth + 1);

				Type[] interfaces = t.GetInterfaces();
				foreach (Type i in interfaces)
					PrepareInheritanceList(i, tl, depth + 1);
			}

			if (depth == 0) {
				// FIX: SD-17 (вытесняю Object в самый низ. заплатка!
				if (t != typeof(Object) && tl.ContainsKey(typeof(Object))) {
					int maxdepth = 1;
					foreach (int v in tl.Values)
						if (v > maxdepth)
							maxdepth = v;

					tl[typeof(Object)] = maxdepth;
				}
			}
			return tl;
		}
	}
}
