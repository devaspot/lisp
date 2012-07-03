using System;
using System.Collections.Generic;
using System.Text;

namespace Front {

	public interface IContextInfo {
		object this[Type contextType] { get; set; }
		T GetContext<T>();
		void SetContext<T>(T context);
	}

	public interface IContextDepended {
		IContextInfo ContextInfo { get; set; }
	}

	public abstract class ContextDependedBase {
		public abstract IContextInfo ContextInfo { get; }
	}

	public abstract class ContextInfoBase : IContextInfo {
		public abstract object this[Type contextType] { get; set; }
		public abstract T GetContext<T>();
		public abstract void SetContext<T>(T context);
	}

	public class ContextInfo : ContextInfoBase {
		protected TypeDispatcher<object> InnerInfo = new TypeDispatcher<object>();

		public override object this[Type contextType] {
			get { return InnerInfo[contextType]; }
			set { InnerInfo[contextType] = value; }
		}

		public override T GetContext<T>() {
			return (T)InnerInfo[typeof(T)];
		}

		public override void SetContext<T>(T context) {
			InnerInfo[typeof(T)] = context;
		}
	}
}
