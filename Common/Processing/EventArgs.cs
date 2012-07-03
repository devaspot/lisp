using System;
using System.Collections.Generic;
using System.Text;

namespace Front.Processing {

	public class CommandArgs : EventArgs {

		public CommandArgs() {
		}

		// TODO: может передавать сюда весь  Event?
		public CommandArgs(Event ev) {
			this.EventCode = ev.Name;
			this.SessionID = ev.SessionID;
			this.ID = ev.ID;
			this.Args = ev.Args;
		}

		public string EventCode;
		public string SessionID;

		public object[] Args;

		public long ID;
		public int status;
	}


	public class QueueEventArgs : EventArgs {
		protected long InnerKey;
		protected object InnerItem;

		public QueueEventArgs(long key, object item) : base() {
			InnerKey = key;
			InnerItem = item;
		}

		public long Key {
			get { return InnerKey; }
		}

		public object Item {
			get { return InnerItem; }
		}
	}


	public class QueueErrorEventArgs : QueueEventArgs {

		protected int InnerDelay;
		protected Exception InnerException;
		protected int InnerRetryCount;

		public QueueErrorEventArgs(long key, object item, int delay, Exception ex, int retryCount)
			: base(key, item) {
			InnerDelay = delay;
			InnerException = ex;
			InnerRetryCount = retryCount;
		}

		public int Delay {
			get { return InnerDelay; }
		}

		public Exception Exception {
			get { return InnerException; }
		}

		public int RetryCount {
			get { return InnerRetryCount; }
		}
	}

}
