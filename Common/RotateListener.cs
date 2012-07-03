using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Runtime.Remoting.Messaging;
using System.Net;
using System.Threading;


namespace Front.Diagnostics {


	/// <summary>
	/// TextWriter Trace Listener with event timing
	/// </summary>
	//...........................................................................................
	public class RotatedListener : TraceListener {
		private IPrefixBuilder     _prefixBuilder;
		private TextWriter         _writer;
		private DateTime    			_logTime;
		private TimeSpan				_logSpan = TimeSpan.MaxValue;
		private string					_logLocation = "Log";
		private string					_logPrefix = "log-";

		public RotatedListener() : this(null) { }
		
		public RotatedListener( IPrefixBuilder builder ) {
			//_prefixBuilder = (builder == null) ? new DefaultPrefixBuilder() : builder;
			_prefixBuilder = builder;

			string o = ConfigurationSettings.AppSettings["RotatedListener.LogLocation"];
			if (o != null && o.Length != 0) 
				_logLocation = o;
			if (!Path.IsPathRooted(_logLocation))
				_logLocation = Path.Combine(
						Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile),
						_logLocation);

			o = ConfigurationSettings.AppSettings["RotatedListener.LogPrefix"];
			if (o != null && o.Length != 0) _logPrefix = o;

			o = ConfigurationSettings.AppSettings["RotatedListener.LogSpan"];
			if (o != null && o.Length != 0) _logSpan = TimeSpan.FromMinutes(Convert.ToInt32(o));

			_logTime = DateTime.Now;
		}

		public IPrefixBuilder PrefixBuilder  { 
			get { return _prefixBuilder; } 
			set { _prefixBuilder = value; } 
		}

		protected override void WriteIndent() {
		   IPrefixBuilder pb = this.PrefixBuilder;
		   if (pb != null) lock (this) {
		      Writer.Write(pb.Prefix);
		      base.WriteIndent();
		   } else
		      base.WriteIndent();
			base.WriteIndent();
		}

		protected virtual TextWriter Writer {
			get {
				DateTime now = DateTime.Now;
				if ( (_logSpan < TimeSpan.MaxValue && _logTime + _logSpan < now) || _writer == null) lock (this) 
				{
					if (_writer != null) 
						this.Close();
					if (!Directory.Exists(_logLocation))
						Directory.CreateDirectory(_logLocation);
					string name = Path.Combine(_logLocation, _logPrefix + now.ToString("yyyy-MM-dd HH.mm.ms") + ".log");
					_writer = new StreamWriter(name, true);
					_logTime = now;
				}
				return _writer;
			}
		}

		public override void Write(string message) {
			lock (this) {
				if (base.NeedIndent)
					this.WriteIndent();
				Writer.Write(message);
			}
		}

		public override void WriteLine(string message) {
			lock (this) {
				if (base.NeedIndent)
					this.WriteIndent();
				Writer.WriteLine(message);
				this.NeedIndent = true;
			}
		}		

		public override void Flush() {
			lock (this) {
				if (_writer != null) _writer.Flush();
			}
		}

		public override void Close() {
			lock (this) {
				if (_writer != null) {
					_writer.Close();
					_writer = null;
				}
			}
		}
	}

	public interface IPrefixBuilder {
		string Prefix {
			get;
		}
	}

	//...........................................................................................
	public class DefaultPrefixBuilder : MarshalByRefObject, IPrefixBuilder {
		public string time_format = "yyyy-MM-dd HH:mm:ss";

		public DefaultPrefixBuilder() {
			string o = System.Configuration.ConfigurationSettings.AppSettings["PrefixBuilder.TimeFormat"];
			if (o != null && o != "")
				time_format = o;
		}

		public virtual string Prefix {
			get {
				return DateTime.Now.ToString(time_format) + '\t';
			}
		}
	}


	//...........................................................................................
	public class VerbosePrefixBuilder : MarshalByRefObject, IPrefixBuilder {
		public string time_format = "yyyy-MM-dd HH:mm:ss";

		public VerbosePrefixBuilder() {
			string o = System.Configuration.ConfigurationSettings.AppSettings["PrefixBuilder.TimeFormat"];
			if (o != null && o != "")
				time_format = o;
		}

		public virtual string Prefix {
			get {
				// TODO: Добавить управляемую возможность вывода имени класса и метода
				// Front.Diagnostics.Log и System.Diagnostics.Trace не учитывать
				// XXX кстати, что настраивается в Log4net ?

				object o = CallContext.GetData("remote_ip");		// XXX: Согласовать ключ хранения удаленного ip-адреса.
				string userName = Thread.CurrentPrincipal.Identity.Name;
				//if (userName == "")
				//	userName = "[" + WindowsIdentity.GetCurrent().Name + "]";

				return DateTime.Now.ToString(time_format) + ' '
						+ '#' + Thread.CurrentThread.ManagedThreadId.ToString() + ' '
						+ userName
						+ ((o != null) ? "  " + o.ToString() : "")
						+ "   ";
			}
		}
	}	

}
