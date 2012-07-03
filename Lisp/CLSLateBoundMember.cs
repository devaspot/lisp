using System;
using System.Reflection;
using System.Collections.Specialized;
using Front.ObjectModel;

namespace Front.Lisp {

	public class CLSLateBoundMember : CLSMember {

		#region Protected Fields
		//.........................................................................
		protected HybridDictionary InnerCache = new HybridDictionary();
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public CLSLateBoundMember(string name) {
			InnerName = name;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public HybridDictionary Cache {
			get { return InnerCache; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override object Invoke(params object[] args) {
			// instance member gets target from first arg
			object target = args[0];

			if (target is Record) {
				Record rec = (Record)target;

				if (args.Length == 2) {	//set call
					rec[InnerName] = args[1];
					return args[1];
				} else	{ //get
					object ret = rec[InnerName];
					if (ret == null && !rec.Contains(InnerName))
						throw new LispException("Record does not contain member: " + InnerName);
					return ret;
				}
			} else {
				object result;
				if (InvokeFLOSMember(InnerName, target, args, out result))
					return result;

				//get a real member to do the work
				//first check the cache
				Type targetType = target.GetType();
				CLSMember member = (CLSMember)InnerCache[targetType];
				if (member == null) {
					//late-bound members are never static
					InnerCache[targetType] = member = CLSMember.FindMember(InnerName, targetType, false);
				}
				return member.Invoke(args);
			}
		}

		protected virtual bool InvokeFLOSMember(string name, object target, object[] args, out object result) {
			result = null;

			IObject ci = target as IObject;
			if (ci != null) {
				SlotDefinition slot = ci.GetSlot(InnerName);
				if (slot != null) {
					if (args.Length == 2)
						result = ci.SetSlotValue(InnerName, args[1]);
					else
						result = ci.GetSlotValue(InnerName);

					return true;
				}
			}
		
			IBehaviored b = target as IBehaviored;
			if (b != null && b.CanInvoke(InnerName)) {
				result = b.Invoke(InnerName, args);
				return true;
			}

			return false;
		}
		//.........................................................................
		#endregion

	}
}
