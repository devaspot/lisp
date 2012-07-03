using System;
using System.Collections.Generic;
using System.Text;

namespace Front.ObjectModel {

	// TODO: продумать...
	public class FrontObjectAttribute : Attribute {
		public FrontObjectAttribute() {}
	}


	public class DictionaryGroupAttribute : NamedAttribute {

		public DictionaryGroupAttribute(string grpname) : base("Dictionary.Group", grpname) {
		}

		public string GroupName { 
			get { return (string)Value; } 
		}
	}


	public class BehaviorMethodAttribute : NamedAttribute {

		public BehaviorMethodAttribute(string methodname) : base("Behavior.Method", methodname) {
		}

		public string MethodName {
			get { return  (string)Value; }
		}
	}

	public class BacklinkAttribute : NamedAttribute {

		public BacklinkAttribute(string field)
			: base("Backlink", field) {
		}

		public string FieldName {
			get { return (string)Value; }
		}
	}

}
