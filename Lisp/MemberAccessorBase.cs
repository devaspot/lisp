using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Front.Lisp {
	
	public abstract class MemberAccessorBase : IFunction {

		#region Protected Fields
		//.........................................................................
		protected Type InnerType;
		protected string InnerName;
		protected MemberInfo InnerMemberInfo;
		protected Type[] InnerArgs;
		//.........................................................................
		#endregion

		#region Constructors
		//.........................................................................
		public MemberAccessorBase(Type type, string name, params Type[] args) {
			if (args == null)
				args = new Type[0];
			if (type == null)
				type = typeof(object);

			InnerType = type;
			InnerName = name;
			InnerArgs = args;
		}
		//.........................................................................
		#endregion


		#region Pubic Properties
		//.........................................................................
		public Type Type {
			get { return InnerType; }
		}

		public string Name { 
			get { return InnerName; }
		}

		public Type[] Args {
			get { return InnerArgs; }
		}

		public MemberInfo MemberInfo {
			get { return GetMemberInfo(); }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public abstract object Invoke(params object[] args);

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append(Type.Name);
			sb.Append('.');
			sb.Append(Name);
			sb.Append("(");
			for (int i = 0; i < Args.Length; i++) {
				if (i > 0)
					sb.Append(", ");
				sb.Append(Args[i].Name);
			}
			sb.Append(")");	
			return sb.ToString();
		}
		//.........................................................................
		#endregion

		#region Protected Methods
		//.........................................................................
		protected virtual MemberInfo GetMemberInfo() {
			if (InnerMemberInfo == null)
				InnerMemberInfo = RetrieveMember(Type, Name, Args);

			return InnerMemberInfo;
		}

		protected abstract MemberInfo RetrieveMember(Type t, string name, Type[] args);
		//.........................................................................
		#endregion
	}
}
