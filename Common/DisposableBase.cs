using System;
using System.ComponentModel;

namespace Front {

	// XXX: этот класс можно расссматривать только как наглядное пособие
	// наследоваться от него никто не будет!

	public abstract class DisposableBase : IDisposable {
		#region DisposableBase

		#region Fields

		protected bool InnerIsDisposed = false;

		#endregion

		#region Methods

		protected virtual void CheckDisposed() {
			if (this.InnerIsDisposed) {
				throw new ObjectDisposedException(GetType().FullName);
			}
		}

		protected virtual void Dispose(bool disposing) {
			try {
				if (disposing) {
					if (Disposed != null) {
						Disposed(this, EventArgs.Empty);
					}
				}
			} finally {
				this.InnerIsDisposed = true;
			}
		}

		public DisposableBase() {
		}

		~DisposableBase() {
			Dispose(false);
		}

		#endregion

		#region Properties

		[Browsable(false)]
		public bool IsDisposed {
			get { return this.InnerIsDisposed; }
		}

		#endregion

		#region Events

		public event EventHandler Disposed;

		#endregion

		#endregion

		#region IDisposable

		public void Dispose() {
			if (!this.InnerIsDisposed) {
				Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		#endregion
	}
}