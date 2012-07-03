using System;
using System.IO;
using System.Security.Principal;
using System.Diagnostics;
using System.Net;
using System.Runtime.Remoting.Messaging;


namespace Front.Diagnostics {
	
	public class PrefixListener : TextWriterTraceListener {
		private IPrefixBuilder		_prefixBuilder;

		protected void  Init() {
			_prefixBuilder = new DefaultPrefixBuilder();
		}

		public PrefixListener():base() { Init(); }
		public PrefixListener(Stream stream):base(stream) { Init(); }
		public PrefixListener(string fileName):base(fileName) { Init(); }
		public PrefixListener(TextWriter writer):base(writer) { Init(); }
		public PrefixListener(Stream stream, string name):base(stream, name) { Init(); }
		public PrefixListener(string fileName, string name):base(fileName, name) { Init(); }
		public PrefixListener(TextWriter writer, string name):base(writer, name) { Init(); }

		public PrefixListener(Stream stream, IPrefixBuilder prefix): this(stream) { _prefixBuilder = prefix; }
		public PrefixListener(string fileName, IPrefixBuilder prefix) : this(fileName) { _prefixBuilder = prefix; }
		public PrefixListener(TextWriter writer, IPrefixBuilder prefix) : this(writer) { _prefixBuilder = prefix; }
		public PrefixListener(Stream stream, string name, IPrefixBuilder prefix) : this(stream, name) { _prefixBuilder = prefix; }
		public PrefixListener(string fileName, string name, IPrefixBuilder prefix) : this(fileName, name) { _prefixBuilder = prefix; }
		public PrefixListener(TextWriter writer, string name, IPrefixBuilder prefix) : this(writer, name) { _prefixBuilder = prefix; }

		public IPrefixBuilder PrefixBuilder { get { return _prefixBuilder; } set { _prefixBuilder = value; } }
		
		protected override void  WriteIndent() {
			IPrefixBuilder pb = this.PrefixBuilder;
			if (pb != null) lock (this) {
				Writer.Write(pb.Prefix);
				base.WriteIndent();
			} else
				base.WriteIndent();
		}

		public override void  WriteLine(string message) {
			try {
				base.WriteLine(message);
			} catch (ObjectDisposedException) {
			}
		}		

		public override void  Flush() {
			try {
				base.Flush();
			} catch (ObjectDisposedException) {
				// ignore
			}
		}
	}
}
