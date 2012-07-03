using System;
using System.Collections.Generic;
using System.Text;

namespace Front.ObjectModel {

	// TODO Впроцессе размышлений....
	public interface IObjectFactory {
		IObject Create(ClassDefinition cls, params object[] args);
		IObject Create(string className, params object[] args);

		IDataContainer CreateDataContainer(ClassDefinition cls);
		IDataContainer CreateDataContainer(string className);

		IObjectScope CreateScope(ClassDefinition cls);
		IObjectScope CreateScope(string className);
	}


	public class LObjectFactory : InitializableBase, IObjectFactory {

		#region Constructors
		//...............................................................
		public LObjectFactory() : base() { }

		public LObjectFactory(IServiceProvider sp) : base(sp) { }

		public LObjectFactory(IServiceProvider sp, bool init) : base(sp, init) {}
		//...............................................................
		#endregion


		#region IObjectFactory Members
		//.................................................................
		public virtual IObject Create(ClassDefinition cls, params object[] args) {
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual IObject Create(string className, params object[] args) {
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual IDataContainer CreateDataContainer(ClassDefinition cls) {
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual IDataContainer CreateDataContainer(string className) {
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual IObjectScope CreateScope(ClassDefinition cls) {
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual IObjectScope CreateScope(string className) {
			throw new Exception("The method or operation is not implemented.");
		}
		//.................................................................
		#endregion
	}
}
