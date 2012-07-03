using System;
using System.Collections;
using System.Reflection;
using System.IO;

namespace Front.Lisp {

	// TODO Переименовать примитивы на манер Common Lisp

	public class Primitives {

		public static void InternAll(Interpreter interpreter) {
			interpreter.InternAndExport("not", new Function(not));
			interpreter.InternAndExport("to-bool", new Function(to_bool));
			interpreter.InternAndExport("nil?", new Function(nilp));
			interpreter.InternAndExport("symbol?", new Function(symbolp));
			interpreter.InternAndExport("eqv?", new Function(eqv));
			interpreter.InternAndExport("eql?", new Function(eql));
			interpreter.InternAndExport("cons", new Function(cons));
			interpreter.InternAndExport("cons?", new Function(consp));
			interpreter.InternAndExport("atom?", new Function(atomp));
			interpreter.InternAndExport("first", new Function(first));
			interpreter.InternAndExport("rest", new Function(rest));
			interpreter.InternAndExport("set-first", new Function(setfirst));
			interpreter.InternAndExport("set-rest", new Function(setrest));
			interpreter.InternAndExport("second", new Function(second));
			interpreter.InternAndExport("third", new Function(third));
			interpreter.InternAndExport("fourth", new Function(fourth));
			interpreter.InternAndExport("reverse", new Function(reverse));
			interpreter.InternAndExport("list?", new Function(listp));
			interpreter.InternAndExport("len", new Function(listlength));
			interpreter.InternAndExport("nth", new Function(nth));
			interpreter.InternAndExport("nth-rest", new Function(nthrest));
			interpreter.InternAndExport("append", new Function(append));
			interpreter.InternAndExport("concat!", new Function(concat_d));
			interpreter.InternAndExport("type-of", new Function(type_of));
			interpreter.InternAndExport("is?", new Function(isp));
			interpreter.InternAndExport("even?", new Function(evenp));
			interpreter.InternAndExport("macroexpand-1", new Function(macroexpand_1));
			interpreter.InternAndExport("vector", new Function(vector));
			interpreter.InternAndExport("vector-of", new Function(vector_of));
			interpreter.InternAndExport("throw", new Function(throwfun));
			interpreter.InternAndExport("try-catch-finally", new Function(try_catch_finally));

			interpreter.InternAndExport("<i", new Function(lti));
			interpreter.InternAndExport("addi", new Function(addints));

			BinOp addop = new BinOp();
			addop.AddMethod(typeof(Int32), typeof(Int32), new Function(addints));
			addop.AddMethod(typeof(Int32), typeof(Object), new Function(addobjs));
			addop.AddMethod(typeof(Object), typeof(Object), new Function(addobjs));
			interpreter.InternAndExport("add", addop);

			BinOp subtractop = new BinOp();
			subtractop.AddMethod(typeof(Int32), typeof(Int32), new Function(subtractints));
			subtractop.AddMethod(typeof(Int32), typeof(Object), new Function(subtractobjs));
			subtractop.AddMethod(typeof(Object), typeof(Object), new Function(subtractobjs));
			interpreter.InternAndExport("subtract", subtractop);

			BinOp multiplyop = new BinOp();
			multiplyop.AddMethod(typeof(Int32), typeof(Int32), new Function(multiplyints));
			multiplyop.AddMethod(typeof(Int32), typeof(Object), new Function(multiplyobjs));
			multiplyop.AddMethod(typeof(Object), typeof(Object), new Function(multiplyobjs));
			interpreter.InternAndExport("multiply", multiplyop);

			BinOp divideop = new BinOp();
			divideop.AddMethod(typeof(Int32), typeof(Int32), new Function(divideints));
			divideop.AddMethod(typeof(Int32), typeof(Object), new Function(divideobjs));
			divideop.AddMethod(typeof(Object), typeof(Object), new Function(divideobjs));
			interpreter.InternAndExport("divide", divideop);

			BinOp modop = new BinOp();
			modop.AddMethod(typeof(Int32), typeof(Int32), new Function(modints));
			interpreter.InternAndExport("mod", modop);

			BinOp compareop = new BinOp();
			compareop.AddMethod(typeof(Int32), typeof(Int32), new Function(subtractints));
			compareop.AddMethod(typeof(Int32), typeof(Object), new Function(compareobjs));
			compareop.AddMethod(typeof(Object), typeof(Object), new Function(compareobjs));
			interpreter.InternAndExport("compare", compareop);

			BinOp bitorop = new BinOp();
			bitorop.AddMethod(typeof(Enum), typeof(Enum), new Function(bitorenum));
			bitorop.AddMethod(typeof(Object), typeof(Object), new Function(bitor));
			interpreter.InternAndExport("bit-or", bitorop);

			BinOp bitandop = new BinOp();
			bitandop.AddMethod(typeof(Enum), typeof(Enum), new Function(bitandenum));
			bitandop.AddMethod(typeof(Object), typeof(Object), new Function(bitand));
			interpreter.InternAndExport("bit-and", bitandop);

			BinOp bitxorop = new BinOp();
			bitxorop.AddMethod(typeof(Enum), typeof(Enum), new Function(bitxorenum));
			bitxorop.AddMethod(typeof(Object), typeof(Object), new Function(bitxor));
			interpreter.InternAndExport("bit-xor", bitxorop);

			SimpleGenericFunction bitnotgf = new SimpleGenericFunction();
			bitnotgf.AddMethod(typeof(Enum), new Function(bitnotenum));
			bitnotgf.AddMethod(typeof(Object), new Function(bitnot));
			interpreter.InternAndExport("bit-not", bitnotgf);

			SimpleGenericFunction get_enum_gf = new SimpleGenericFunction();
			get_enum_gf.AddMethod(typeof(IEnumerator), new Function(get_enum_IEnumerator));
			get_enum_gf.AddMethod(typeof(IEnumerable), new Function(get_enum_IEnumerable));
			get_enum_gf.AddMethod(null, new Function(get_enum_nil));
			interpreter.InternAndExport("get-enum", get_enum_gf);

			SimpleGenericFunction strgf = new SimpleGenericFunction();
			strgf.AddMethod(null, new Function(strnil));
			strgf.AddMethod(typeof(Object), new Function(strobj));
			interpreter.InternAndExport("str", strgf);
		}

		public static Object Arg(Int32 idx, Object[] args) {
			if (idx >= args.Length) {
				throw new LispException("Insufficient arguments");
			}
			return args[idx];
		}

		//basics///////////////////////////

		public static Object not(params Object[] args) {
			return !Util.IsTrue(Arg(0, args));
		}

		public static Object to_bool(params Object[] args) {
			return Util.IsTrue(Arg(0, args));
		}

		public static Object nilp(params Object[] args) {
			Object x = Arg(0, args);
			return x == null;
		}

		public static Object symbolp(params Object[] args) {
			Object x = Arg(0, args);
			return x is Symbol;
		}

		public static Object eqv(params Object[] args) {
			Object x = Arg(0, args);
			Object y = Arg(1, args);
			return Object.Equals(x, y);
			//return x == y || (x != null && x.Equals(y));
		}

		public static Object eql(params Object[] args) {
			Object x = Arg(0, args);
			Object y = Arg(1, args);
			if (x != null && x.GetType().IsValueType) {
				return x.Equals(y);
			}
			return x == y;
		}



		//lists/////////////////////////////
		public static Object cons(params Object[] args) {
			Object x = Arg(0, args);
			Cons y = (Cons)Arg(1, args);
			return new Cons(x, y);
		}

		public static Object consp(params Object[] args) {
			Object x = Arg(0, args);
			return x is Cons;
		}

		public static Object atomp(params Object[] args) {
			Object x = Arg(0, args);
			return !(x is Cons);
		}

		public static Object first(params Object[] args) {
			Cons x = (Cons)Arg(0, args);
			if (x == null)
				return null;
			return x.First;
		}

		public static Object rest(params Object[] args) {
			Cons x = (Cons)Arg(0, args);

			if (x == null)
				return null;
			return x.Rest;
		}


		public static Object setfirst(params Object[] args) {
			Cons x = (Cons)Arg(0, args);
			Object y = Arg(1, args);
			return x.First = y;
		}

		public static Object setrest(params Object[] args) {
			Cons x = (Cons)Arg(0, args);
			Cons y = (Cons)Arg(1, args);
			return x.Rest = y;
		}

		public static Object second(params Object[] args) {
			Cons x = (Cons)Arg(0, args);
			if (x == null)
				return null;
			return Cons.GetSecond(x); ;
		}

		public static Object third(params Object[] args) {
			Cons x = (Cons)Arg(0, args);
			if (x == null)
				return null;
			return Cons.GetThird(x);
		}

		public static Object fourth(params Object[] args) {
			Cons x = (Cons)Arg(0, args);
			if (x == null)
				return null;
			return Cons.GetFourth(x);
		}

		public static Object reverse(params Object[] args) {
			Cons x = (Cons)Arg(0, args);
			if (x == null)
				return null;
			return Cons.Reverse(x);
		}

		public static Object listp(params Object[] args) {
			Object x = Arg(0, args);
			return x == null || x is Cons;
		}

		public static Object listlength(params Object[] args) {
			Cons x = (Cons)Arg(0, args);
			return Cons.GetLength(x);
		}

		public static Object nth(params Object[] args) {
			Int32 i = Convert.ToInt32(Arg(0, args));
			Cons list = (Cons)Arg(1, args);
			return Cons.GetNth(i, list);
		}

		public static Object nthrest(params Object[] args) {
			Int32 i = Convert.ToInt32(Arg(0, args));
			Cons list = (Cons)Arg(1, args);
			return Cons.GetNthTail(i, list);
		}

		public static Object append(params Object[] args) {
			Cons x = (Cons)Arg(0, args);
			Cons y = (Cons)Arg(1, args);
			return Cons.Append(x, y);
		}

		public static Object concat_d(params Object[] args) {
			Cons tail = (Cons)args[args.Length - 1];
			for (int x = args.Length - 2; x >= 0; --x) {
				Cons prev = (Cons)args[x];
				if (prev != null) {
					Cons.GetLast(1, prev).Rest = tail;
					tail = prev;
				}
			}
			return tail;
		}

		public static Object type_of(params Object[] args) {
			Object x = Arg(0, args);

			if (x == null)
				return null;
			return x.GetType();
		}

		public static Object isp(params Object[] args) {
			Object x = Arg(0, args);
			Type y = (Type)Arg(1, args);
			return y.IsInstanceOfType(x);
		}

		public static Object macroexpand_1(params Object[] args) {
			Cons x = (Cons)Arg(0, args);
			Symbol s = x.First as Symbol;
			if (s != null && s.IsDefined && s.GlobalValue is Macro) {
				Macro m = (Macro)s.GlobalValue;
				Object[] argarray = Cons.ToVector(x.Rest);
				return m.Invoke(argarray);
			} else
				return x;
		}

		public static Object vector(params Object[] args) {
			if (args.Length == 0)
				return new Object[0];
			Boolean same = true;

			if (Util.ContainsNull(args))
				same = false;
			//if homogenous, make typed vector
			if (same) {
				Type type = Arg(0, args).GetType();
				foreach (Object o in args) {
					if (!type.IsInstanceOfType(o)) {
						same = false;
						break;
					}
				}
				if (same) {
					Array result = Array.CreateInstance(type, args.Length);
					Array.Copy(args, result, args.Length);
					return result;
				}
			}
			return args.Clone();
		}

		public static Object vector_of(params Object[] args) {
			Type type = (Type)Arg(0, args);
			Array result = Array.CreateInstance(type, args.Length - 1);
			for (int i = 1; i < args.Length; i++) {
				result.SetValue(args[i], i - 1);
			}
			return result;
		}

		public static Object throwfun(params Object[] args) {
			Exception e = (Exception)Arg(0, args);
			throw e;
		}

		public static Object strnil(params Object[] args) {
			return "nil";
		}

		public static Object strobj(params Object[] args) {
			return Arg(0, args).ToString();
		}

		public static Object addobjs(params Object[] args) {
			Object arg0 = Arg(0, args);
			Object arg1 = Arg(1, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			TypeCode tc1 = Convert.GetTypeCode(arg1);
			TypeCode biggerType = tc0 > tc1 ? tc0 : tc1;
			checked {
				switch (biggerType) {
					case TypeCode.Int32:
						return Convert.ToInt32(arg0)
						+ Convert.ToInt32(arg1);
					case TypeCode.Double:
						return Convert.ToDouble(arg0)
						+ Convert.ToDouble(arg1);
					case TypeCode.Decimal:
						return Convert.ToDecimal(arg0)
						+ Convert.ToDecimal(arg1);
					case TypeCode.Int64:
						return Convert.ToInt64(arg0)
						+ Convert.ToInt64(arg1);
					default:
						return Convert.ChangeType(
														 Convert.ToDouble(arg0)
														 + Convert.ToDouble(arg1)
														 , biggerType);
				}
			}
		}

		public static Object subtractobjs(params Object[] args) {
			Object arg0 = Arg(0, args);
			Object arg1 = Arg(1, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			TypeCode tc1 = Convert.GetTypeCode(arg1);
			TypeCode biggerType = tc0 > tc1 ? tc0 : tc1;
			checked {
				switch (biggerType) {
					case TypeCode.Int32:
						return Convert.ToInt32(arg0)
						- Convert.ToInt32(arg1);
					case TypeCode.Double:
						return Convert.ToDouble(arg0)
						- Convert.ToDouble(arg1);
					case TypeCode.Decimal:
						return Convert.ToDecimal(arg0)
						- Convert.ToDecimal(arg1);
					case TypeCode.Int64:
						return Convert.ToInt64(arg0)
						- Convert.ToInt64(arg1);
					default:
						return Convert.ChangeType(
														 Convert.ToDouble(arg0)
														 - Convert.ToDouble(arg1)
														 , biggerType);
				}
			}
		}

		public static Object multiplyobjs(params Object[] args) {
			Object arg0 = Arg(0, args);
			Object arg1 = Arg(1, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			TypeCode tc1 = Convert.GetTypeCode(arg1);
			TypeCode biggerType = tc0 > tc1 ? tc0 : tc1;
			checked {
				switch (biggerType) {
					case TypeCode.Int32:
						return Convert.ToInt32(arg0)
						* Convert.ToInt32(arg1);
					case TypeCode.Double:
						return Convert.ToDouble(arg0)
						* Convert.ToDouble(arg1);
					case TypeCode.Decimal:
						return Convert.ToDecimal(arg0)
						* Convert.ToDecimal(arg1);
					case TypeCode.Int64:
						return Convert.ToInt64(arg0)
						* Convert.ToInt64(arg1);
					default:
						return Convert.ChangeType(
														 Convert.ToDouble(arg0)
														 * Convert.ToDouble(arg1)
														 , biggerType);
				}
			}
		}

		public static Object divideobjs(params Object[] args) {
			Object arg0 = Arg(0, args);
			Object arg1 = Arg(1, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			TypeCode tc1 = Convert.GetTypeCode(arg1);
			TypeCode biggerType = tc0 > tc1 ? tc0 : tc1;
			checked {
				switch (biggerType) {
					case TypeCode.Int32:
						return Convert.ToInt32(arg0)
						/ Convert.ToInt32(arg1);
					case TypeCode.Double:
						return Convert.ToDouble(arg0)
						/ Convert.ToDouble(arg1);
					case TypeCode.Decimal:
						return Convert.ToDecimal(arg0)
						/ Convert.ToDecimal(arg1);
					case TypeCode.Int64:
						return Convert.ToInt64(arg0)
						/ Convert.ToInt64(arg1);
					default:
						return Convert.ChangeType(
														 Convert.ToDouble(arg0)
														 / Convert.ToDouble(arg1)
														 , biggerType);
				}
			}
		}

		public static Object compareobjs(params Object[] args) {
			Object arg0 = Arg(0, args);
			Object arg1 = Arg(1, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			TypeCode tc1 = Convert.GetTypeCode(arg1);
			TypeCode biggerType = tc0 > tc1 ? tc0 : tc1;
			checked {
				if (arg0 is IComparable)
					return ((IComparable)arg0).CompareTo(arg1);
				else if ((arg0 is IConvertible) && (arg1 is IConvertible))
					return ((IComparable)Convert.ChangeType(arg0, biggerType)).CompareTo(Convert.ChangeType(arg1, biggerType));
				else
					return Object.Equals(arg0, arg1) ? 0 : -1; // это какая-то фигня...
			}
		}

		public static Object subtractints(params Object[] args) {
			//Int32 arg0 = (Int32)arg(0,args);
			//Int32 arg1 = (Int32)arg(1,args);
			checked {
				return (Int32)args[0] - (Int32)args[1];
			}
		}

		public static Object addints(params Object[] args) {
			//Int32 i0 = (Int32)arg(0,args);
			//Int32 i1 = (Int32)arg(1,args);
			checked {
				return (Int32)args[0] + (Int32)args[1]; ;
			}
		}

		public static Object multiplyints(params Object[] args) {
			checked {
				return (Int32)args[0] * (Int32)args[1];
			}
		}

		public static Object divideints(params Object[] args) {
			checked {
				return (Int32)args[0] / (Int32)args[1];
			}
		}

		public static Object modints(params Object[] args) {
			checked {
				return (Int32)args[0] % (Int32)args[1];
			}
		}

		public static Object lti(params Object[] args) {
			return ((Int32)args[0] - (Int32)args[1]) < 0;
		}


		public static Object evenp(params Object[] args) {
			Int32 x = Convert.ToInt32(Arg(0, args));
			return (x % 2) == 0;
		}

		public static Object try_catch_finally(params Object[] args) {
			IFunction body = Arg(0, args) as IFunction;
			IFunction catchHandler = Arg(1, args) as IFunction;
			IFunction finallyCleanup = Arg(2, args) as IFunction;
			try {
				return body.Invoke();
			} catch (Exception e) {
				if (catchHandler != null)
					return catchHandler.Invoke(e);
				else
					throw;
			} finally {
				if (finallyCleanup != null) {
					finallyCleanup.Invoke();
				}
			}
		}

		public static Object get_enum_IEnumerable(params Object[] args) {
			IEnumerable e = (IEnumerable)Arg(0, args);
			return e.GetEnumerator();
		}

		public static Object get_enum_IEnumerator(params Object[] args) {
			return (IEnumerator)Arg(0, args);
		}

		public static Object get_enum_nil(params Object[] args) {
			return new ConsEnumerator(null);
		}

		public static Object bitor(params Object[] args) {
			Object arg0 = Arg(0, args);
			Object arg1 = Arg(1, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			TypeCode tc1 = Convert.GetTypeCode(arg1);
			TypeCode biggerType = tc0 > tc1 ? tc0 : tc1;
			checked {
				switch (biggerType) {
					case TypeCode.Int32:
						return Convert.ToInt32(arg0)
						| Convert.ToInt32(arg1);
					case TypeCode.Int64:
						return Convert.ToInt64(arg0)
						| Convert.ToInt64(arg1);
					default:
						return Convert.ChangeType(
														 Convert.ToUInt32(arg0)
														 | Convert.ToUInt32(arg1)
														 , biggerType);
				}
			}
		}

		public static Object bitorenum(params Object[] args) {
			Object arg0 = Arg(0, args);
			Object arg1 = Arg(1, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			TypeCode tc1 = Convert.GetTypeCode(arg1);
			TypeCode biggerType = tc0 > tc1 ? tc0 : tc1;
			checked {
				switch (biggerType) {
					case TypeCode.Int32:
						return Enum.ToObject(arg0.GetType(), Convert.ToInt32(arg0)
													| Convert.ToInt32(arg1));
					case TypeCode.Int64:
						return Enum.ToObject(arg0.GetType(), Convert.ToInt64(arg0)
													| Convert.ToInt64(arg1));
					default:
						return Enum.ToObject(arg0.GetType(),
													Convert.ToUInt32(arg0)
													| Convert.ToUInt32(arg1));
				}
			}
		}

		public static Object bitand(params Object[] args) {
			Object arg0 = Arg(0, args);
			Object arg1 = Arg(1, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			TypeCode tc1 = Convert.GetTypeCode(arg1);
			TypeCode biggerType = tc0 > tc1 ? tc0 : tc1;
			checked {
				switch (biggerType) {
					case TypeCode.Int32:
						return Convert.ToInt32(arg0)
						& Convert.ToInt32(arg1);
					case TypeCode.Int64:
						return Convert.ToInt64(arg0)
						& Convert.ToInt64(arg1);
					default:
						return Convert.ChangeType(
														 Convert.ToUInt32(arg0)
														 & Convert.ToUInt32(arg1)
														 , biggerType);
				}
			}
		}

		public static Object bitandenum(params Object[] args) {
			Object arg0 = Arg(0, args);
			Object arg1 = Arg(1, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			TypeCode tc1 = Convert.GetTypeCode(arg1);
			TypeCode biggerType = tc0 > tc1 ? tc0 : tc1;
			checked {
				switch (biggerType) {
					case TypeCode.Int32:
						return Enum.ToObject(arg0.GetType(), Convert.ToInt32(arg0)
													& Convert.ToInt32(arg1));
					case TypeCode.Int64:
						return Enum.ToObject(arg0.GetType(), Convert.ToInt64(arg0)
													& Convert.ToInt64(arg1));
					default:
						return Enum.ToObject(arg0.GetType(),
													Convert.ToUInt32(arg0)
													& Convert.ToUInt32(arg1));
				}
			}
		}

		public static Object bitxor(params Object[] args) {
			Object arg0 = Arg(0, args);
			Object arg1 = Arg(1, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			TypeCode tc1 = Convert.GetTypeCode(arg1);
			TypeCode biggerType = tc0 > tc1 ? tc0 : tc1;
			checked {
				switch (biggerType) {
					case TypeCode.Int32:
						return Convert.ToInt32(arg0)
						^ Convert.ToInt32(arg1);
					case TypeCode.Int64:
						return Convert.ToInt64(arg0)
						^ Convert.ToInt64(arg1);
					default:
						return Convert.ChangeType(
														 Convert.ToUInt32(arg0)
														 ^ Convert.ToUInt32(arg1)
														 , biggerType);
				}
			}
		}

		public static Object bitxorenum(params Object[] args) {
			Object arg0 = Arg(0, args);
			Object arg1 = Arg(1, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			TypeCode tc1 = Convert.GetTypeCode(arg1);
			TypeCode biggerType = tc0 > tc1 ? tc0 : tc1;
			checked {
				switch (biggerType) {
					case TypeCode.Int32:
						return Enum.ToObject(arg0.GetType(), Convert.ToInt32(arg0)
													^ Convert.ToInt32(arg1));
					case TypeCode.Int64:
						return Enum.ToObject(arg0.GetType(), Convert.ToInt64(arg0)
													^ Convert.ToInt64(arg1));
					default:
						return Enum.ToObject(arg0.GetType(),
													Convert.ToUInt32(arg0)
													^ Convert.ToUInt32(arg1));
				}
			}
		}

		public static Object bitnotenum(params Object[] args) {
			Object arg0 = Arg(0, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			checked {
				switch (tc0) {
					case TypeCode.Int32:
						return Enum.ToObject(arg0.GetType(), ~Convert.ToInt32(arg0));
					case TypeCode.Int64:
						return Enum.ToObject(arg0.GetType(), ~Convert.ToInt64(arg0));
					default:
						return Enum.ToObject(arg0.GetType(),
													~Convert.ToUInt32(arg0));
				}
			}
		}

		public static Object bitnot(params Object[] args) {
			Object arg0 = Arg(0, args);
			TypeCode tc0 = Convert.GetTypeCode(arg0);
			checked {
				switch (tc0) {
					case TypeCode.Int32:
						return ~Convert.ToInt32(arg0);
					case TypeCode.Int64:
						return ~Convert.ToInt64(arg0);
					default:
						return Convert.ChangeType(
														 ~Convert.ToUInt32(arg0)
														 , tc0);
				}
			}
		}
	}
}