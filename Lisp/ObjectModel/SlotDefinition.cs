using System;
using Front.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Front.Lisp;

namespace Front.ObjectModel {
	

	/// <summary>Описание слота класса</summary>
	public class SlotDefinition : SchemeNode, ISerializable {

		#region Protected Fields
		//.........................................................................
		protected string InnerName;
		protected IFunction InnerDefaultValueAccessor;
		protected object InnerDefaultValue;
		protected string InnerDeclaredClass;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		protected SlotDefinition() {
		}

		public SlotDefinition(string className, string name, object defaultValue) : this(className, name, (IFunction)null) {
			InnerDefaultValue = defaultValue;
		}

		public SlotDefinition(string className, string name, IFunction defaultValue) {
			InnerDeclaredClass = className;
			InnerName = name;
			InnerDefaultValueAccessor = defaultValue;
		}

		public SlotDefinition(string name, IFunction defaultValue) : this(null, name, defaultValue) {
		}

		public SlotDefinition(string className, string name) : this(className, name, null) {
		}

		public SlotDefinition(string name) : this(name, (IFunction)null)  {
		}

		public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
			// TODO: написать!
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public string Name {
			get { return InnerName; }
			set {
				if (! CheckReadOnlyScheme())
					InnerName = value;
			}
		}

		public object DefaultValue {
			get { return GetDefaultValue(); }
			set { 
				if (!CheckReadOnlyScheme())
					SetDefaultValue(value);
			}
		}

		public string DeclaredClass { 
			get { return InnerDeclaredClass; }
			set {
				if (!CheckReadOnlyScheme())
					InnerDeclaredClass = value;
			}
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override int GetHashCode() {
			// XXX не нужно ли тут учитывать имя класса?
			return InnerName.GetHashCode();
		}
		
		public override bool Equals(object obj) {
			if (obj == null)
				return false;
			SlotDefinition sd = obj as SlotDefinition;
			if (sd == null)
				return false;

			return InnerName == sd.InnerName;
		}


		public virtual object Evaluate() {
			object result = null;
			if (InnerDefaultValueAccessor != null)
				result = InnerDefaultValueAccessor.Invoke();
			else
				result = InnerDefaultValue;

			return result;
		}

		public virtual object GetDefaultValueAccessor() {
			return InnerDefaultValueAccessor;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual object GetDefaultValue() {
			return Evaluate();
		}

		protected virtual void SetDefaultValue(object value) {
			IFunction f = value as IFunction;
			if (f != null) 
				InnerDefaultValueAccessor = f;
			else {
				InnerDefaultValue = value;
				InnerDefaultValueAccessor = null;
			}
		}
		//.........................................................................
		#endregion


		#region Supplementary Methods
		//..................................................................

		public static explicit operator string(SlotDefinition p) {
			return (p != null) ? p.Name : null;
		}

		//..................................................................
		#endregion
	}
}
