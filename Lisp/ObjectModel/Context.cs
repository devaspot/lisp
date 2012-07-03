using System;
using System.Collections.Generic;
using System.Text;

namespace Front {

	public interface IContext {
		object this[ Type t ] { get; set; }

		object this[ string key ] { get; set; }

		object GetItem(Type t);
		void SetItem(Type t, object value);
	}

	public interface IContexBound {
		IContext Context { get; }
	}

}
