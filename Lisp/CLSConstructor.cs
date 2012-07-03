using System;
using System.Reflection;

namespace Front.Lisp {

	public class CLSConstructor {

		#region Protected Fields
		//.........................................................................
		protected Type InnerType;
		protected string InnerName;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public CLSConstructor(Type type) {
			InnerType = type;
			InnerName = type.Name;
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public static Object Invoke(Type type, params object[] args) {
			//if(Util.containsNull(args)) {

			Type[] argtypes = Util.GetTypeArray(args);//Type.GetTypeArray(args);
			ConstructorInfo ci = type.GetConstructor(argtypes);
			if (ci == null) {
				// TODO: Show type list in message
				throw new LispException("Can't find matching Constructor for: " + type.Name + " taking those arguments");
			}
			return ci.Invoke(Util.GetValues(args));
			//}
		}
		//.........................................................................
		#endregion
	}
}
