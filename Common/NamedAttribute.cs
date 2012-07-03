using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Front {

	[Serializable]
	public class NamedAttribute : Attribute, INamedValue, IComparable, ISerializable {

		protected Name InnerName;
		protected object InnerValue;

		#region Constructors
		//.....................................................................
		public NamedAttribute(Name name) : this(name, null) { }

		public NamedAttribute(Name name, object value) {
			InnerName = name;
			InnerValue = value;
		}

		protected NamedAttribute(SerializationInfo info, StreamingContext context) {
			InnerName = new Name(info.GetString("name"));
			InnerValue = info.GetValue("value", typeof(object));
		}

		public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
			info.AddValue("name", Name);
			info.AddValue("value", Value);
		}
		//.....................................................................
		#endregion


		#region INamedValue implementation
		//.....................................................................
		public virtual Name FullName {
			get { return InnerName; }
		}

		public virtual string Name {
			get { return FullName.OwnAlias; }
			set { FullName.OwnAlias = value; }
		}

		public virtual object Value {
			get { return InnerValue; }
			set { InnerValue = value; }
		}
		//.....................................................................
		#endregion


		#region IComparable implementation

		int IComparable.CompareTo(object obj) {
			if (obj == null)
				return -1;
			if (obj is string)
				return Name.CompareTo(obj);
			else if (obj is NamedAttribute)
				return CompareTo((NamedAttribute)obj);
			else
				throw new NotSupportedException();
		}

		public int CompareTo(NamedAttribute attr) {
			if (attr == null) return -1;
			return Name.CompareTo(attr.Name);
		}

		#endregion

	}

}
