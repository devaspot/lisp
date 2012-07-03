// $Id: Wrapper.cs 424 2006-04-26 13:12:39Z pilya $

using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Front.Collections;


namespace Front {

	public class Delegator {
		public readonly AnyInvoker Delegate;
		public readonly Type DelegateType;
		public ParameterInfo[] ParamInfo;

		public Delegator(Type t, AnyInvoker i) {
			this.Delegate = i;
			this.DelegateType = t;
			this.ParamInfo = t.GetMethod("Invoke").GetParameters();
		}

		public object Invoke(params object[] args) {
			return Delegate.Invoke(args);
		}
	}

	public class GenericFunctionBottomEventArgs : CancelEventArgs {
		public object[] Args;
		public object Result;
		public GenericFunctionBottomEventArgs(params object[] args) {
			this.Args = args;
		}
	}

	/// <summary>Обобщенный делегат.</summary>
	public delegate object AnyInvoker(params object[] args);

	/// <summary>Обобщенная функция.</summary>
	/// <remarks>
	/// <para>Аналог <see cref="MulticastDelegate"/>, но с расширенной гибкостью в отношении реализации.</para>
	/// <para>Обобщенная функция может иметь различные цепочки вызываемых делегатов, в зависимости от
	/// типов (или значения) передаваемых параметров.</para>
	/// </remarks>
	public class GenericFunction : INamed {
		public static int InvokeCount = 0;
		public static int FindMethodInvokes = 0;

		// такое хранение очень неэффективно - много беготни по массивам!
		// TODO SD-6: нужен свой описатель метода и списка параметров, если метод задан как AnyInvoker,
		// но имеется ввиду определенное ограничение для типов параметров.
		protected ArrayList AroundMethods = new ArrayList();
		protected ArrayList BeforeMethods = new ArrayList();
		protected ArrayList BodyMethods = new ArrayList();
		protected ArrayList AfterMethods = new ArrayList();
		
		protected ArrayList AroundDelegators = new ArrayList();
		protected ArrayList BeforeDelegators = new ArrayList();
		protected ArrayList BodyDelegators = new ArrayList();
		protected ArrayList AfterDelegators = new ArrayList();


		/// <summary>Тип метода обобщенной функции.</summary>
		public enum MethodType { Around, Before, Body, After, None }

		protected Name InnerName = new Name("GenericFunction");

		public class ContextSwitch : Front.ContextSwitch<GenericFunction> {
			public object[] Arguments = null;
			
			public ContextSwitch( GenericFunction function, params object[] args) : base(function) { 
				Arguments = args;
			}

			public static object[] CurrentArgs {
				get {
					ContextSwitch cs = (ContextSwitch)CurrentSwitch;
					return (cs != null) ? cs.Arguments : new object[0]; 
				}
			}
		}

		public class MethodContextSwitch : Front.ContextSwitch<Delegate> {
			public MethodContextSwitch( Delegate method ) : base( method ) {
			}
		}
		
		//..........................................................................
		
		// TODO SD-3: заменить Dynamicinvoke на собственную процедуру вызова 
		// (DynamicInvoke не интерпретирует вызовы с params t[] параметрами! и не 
		// производит конвертацию параметров.)
		
		protected GenericFunction() {}
		
		public GenericFunction(string name) {
			InnerName = new Name(name);
		}

		//public GenericFunction(string name, Delegate method) : this(name) {
		// XXX пробуем ускорить малой кровью!
		public GenericFunction(string name, Type delegateType, AnyInvoker method): this(name) {
			AddMethod(MethodType.Body, delegateType, method);
		}

		#region INamed Implementation
		public string Name { 
			get { return FullName.OwnAlias;} 
			set { FullName.OwnAlias = value;} 
		}

		public Name FullName { get { return InnerName; } }
		#endregion

		public virtual bool IsSuitable(params object[] args) {
			return true;
		}

		/// <summary>Событие, которое будет вызвано, если не найдется подходящего метода.</summary>
		public event CancelEventHandler Bottom;

		public virtual MethodType GetMethodType(Delegate method) {
			MethodType res =
				(AroundMethods.Contains(method)) ? MethodType.Around
					: (BeforeMethods.Contains(method)) ? MethodType.Before
						: (BodyMethods.Contains(method)) ? MethodType.Body
							: (AfterMethods.Contains(method)) ? MethodType.After
								: MethodType.None;
			return res;
		}

		public virtual AnyInvoker Delegate { 
			get { return new AnyInvoker(Invoke); }
		}

		public virtual object Invoke(params object[] args) {
			if (args == null) args = new object[] { null };
			using ( new ContextSwitch(this, args) ) {
				Delegate around = FindMethod(MethodType.Around, args);
				if (around != null)
					using ( new MethodContextSwitch( around ) ) {
						GenericFunction.InvokeCount++;
						return ((AnyInvoker)around).Invoke( args );
					}
				return InvokeInnerMethod(true, args);
			}
		}

		/// <summary>
		/// Функция вызывается в контексте любого метода, для перехода к менее специфичной реализации.
		/// </summary>
		public static object InvokeNextMethod() {
			GenericFunction gf = ContextSwitch.Current;
			if (gf == null)
				throw new Exception("Invalid call Context"); // TODO: правильный тип Exception'а!
			Delegate dlg = MethodContextSwitch.Current;
			if (dlg == null)
				throw new Exception("Invalid call Context"); // TODO: правильный тип Exception'а!
			object[] args = ContextSwitch.CurrentArgs;
			return gf.InvokeNextMethod(dlg, args);
		}

		/// <summary>
		/// Функция должна вызываться в контексте <c>around</c>-метода обобщенной функции, для
		/// получения результата работы "внутренних" методов.
		/// </summary>
		public static object InvokeInnerMethod() {
			GenericFunction gf = ContextSwitch.Current;
			if (gf == null) 
				throw new Exception("Invalid call Context"); // TODO: правильный тип Exception'а!
			Delegate dlg = MethodContextSwitch.Current;
			if (dlg == null) 
				throw new Exception("Invalid call Context"); // TODO: правильный тип Exception'а!
			if (gf.GetMethodType(dlg) != MethodType.Around)
				throw new Exception("Invalid call Context"); // TODO: правильный тип Exception'а!
			object[] args = ContextSwitch.CurrentArgs;
			return gf.InvokeInnerMethod(false, args);
		}

		protected virtual object InvokeNextMethod(Delegate method, params object[] args) {
			Delegate nm = FindNextMethod(method,args);
			if (nm != null)
				using (new MethodContextSwitch(nm)) {
					GenericFunction.InvokeCount++;
					return ((AnyInvoker)nm).Invoke(args);
				}
			return null;
		}

		protected virtual object InvokeInnerMethod(bool fallbottom, params object[] args) {
			Delegate before;
			Delegate body;
			Delegate after;

			FindMethod(out before, out body, out after, args);

			object result = null;
			if (body != null) {
				// TODO: установить результат before и body в контекст.
				if (before != null)
					using (new MethodContextSwitch(before)) {
						GenericFunction.InvokeCount++;
						((AnyInvoker)before).Invoke(args);
					}

				using (new MethodContextSwitch(body)) {
					GenericFunction.InvokeCount++;
					result = ((AnyInvoker)body).Invoke(args);
				}

				if (after != null)
					using (new MethodContextSwitch(after)) {
						GenericFunction.InvokeCount++;
						((AnyInvoker)after).Invoke(args);
					}
			} else {
				if (fallbottom)
					result = OnBottom(args);
			}
			return result;
		}

		protected virtual void FindMethod(out Delegate before,out Delegate body, out Delegate after, params object[] args) {
			before = FindMethod(MethodType.Before, args);
			body = FindMethod(MethodType.Body, args);
			after = FindMethod(MethodType.After, args);
		}

		public virtual Delegate FindMethod(MethodType methodType, params object[] args) {
			return FindMethod(
					null,
					(methodType == MethodType.Around) ? AroundMethods
						: (methodType == MethodType.Before) ? BeforeMethods
							: (methodType == MethodType.Body) ? BodyMethods
								: AfterMethods,
					(methodType == MethodType.Around) ? AroundDelegators
						: (methodType == MethodType.Before) ? BeforeDelegators
							: (methodType == MethodType.Body) ? BodyDelegators
								: AfterDelegators,

					args);
		}

		protected virtual Delegate FindNextMethod(Delegate method, params object[] args) {
			ArrayList methods = null;
			ArrayList delegators = null;
			 
			if (AroundMethods.Contains(method)) {
				methods = AroundMethods;
				delegators = AroundDelegators;
			} else if (BeforeMethods.Contains(method)) {
				methods = BeforeMethods;
				delegators = BeforeDelegators;
			} else if (BodyMethods.Contains(method)) {
				methods = BodyMethods;
				delegators = BodyDelegators;
			} else if (AfterMethods.Contains(method)) {
				methods = AfterMethods;
				delegators = AfterDelegators;
			}

			if (methods == null) 
				throw new Exception("Method was not found!");
			return FindMethod(method, methods, delegators, args);
		}

		/// <summary>Метод, который будет вызван, если не на:qйдется подходящей реализации.</summary>
		protected virtual object OnBottom(params object[] args) {
			CancelEventHandler eh = Bottom;
			bool t = true;
			object result = null;
			if (eh != null) {
				GenericFunctionBottomEventArgs ea = new GenericFunctionBottomEventArgs(args);
				eh(this, ea);
				t = !ea.Cancel;
				result = ea.Result;
			}
			if (t)
				throw new NotImplementedException();
			return result;
		}

		//...............................................................


		#region Adding new Method
		

		public virtual void AddMethod(MethodType methodType, Type delegateType, AnyInvoker m) {
			ArrayList methods = 
					(methodType == MethodType.Around) ? AroundMethods
						: (methodType == MethodType.Before) ? BeforeMethods
							: (methodType == MethodType.Body) ? BodyMethods
								: AfterMethods;
			ArrayList delegators = 
					(methodType == MethodType.Around) ? AroundDelegators
						: (methodType == MethodType.Before) ? BeforeDelegators
							: (methodType == MethodType.Body) ? BodyDelegators
								: AfterDelegators;

			// TODO: проверить правильность метода.			
			methods.Add(m);
			delegators.Add( new Delegator(delegateType, m));
		}

		public virtual void AddMethod(Type delegateType, AnyInvoker m) {
			AddMethod( MethodType.Body, delegateType, m);
		}


		#endregion

		

		// TODO: реализация далека от эффективной!
		public static Delegate FindMethod(Delegate method, ArrayList methods, ArrayList delegators, params object[] args) {
			GenericFunction.FindMethodInvokes++;

			//SortedList NotApplicable = new SortedList();
			SortedList MethodWeigth = new SortedList();
			ArrayList m = new ArrayList(delegators);

			Type methodType = null;
			if (method != null) {
				methodType = method.GetType();
				if (!IsApplicable(methodType, args)) 
					method = null;
			}	

			Delegator MaxSuitable = null;
			Type MaxSuitableType = null;

			// находим максимально точный, но не точнее переданого (если он передан).
			for( int i=0; i< m.Count; ) {
				Delegator candidate = (Delegator)m[i];
				// если метод не подходит или 
				// он более точный, чем имеющийся, то пропускаем его
				if (! IsApplicable( candidate.ParamInfo, args) 
					|| (method != null && CompareMethods(methodType, candidate.DelegateType, args) < 0 )) {
					m.RemoveAt(i);
					continue;
				}

				if (CompareMethods(MaxSuitableType, candidate.DelegateType, args) < 0) {
					MaxSuitable = candidate;
					MaxSuitableType = candidate.DelegateType;
				}
				i++;
			}
			if (method != null && MaxSuitable != null && method.Equals(MaxSuitable.Delegate))
				MaxSuitable = null;

			return (MaxSuitable != null ) ? MaxSuitable.Delegate : null;
		}

		/// <summary>Сравнивает два метода по степени из применимости к данному списку аргументов.</summary>
		/// <returns>
		/// 1 - первый метод более точен
		/// 0 - методы одинаковы (или одинаково не подходят :-)
		/// -1 - второй метод более точен
		/// </returns>		
 		public static int CompareMethods(Type m1, Type m2, object[] args) {
			if (m1 == null) return -1;
			if (m2 == null) return 1;
			if (args == null) return 0; 
				// XXX а это не правильно! если один метод без параметров, а второй 
				// c ParamArray - то первый подходит больше.

			ParameterInfo[] p1 = m1.GetMethod("Invoke").GetParameters();
			ParameterInfo[] p2 = m2.GetMethod("Invoke").GetParameters();
			Type param1Type = null;
			Type param2Type = null;

			int result = 0;
			int result1 = 0;

			// сравнение ведем по минимальной коллекции
			int argc = args.Length;
			if (argc > p1.Length) argc = p1.Length;
			if (argc > p2.Length) argc = p2.Length;

			for (int i = 0; i < argc; i++) {
				param1Type = p1[i].ParameterType;
				param2Type = p2[i].ParameterType;

				result = CompareTypes(param1Type, param2Type, args[i]);
				if (args[i] == null) {
					// если не будет результата, то будем использовать более общий метод.
					// "общность" определяется по общности первого параметра.
					if (result1 == 0 && result != 0) result1 = -1 * result;
					continue; 
				} else
					if (result != 0) break;
			}

			if (result == 0 && result1 != 0)
				result = result1;
			// TODO: проверить попадение по числу параметров!

			return result;
		}
		
		// TODO: эти методы нужно вынести куда-то в более логичное место.
		/// <summary> Выявляет более "точный" тип для заданного значения. </summary>
		/// <returns>
		/// 1 - первый тип более точен
		/// 0 - типы одинаковы (или одинаково не подходят :-)
		/// -1 - второй тип более точен
		/// </returns>
		// TODO SD-7
		public static int CompareTypes(Type t1, Type t2, object value) {
			if (t1.IsArray) t1 = t1.GetElementType();
			if (t2.IsArray) t2 = t2.GetElementType();

			if (t1 == t2) return 0; // это один и тот же тип.

			if (value == null) {
				if (t1 == typeof(Object)) return 1;
				if (t2 == typeof(Object)) return -1;

				// если тип значение не указан - сравниваем типы между собой
				IDictionary d1 = InheritanceList.GetInheritanceList( t1 );
				if (d1.Contains(t2)) return 1; // первый тип унаследован от второго

				IDictionary d2 = InheritanceList.GetInheritanceList(t2);
				if (d2.Contains(t1)) return -1; // второй тип унаследован от первого

				return 0; // перед NULL'ом все равны :-)
			}

			Type argType = value.GetType();
			if (argType.IsArray) argType = argType.GetElementType();

			TypeApplicability a1 = TypeApplicability.Test(t1, argType);
			TypeApplicability a2 = TypeApplicability.Test(t2, argType);

			// если один из типов вобще выпадает - другой побеждает.
			if (a1.Applicable != a2.Applicable)
				return (a1.Applicable) ? 1 : -1;

			if (!a1.Applicable && !a2.Applicable) 
				return 0; // в принципе, оба типа одинаково не подходят :-)


			int result = ((int)a1.Rule < (int)a2.Rule)
						? 1
						: ((int)a1.Rule > (int)a2.Rule)
							? -1
							: 0;

			// TODO: нужно детальнее анализировать a1.Rule и a2.Rule!

			if (result == 0 ) {
				FixedOrderDictionary argh = InheritanceList.GetInheritanceList(argType);
				int v1 = (argh.ContainsKey(a1.ConvertType)) ? (int)argh[ a1.ConvertType ]: -1;
				int v2 = (argh.ContainsKey(a2.ConvertType)) ? (int)argh[ a2.ConvertType ]: -1;
				
				if ( v1 <0 && v2 >=0) return -1;
				if ( v2 <0 && v1 >=0) return 1;

				// (при прочих равных, предпочтение отдается интерфейсу)
				result = (v1 < v2 || (v1 == v2 && a1.ConvertType.IsInterface && !a2.ConvertType.IsInterface))
							? 1 // первый тип более точный 
							: (v1 > v2 || (v1 == v2 && a2.ConvertType.IsInterface && !a1.ConvertType.IsInterface))
								? -1 // второй тип более точный
								: 0;
				// TODO: проанализировать числовые типы!
			}
				
			return result;
		}


		// TODO SD-5: проверить работу с боксингом и со ссылками.
		// может сломаться на RealProxy (удаленный объект!)
		// TODO SD-6 нужен свой описатель параметров - делегат врет!
		public static bool IsApplicable(Delegate method, params object[] args) {
			return (method != null) ? IsApplicable(method.Method.GetParameters(), args) : false;
		}

		public static bool IsApplicable(Delegator dlgtr, params object[] args) {
			return (dlgtr != null) ? IsApplicable(dlgtr.ParamInfo, args) : false;
		}

		public static bool IsApplicable(Type methodType, params object[] args) {
			return (methodType != null) ? IsApplicable(methodType.GetMethod("Invoke").GetParameters(), args): false;
		}

		public static bool IsApplicable(ParameterInfo[] pi, params object[] args) {
			
			if (args == null) args = new object[] { null };
			int args_length = args.Length;
			Type paramType = null;

			// если аргументов меньше чем параметров, 
			// то последний параметр должен быть Param-Array
			if (args_length < pi.Length && !IsParamArray(pi[ pi.Length - 1 ]))
				return false;

			bool param_array = false;
			for( int i = 0; i< args_length; i++ ) {
				if (i < pi.Length)	
					paramType = pi[i].ParameterType;
					// для Param-Array'я тип аргуметна не меняется!

				
				
				if (i == pi.Length - 1) {
					// последний параметр, возможно Param-Array
					param_array = IsParamArray( pi[ i ] );

					// если агрументов больше чем параметров и последний параметр не является Param-Array'ем
					if (!param_array && args_length > pi.Length)
						return false;
						
					if (param_array) {
						paramType = paramType.GetElementType();

						if (paramType == typeof(object)) 
							// params object[] - это все ест, нечего дальше считать!
							return true;
					}
				}
				
				if (args[i] == null) continue;
				Type argType = args[i].GetType();
				
				// если последний агрумент является массивом, а последний параметр - Param-Array,
				// и массивы совместимы по типу - то вызов подходит.
				if (argType.IsArray && param_array && i == args_length - 1)
					argType = argType.GetElementType();

				TypeApplicability a = TypeApplicability.Test(paramType, argType);
				if (!a) return false;
			}

			return true;
		}


		// Приоритетность численных типов
		public static Type[] TypePriority = new Type[] {
					typeof(SByte), typeof(Byte), typeof(Int16), typeof(UInt16), typeof(Int32), typeof(UInt32),
					typeof(Int64), typeof(UInt64), typeof(Decimal),  typeof(Single), typeof(Double)
				};


		public static bool IsParamArray(ParameterInfo pi) {
			if (pi == null) return false;

			Object[] attr = pi.GetCustomAttributes(true);
			if (attr != null)
				foreach (object o in attr)
					if (o != null && o.GetType() == typeof(ParamArrayAttribute)) return true;
			return false;
		}
	
		
	}


	public enum TypeApplicabilityRule { 
		None = 0,				// типы совместимы по присвоению
		ImplictConversion =	1,	// значение требует неявной конвертации
		Degradation = 2,		// значение требует "деградации типа" (например, присвоение целого числа в float)
		Conversion = 3			// значение требует конвертации (реализует IConvertable)
	}

	/// <summary>Описывает степень применимости типа аргумент для типа параметра</summary>
	public class TypeApplicability {

		public bool Applicable = false;
		public Type ParamType;
		public Type ArgType;
		public Type ConvertType;
		public TypeApplicabilityRule Rule;

		public TypeApplicability(Type paramType, Type argType) {
			ParamType = paramType;
			ArgType = argType;
			Check();
		}

		// TODO SD-7: правильно проверять совместимость массивов!
		protected virtual void Check() {
			Applicable = false;
			Rule = TypeApplicabilityRule.None;
			ConvertType = ParamType;

			if (ParamType.IsAssignableFrom( ArgType )) {
				Applicable = true;
				return;
			}

			// проверим возможность Implict конвертации
			MemberInfo[] m = ArgType.GetMethods(BindingFlags.Static | BindingFlags.Public);
			foreach (MethodInfo mi in m)
				if (mi.Name == "op_Implicit" && ParamType.IsAssignableFrom(mi.ReturnType)) {
					Applicable = true;
					Rule = TypeApplicabilityRule.ImplictConversion;
					ConvertType = mi.ReturnType;
					// TODO DS-9: Могут быть несолько операторов, и некоторые конвертируют в более близкие типы!
					return;
				}

			// параметр имеет простой встроенный тип .NET, возможна конвертация.
			if (Type.GetTypeCode(ParamType) != TypeCode.Object) {
			
				// если аргумент IConvertiable...
				Type[] interfaces = ArgType.GetInterfaces();
				int x = Array.IndexOf(interfaces, typeof(IConvertible));
				if (x >= 0) {
					Applicable = false;
					Rule = TypeApplicabilityRule.Conversion;
					ConvertType = ArgType;
					// TODO SD-8: нужно проверить возможность конвертации!
					// рассмотреть отдельно варианты DBNull, Empty, DateTime, String, Char
					// Для числовых значений нужно проверить возможность деградации.
					return;
				}			
			}
		}

		public static implicit operator bool(TypeApplicability a) { return a.Applicable; }

		protected static IDictionary<Type,IDictionary<Type, TypeApplicability>> Cache = new Dictionary<Type,IDictionary<Type, TypeApplicability>>();
		public static TypeApplicability Test(Type paramType, Type argType) {
			if (argType == null)
				throw new ArgumentNullException("argType");
			
			IDictionary<Type, TypeApplicability> m = (Cache.ContainsKey(paramType))
				? Cache[paramType] : (Cache[paramType] = new Dictionary<Type, TypeApplicability>());

			return (m.ContainsKey(argType))
				? m[argType] : (m[argType] = new TypeApplicability( paramType, argType ));
		}
	}

	public class GenericFunction<T> : GenericFunction {

		protected GenericFunction() : base() { }
		public GenericFunction(string name) : base() { }

		public GenericFunction(string name, Type delegateType, AnyInvoker method) : base(name, delegateType, method) {
		}

		new public virtual T Invoke(params object[] args) {
			if (args == null) args = new object[] { null };
			object res = base.Invoke(args);
			if (res == null) return default(T);
			return (T)res;
		}

	}


}
