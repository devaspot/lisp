using System;
using System.Data;
using System.Reflection;
using System.Collections;
using System.Runtime.Remoting.Proxies;

namespace Front {



	public class GenericClone {

		// XXX Ќе дай Ѕог нам попасть в циклическую структуру.....
		// TODO: и нужно сделать этот код Thread Safe
		public static object Clone(object o) {
			if (o == null) return null;
			
			Hashtable sl = ContextSwitch<Hashtable>.GetCurrent("Front.GenericClone:CloneList");
			if (sl == null) {
				using (GenericClone.StartClone) {
					// клонируем объект в каскадном режиме.
					return Clone(o);
				}
			} else {
				Type t = o.GetType();

				if (t.IsValueType) return o;

				object res = sl[o];
				if (res == null) {
				// объект ранее не клонировалс€!

					// в начале копировани€ ставим отметку о том, что копировани€ было начато!
					// TODO: Ёту метку нужно обработать при втором проходе, иначе
					// циклические структуры будут склюнированы не правильно!
					if (sl.ContainsKey(o)) return null;
					sl[o] = null;


					if ((o is MarshalByRefObject) || (o is RealProxy) || (o is WeakReference)) {
						// объекты этих типов не клонируютс€!
						return o;

					} else if (o is ICloneable) {
						// TODO: Ќужно иметь возможность подменить процедуру клонировани€ 
						// дл€ некоторых ICloneable,таких как ArrayList, которые не клонируют свои элементы!
						res = ((ICloneable)o).Clone();
 
					} else {
						
						MethodInfo mi = t.GetMethod("Clone", 
							BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
							BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy, null, new Type[0], null);

						if (mi != null) {
							try {
								res = mi.Invoke(o, null);
							} catch (Exception ex) {
								// подумать, как это можно обработать?
							}
						} else {
							// хитрожопый вызов MemberwiseClone....
							// мы не дублируем структуру полностью!
							mi = t.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
							res = mi.Invoke(o, null);
						}
					}
					sl[o] = res;
				}
				return res;
			}	
		}

		public static ContextSwitch<Hashtable> StartClone { 
			get {
				// при попытке вложенных Using(StartClone), нужно игнорировать все вложенные!
				if (ContextSwitch<Hashtable>.GetCurrentSwitch("Front.GenericClone:CloneList") == null)
					return new ContextSwitch<Hashtable>(new Hashtable(), "Front.GenericClone:CloneList");
				else
					return new ContextSwitch<Hashtable>(null, "Front.GenericClone.SecondaryClone");
			}
		}

	}
}
