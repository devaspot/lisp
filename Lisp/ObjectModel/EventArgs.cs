// $Id$
// (c) Pilikn Programmers Group


using System;
using System.Collections;
using System.Data;
using System.Text;
using System.ComponentModel;

namespace Front.ObjectModel {

	public class SlotChangeEventArgs : CancelEventArgs {
		
		public object Value;
		public object OriginalValue;
		public string SlotName;
		public SlotDefinition Slot;

		public SlotChangeEventArgs(SlotDefinition slot, object value) {
			Value = value;
			Slot = slot;
			if (slot != null) 
				SlotName = slot.Name;
		}

		public SlotChangeEventArgs(string slotName, object value) : this((SlotDefinition)null, value) {
			SlotName = slotName;
		}
	}


	public class SlotListChangedEventArgs : EventArgs {

		protected ListChangeType InnerChangeType;
		protected SlotDefinition[] InnerSlots; // TODO: может это и излишне...

		public SlotListChangedEventArgs(ListChangeType ct, params SlotDefinition[] slt) {
			InnerChangeType = ct;
			InnerSlots = slt;
		}

		public ListChangeType ChangeType {
			get { return InnerChangeType; }
		}

		public SlotDefinition[] Slots {
			get { return InnerSlots; }
		}

		public SlotDefinition Slot {
			get { return (InnerSlots != null && InnerSlots.Length > 0) ? InnerSlots[0] : null; }
		}
	}



	public class MethodListChangedEventArgs : EventArgs {
		protected ListChangeType InnerChangeType;
		protected MethodDefinition[] InnerMethods; // TODO: может это и излишне...

		public MethodListChangedEventArgs(ListChangeType ct, params MethodDefinition[] mth) {
			InnerChangeType = ct;
			InnerMethods = mth;
		}

		public ListChangeType ChangeType {
			get { return InnerChangeType; }
		}

		public MethodDefinition[] Methods {
			get { return InnerMethods; }
		}

		public MethodDefinition Method {
			get { return (InnerMethods != null && InnerMethods.Length > 0) ? InnerMethods[0] : null; }
		}
	}


	public class ExtensionListChangedEventArgs : EventArgs {
		protected ListChangeType InnerChangeType;
		protected ClassDefinition[] InnerExtensions; // TODO: может это и излишне...

		public ExtensionListChangedEventArgs(ListChangeType ct, params ClassDefinition[] mth) {
			InnerChangeType = ct;
			InnerExtensions = mth;
		}

		public ListChangeType ChangeType {
			get { return InnerChangeType; }
		}

		public ClassDefinition[] Extensions {
			get { return InnerExtensions; }
		}

		public ClassDefinition Extension {
			get { return (InnerExtensions != null && InnerExtensions.Length > 0) ? InnerExtensions[0] : null; }
		}
	}


	public class SlotErrorEventArgs : SlotChangeEventArgs {

		public Exception Exception;

		public SlotErrorEventArgs(Exception ex, string slotName, object value) : base(slotName, value) {
			Exception = ex;
		}

		public SlotErrorEventArgs(Exception ex, SlotDefinition slot, object value) : base(slot, value) {
			Exception = ex;
		}
	}


	public class CollectionEventArgs : EventArgs {
		public object Collection;

		// TODO: дописать
	}
	
}
