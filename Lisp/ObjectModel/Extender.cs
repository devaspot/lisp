using System;
using System.Collections.Generic;
using System.Text;

using Front.Lisp;

namespace Front.ObjectModel {

	public delegate object ExtenderDelegate(ClassDefinition cd, params object[] args);

	// TODO Может появится какой-нить Description и другие делегаты (Attach/Detach) или еще что-то. Подумать!
	public interface IExtender {
		string Name { get; }
		bool Apply(ClassDefinition cls, params object[] args);
	}

	/// <summary>"Расширитель" класса. Расширяет (модифицирует) классы</summary>
	public class Extender : IExtender {

		#region Protected Fields
		//.........................................................................
		protected ExtenderDelegate InnerBody;
		protected string InnerName;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public Extender(string name) : this(name, null) { }
		public Extender(string name, ExtenderDelegate body) {
			InnerName = name;
			InnerBody = body;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public string Name {
			get { return InnerName; }
			set { InnerName = value; }
		}

		public ExtenderDelegate Body {
			get { return InnerBody; }
			set { InnerBody = value; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual bool Apply(ClassDefinition cls, params object[] args) {
			ExtenderDelegate body = Body;
			if (body == null) return false;

			return body(cls, args) != null;
		}

		public static ExtenderDelegate CreateExtenderBody(IFunction func) {
			ExtenderDelegate body = null;
			if (func != null)
				body = delegate(ClassDefinition cls, object[] args) {
					object[] fargs = new object[args != null ? args.Length + 1 : 1];
					fargs[0] = cls;
					if (args != null && args.Length > 0)
						args.CopyTo(fargs, 1);
					return func.Invoke(fargs);
				};

			return body;
		}
		//.........................................................................
		#endregion
	}
}
