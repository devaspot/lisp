using System;
using System.Collections;
using System.Data;
using System.Text;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

using Front.Lisp;

namespace Front.ObjectModel {


	public delegate object BehaviorMethodDelegate(IObject instance, params object[] args);

	///<summary>Описание и оболочка метода</summary>
	public class MethodDefinition : SchemeNode, ISerializable {
		
		#region Protected Properties
		//.........................................................................
		protected string InnerName;
		protected string InnerDeclaredClass;
		protected BehaviorMethodDelegate InnerBody;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		protected MethodDefinition() { }

		public MethodDefinition(string name) : this(null, name, null) { }
		public MethodDefinition(string name, BehaviorMethodDelegate body) : this(null, name, body) { }
		public MethodDefinition(string className, string name, BehaviorMethodDelegate body) {
			InnerDeclaredClass = className;
			InnerName = name;
			InnerBody = body;
		}	
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public virtual string Name {
			get { return GetName(); }
			set { SetName(value); }
		}

		public virtual string DeclaredClass {
			get { return GetDeclaredClass(); }
			set { SetDeclaredClass(value); }
		}

		public virtual BehaviorMethodDelegate Body {
			get { return GetBody(); }
			set { SetBody(value); }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
			// TODO: написать!
		}

		public virtual object Invoke(IObject instance, params object[] args) {
			object result = null;
			BehaviorMethodDelegate body = Body;
			if (body != null)
				result = body(instance, args);
			else
				Error.Warning(new ApplicationException(string.Format("Method body is not found: \"{0}\"", Name)),
					typeof(MethodDefinition));

			return result;
		}

		public virtual ClassDefinition GetClass() {
			IMetaInfo mi = (IMetaInfo)ProviderPublisher.Provider.GetService(typeof(IMetaInfo));
			return mi.GetClass(DeclaredClass);
		}

		public static BehaviorMethodDelegate CreateDelegate(IFunction fn) {
			BehaviorMethodDelegate d = null;
			if (fn != null) {
				d = delegate(IObject target, object[] args) {
					object[] fargs = new object[args != null ? args.Length + 1 : 1];
					fargs[0] = target;
					if (args != null && args.Length > 0)
						args.CopyTo(fargs, 1);
					return fn.Invoke(fargs);
				};
			}

			return d;
		}

		public override string ToString() {
			return string.Format("{0} Method: {1}.{2}", GetType().Name, DeclaredClass, Name);
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual string GetName() {
			return InnerName; 
		}

		protected virtual void SetName(string name) {
			if (!CheckReadOnlyScheme())
				InnerName = name; 
		}

		protected virtual string GetDeclaredClass() {
			return InnerDeclaredClass;
		}

		protected virtual void SetDeclaredClass(string className) {
			if (!CheckReadOnlyScheme())
				InnerDeclaredClass = className;
		}

		protected virtual BehaviorMethodDelegate GetBody() {
			return InnerBody;
		}

		protected virtual void SetBody(BehaviorMethodDelegate body) {
			if (!CheckReadOnlyScheme())
				InnerBody = body;
		}
		//.........................................................................
		#endregion
	}

	
}
