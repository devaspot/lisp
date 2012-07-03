using System;
using System.Collections;
using System.Text;

namespace Front.Lisp {

	public class Cons : IEnumerable {

		#region Protected Fields
		//.........................................................................
		protected object InnerFirst;
		protected Cons InnerRest;
		//.........................................................................
		#endregion

		#region Constructors
		//.........................................................................
		public Cons(Object first) : this(first, null) {	}
		public Cons(object first, Cons rest) {
			InnerFirst = first;
			InnerRest = rest;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public object First {
			get { return InnerFirst; }
			set { InnerFirst = value; }
		}

		public Cons Rest {
			get { return InnerRest; }
			set { InnerRest = value; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public IEnumerator GetEnumerator() {
			return new ConsEnumerator(this);
		}

		public static Object GetFirst(Cons list) {
			return list.First;
		}

		public static Object GetRest(Cons list) {
			return list.Rest;
		}

		public static Object GetSecond(Cons list) {
			return list.Rest.First;
		}

		public static Object GetThird(Cons list) {
			return GetSecond(list.Rest);
		}

		public static Object GetFourth(Cons list) {
			return GetThird(list.Rest);
		}

		public static Cons Reverse(Cons list) {
			Cons result = null;
			while (list != null) {
				result = new Cons(list.First, result);
				list = list.Rest;
			}
			return result;
		}

		public override int GetHashCode() {
			int head = (First == null) ? 17 : First.GetHashCode();
			int tail = (Rest == null) ? 17 : Rest.GetHashCode();
			return head + tail * 37;
		}

		public override Boolean Equals(Object that) {
			if (this == that) return true;
			else return ((that is Cons)
							&& EqualsFirst((Cons)that)
							&& EqualsRest((Cons)that));
		}

		public static int GetLength(Cons list) {
			int len = 0;
			while (list != null) {
				len++;
				list = list.Rest;
			}
			return len;
		}

		public static Cons GetLast(int num, Cons list) {
			int len = GetLength(list);
			if (num > len)
				return null;
			return GetNthTail(len - num, list);
		}

		public static Object GetNth(int n, Cons list) {
			return GetNthTail(n, list).First;
		}

		public static Cons GetNthTail(int n, Cons list) {
			while (n > 0) {
				n--;
				list = list.Rest;
			}
			return list;
		}

		public static Object[] ToVector(Cons list) {
			int len = GetLength(list);
			if (len == 0)
				return Util.EMPTY_VECTOR;
			else {
				Object[] result = new Object[len];
				for (int i = 0; list != null; i++, list = list.Rest) {
					result[i] = list.First;
				}
				return result;
			}
		}

		public static object ToVectorOf(Type t, Cons list) {
			int len = GetLength(list);
			Array result = Array.CreateInstance(t, len);
			for (int i = 0; list != null; i++, list = list.Rest)
				result.SetValue(list.First, i);

			return result;
		}

		public static Object MakeList(params Object[] args) {
			Cons ret = null;
			for (int i = args.Length - 1; i >= 0; --i) {
				ret = new Cons(args[i], ret);
			}
			return ret;
		}

		public static Cons Append(Cons x, Cons y) {
			return (x != null) ? new Cons(x.First, Append(x.Rest, y)) : y;
		}

		public override String ToString() {
			StringBuilder buf = new StringBuilder();
			buf.Append('(');
			if (First != null)
				buf.Append(First.ToString());
			Cons tail = Rest;
			while (tail != null) {
				buf.Append(' ');
				if (tail.First != null)
					buf.Append(tail.First.ToString());
				tail = tail.Rest;
			}
			buf.Append(')');
			return buf.ToString();
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected bool EqualsFirst(Cons that) {
			return (First == null) ? that.First == null :
			First.Equals(that.First);
		}

		protected bool EqualsRest(Cons that) {
			return (Rest == null) ? that.Rest == null :
			Rest.Equals(that.Rest);
		}
		//.........................................................................
		#endregion
	}

	public class ConsEnumerator : IEnumerator {

		public ConsEnumerator(Cons start) {
			this.start = start;
			this.current = null;
			this.next = start;
		}

		//IEnumerator implementation
		public Object Current {
			get {
				return current.First;
			}
		}

		public Boolean MoveNext() {
			current = next;
			if (current != null)
				next = current.Rest;
			return current != null;
		}

		public void Reset() {
			this.current = null;
			this.next = start;
		}

		Cons start;
		Cons current;
		Cons next;
	}

}