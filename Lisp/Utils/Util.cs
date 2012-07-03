using System;

namespace Front.Lisp {

	public class Util {
		public static Object[] EMPTY_VECTOR = new Object[0];

		public static Object[] VectorPush(Object x, Object[] a) {
			Object[] ret = new Object[a.Length + 1];
			Array.Copy(a, 0, ret, 1, a.Length);
			ret[0] = x;
			return ret;
		}

		public static Object[] VectorRest(Object[] a) {
			Object[] ret = new Object[a.Length - 1];
			Array.Copy(a, 1, ret, 0, a.Length - 1);
			return ret;
		}

		public static T[] VectorRest<T>(T[] a) {
			T[] ret = new T[a.Length - 1];
			Array.Copy(a, 1, ret, 0, a.Length - 1);
			return ret;
		}

		public static Boolean ContainsNull(Object[] v) {
			foreach (Object o in v) {
				if (o == null)
					return true;
			}
			return false;
		}

		public static Boolean IsTrue(Object o) {
			return o != null && ((o is Boolean) ? (Boolean)o : true);
		}

		public static void Error(String message) {
			throw new LispException(message);
		}

		public static Object InvokeObject(Object f, params Object[] args) {
			Function func = f as Function;
			if (func != null)
				return func(args);

			IFunction ifunc = f as IFunction;
			if (ifunc != null)
				return ifunc.Invoke(args);


			else if (f is Type) {
				Type t = (Type)f;
				//try to support ctor-style init of Int32 etc. which don't have ctors
				//unfortunately, IntPtr IsPrimitive but has a ctor!
				if (t.IsPrimitive)
					return Convert.ChangeType(args[0], t);
				//CLSConstructor ctor = new CLSConstructor(t);
				return CLSConstructor.Invoke(t, args);
			} else if (args.Length == 1 && !(args[0] is Cons) ) {
				//treat as indexed get
				Array a = args[0] as Array;
				if (a != null) {
					if (f is Int32)
						return a.GetValue((Int32)f);
					else
						return a.GetValue((Int32[])f);
				} else { //not an array, try default member
					if (f is Array)
						return CLSMember.GetDefaultIndexedProperty(args[0], (Object[])f);
					else
						return CLSMember.GetDefaultIndexedProperty(args[0], new Object[] { f });
				}
			} else if (args.Length == 2 && !(args[0] is Cons)) {
				//treat as indexed set
				Object val = args[1];
				Object target = args[0];
				Array a = target as Array;
				if (a != null) {
					if (f is Int32)
						a.SetValue(val, (Int32)f);
					else
						a.SetValue(val, (Int32[])f);
				} else {//not an array, try default member
					if (f is Array) {
						Array af = f as Array;

						Object[] subsandval = new Object[af.Length + 1];
						Array.Copy(af, subsandval, af.Length);
						subsandval[af.Length] = args[1];
						CLSMember.SetDefaultIndexedProperty(target, subsandval);
					} else
						CLSMember.SetDefaultIndexedProperty(target, new Object[] { f, val });
				}
				return args[1];
			}
			//what should policy be on invoke nil?
			if (f == null)
				return null;
			throw new LispException("Don't know how to invoke: " + f);
			//+ " with args: " + Primitives.strgf.Invoke((Object)args));
		}

		public static Type[] GetTypeArray(params object[] args) {
			Type[] res = new Type[args.Length];
			for (int i = 0; i < args.Length; i++)
				res[i] = (args[i] != null)
					? ((args[i] is CastInfo) ? ((CastInfo)args[i]).Type : args[i].GetType())
					: typeof(object);
			return res;
		}

		public static object[] GetValues(params object[] args) {
			object[] values = new object[args.Length];
			for (int i = 0; i < args.Length; i++)
				if (args[i] is CastInfo) {
					CastInfo ci = (CastInfo)args[i];
					if (ci.Value is IConvertible)
						values[i] = Convert.ChangeType(ci.Value, ci.Type);
					else
						values[i] = ci.Value;
				} else {
					values[i] = args[i]; 
					
				}

			return values;
		}
	}

	public class CastInfo {
		protected Type InnerType;
		protected object InnerValue;

		public CastInfo(Type t, object value) {
			InnerType = t;
			InnerValue = value;
			if (InnerType == null && value != null)
				InnerType = value.GetType();
		}

		public Type Type { get { return InnerType; } }
		public object Value { get { return InnerValue; } }
	}
}