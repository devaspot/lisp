using System;
using System.Collections.Generic;
using System.Text;

namespace Front.ObjectModel {


	public interface IObjectScope {
		IObjectScopeBound Get(object key);
		IObject Get(string className, long objectID);
		//T Get<T>(long objectID); хороший метод!

		IObjectScopeBound Merge(IObjectScopeBound obj, ScopeMergeMode mode);		
		// TODO: нужны методы получения списка объектов в скоупе (живых, а не потенциальных)
		// Так же нужны методы типа Merge, Attache и т.п.
		object UnWrap(object obj);

		IDataContainer NewDataContainer(ClassDefinition cls);
		IDataContainer NewDataContainer(string className);

		T New<T>();
	}


	public interface IObjectScopeBound {
		IObjectScope ObjectScope {
			get;
		}

		bool Attach(IObjectScope scope);
	}

	public enum ScopeMergeMode {
		// TODO: нуждаеться в продумывании. написано отфонаря!
		Add,
		Copy,
		Check,
		Diff,
		Strong,
		Replace
	}

	public delegate IDataContainer DataContailerFactoryDelegate(string clsname, object o, params object[] args);
}
