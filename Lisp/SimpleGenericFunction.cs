using System;
using System.Collections;
using System.Collections.Specialized;

namespace Front.Lisp {
	
	// DEPRECATED!!! For internal use only!

	public class BinOpList {

		#region Protected Fields
		//.........................................................................
		protected HybridDictionary InnerMethods = new HybridDictionary();
		protected HybridDictionary InnerCache = new HybridDictionary();
		protected object InnerNullFunc = null;
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public Object NullFunc {
			get { return InnerNullFunc; }
			set { InnerNullFunc = value; }
		}

		public IDictionary Methods {
			get { return InnerMethods; }
		}

		public IDictionary Cache {
			get { return InnerCache; }
		}
		//.........................................................................
		#endregion

	}

	public class BinOpKey {

		#region Protected Fields
		//.........................................................................
		protected Type InnerType1;
		protected Type InnerType2;
		protected Int32 hash;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public BinOpKey(Type t1, Type t2) {
			InnerType1 = t1;
			InnerType2 = t2;
			hash = t1.GetHashCode() + t2.GetHashCode();
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override Boolean Equals(Object other) {
			BinOpKey ob = other as BinOpKey;
			return ob != null && ob.InnerType1 == InnerType1 && ob.InnerType2 == InnerType2;
		}

		public override Int32 GetHashCode() {
			return hash;
		}
		//.........................................................................
		#endregion

	}

	public class BinOpCacheEntry {

		#region Protected Fields
		//.........................................................................
		protected Object InnerValue;
		protected Type InnerKey1;
		protected Type InnerKey2;
		protected BinOpCacheEntry InnerNext = null;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public BinOpCacheEntry(Type key1, Type key2, Object val, BinOpCacheEntry next) {
			InnerKey1 = key1;
			InnerKey2 = key2;
			InnerValue = val;
			InnerNext = next;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public object Value {
			get { return InnerValue; }
			set { InnerValue = value; }
		}

		public BinOpCacheEntry Next {
			get { return InnerNext; }
		}

		public Type Key1 {
			get { return InnerKey1; }
		}

		public Type Key2 {
			get { return InnerKey2; }
		}
		//.........................................................................
		#endregion
	}

	//binops don't support dispatch on value for efficiency
	public class BinOp : IFunction {

		#region Protected Fields
		//.........................................................................
		protected HybridDictionary InnerMethodLists = new HybridDictionary(); //Type -> BinOpList	
		protected HybridDictionary InnerMethodCache = new HybridDictionary(); //BinOpKey -> Object
		protected BinOpCacheEntry InnerCache = null;
		//.........................................................................
		#endregion

		#region Public Methods
		//.........................................................................
		public virtual object Invoke(params object[] args) {
			if (args.Length != 2)
				throw new LispException("Must pass exactly 2 args to BinOp");
			object f = FindBestMethod(args[0], args[1]);
			if (f == null) {
				throw new LispException("Error - no BinOp method found matching arguments: "
										  + args[0] + " " + args[1]);
			}

			return Util.InvokeObject(f, args);
		}

		public virtual void AddMethod(object dispatch1, object dispatch2, object func) {
			BinOpList methods = (BinOpList)InnerMethodLists[dispatch1];

			if (methods == null)
				InnerMethodLists[dispatch1] = methods = new BinOpList();

			methods.Methods[dispatch2] = func;

			ClearCache();
		}

		public object FindBestMethod(object dispatch1, object dispatch2) {
			Object method = null;
			//try to find in cache
			Type t1 = dispatch1.GetType();
			Type t2 = dispatch2.GetType();
			//method = findCachedMethod(key);
			method = FindCachedMethod(t1, t2);
			if (method != null)
				return method;
			//try to find type
			BinOpList methods = (BinOpList)FindBestMatch(t1, InnerMethodLists);
			if (methods != null) {
				method = FindBestMatch(t2, methods.Methods);
				if (method != null) {
					CacheMethod(t1, t2, method);
					return method;
				}
			}
			return null;
		}

		public static Object FindBestMatch(Type argType, IDictionary methods) {
			//walk through to find best match
			Type bestType = null;
			Object best = null;
			foreach (DictionaryEntry e in methods) {
				Type tryType = e.Key as Type;
				if (tryType != null) {
					if (tryType.IsAssignableFrom(argType)
						&& SimpleGenericFunction.IsMoreSpecific(tryType, bestType)) {
						bestType = tryType;
						best = e.Value;
					}
				}
			}
			return best;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual void ClearCache() {
			InnerCache = null;
		}

		protected virtual void CacheMethod(BinOpKey key, Object method) {
			InnerMethodCache[key] = method;
		}

		protected virtual void CacheMethod(Type t1, Type t2, Object method) {
			//if can find an existing entry, swap the value
			BinOpCacheEntry e = FindCacheEntry(t1, t2);
			if (e == null)
				InnerCache = new BinOpCacheEntry(t1, t2, method, InnerCache);
			else
				e.Value = method;
		}

		protected Object FindCachedMethod(BinOpKey key) {
			return InnerMethodCache[key];
		}

		protected Object FindCachedMethod(Type t1, Type t2) {
			BinOpCacheEntry entry = FindCacheEntry(t1, t2);
			if (entry != null)
				return entry.Value;

			return null;
		}

		protected BinOpCacheEntry FindCacheEntry(Type t1, Type t2) {
			for (BinOpCacheEntry e = InnerCache; e != null; e = e.Next) {
				if (e.Key1 == t1 && e.Key2 == t2) {
					return e;
				}
			}
			return null;
		}
		//.........................................................................
		#endregion

	}

	public class SimpleGenericFunction : IFunction {

		#region Protected Fields
		//.........................................................................
		protected Object InnerNullFunc = null;
		protected HybridDictionary InnerMethods = new HybridDictionary();
		protected HybridDictionary InnerCache = new HybridDictionary();
		protected HybridDictionary InnerBaseCache = new HybridDictionary();
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public Object Invoke(params Object[] args) {
			Object f = FindBestMethod(args[0]);
			if (f == null) {
				throw new LispException("Error - no method found matching argument: "
										  + args[0]);
			}
			return Util.InvokeObject(f, args);
		}

		public void AddMethod(Object dispatch, Object func) {
			if (dispatch == null)
				InnerNullFunc = func;
			else
				InnerMethods[dispatch] = func;

			InnerCache.Clear();
			InnerBaseCache.Clear();
		}

		public void RemoveMethod(Object dispatch) {
			InnerMethods.Remove(dispatch);
			InnerCache.Clear();
			InnerBaseCache.Clear();
		}

		public Object FindBaseMethod(Type argType) {
			//try the base cache
			Object cacheMatch = InnerBaseCache[argType];
			if (cacheMatch != null)
				return cacheMatch;
			//walk through to find best match
			Type bestType = null;
			Object bestFunc = null;
			foreach (DictionaryEntry e in InnerMethods) {
				Type tryType = e.Key as Type;
				if (tryType != null) {
					if (tryType != argType && tryType.IsAssignableFrom(argType)
						&& IsMoreSpecific(tryType, bestType)) {
						bestType = tryType;
						bestFunc = e.Value;
					}
				}
			}
			if (bestType != null) {
				InnerBaseCache[argType] = bestFunc;
			}
			return bestFunc;
		}

		public Object FindBestMethod(Object dispatch) {
			if (dispatch == null)
				return InnerNullFunc;
			else {
				//try to find the value
				Object valueMatch = InnerMethods[dispatch];
				if (!(dispatch is Type) && valueMatch != null)
					return valueMatch;
				else {
					Type argType = dispatch.GetType();
					//try the cache
					Object cacheMatch = InnerCache[argType];
					if (cacheMatch != null)
						return cacheMatch;
					//walk through to find best match
					Type bestType = null;
					Object bestFunc = null;
					foreach (DictionaryEntry e in InnerMethods) {
						Type tryType = e.Key as Type;
						if (tryType != null) {
							if (tryType.IsAssignableFrom(argType)
								&& IsMoreSpecific(tryType, bestType)) {
								bestType = tryType;
								bestFunc = e.Value;
							}
						}
					}
					if (bestType != null) {
						InnerCache[argType] = bestFunc;
					}
					return bestFunc;
				}
			}
		}

		public static Boolean IsMoreSpecific(Type t1, Type t2) {
			if (t2 == null)
				return true;
			return (t2.IsAssignableFrom(t1) && !t1.IsAssignableFrom(t2));
		}
		//.........................................................................
		#endregion
	}
}