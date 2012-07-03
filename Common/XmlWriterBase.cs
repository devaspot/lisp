using System;
using System.Collections;
using System.Data;
using System.Text;
using System.Xml;

namespace Front {
	
	public class XmlWriterBase {

		protected XmlWriter writer;

		public XmlWriter Writer { get { return writer; } }
		
		public XmlWriterBase( XmlWriter writer ) {
			this.writer = writer;
		}

		public XmlWriterBase(StringBuilder sb) : this (sb, null) {}

		public XmlWriterBase(StringBuilder sb, XmlWriterSettings settings) : this( XmlWriter.Create(sb, settings)) {
		}

		protected virtual void Begin(string element_name, params object[] attribs) {
			writer.WriteStartElement(element_name);
			Attribute(attribs);
		}

		protected virtual void Attribute(params object[] attribs) {
			if (attribs == null || attribs.Length == 0) return;
			
			for( int i = 0; i < attribs.Length; i+=2 ) 
			{
				string name = attribs[i].ToString();
				string val = (i+1 < attribs.Length) ? attribs[i+1].ToString() : "";
				writer.WriteAttributeString(name, val);
			}
		}

		protected virtual void End() {
			writer.WriteEndElement();
		}

		protected virtual void EndAll() {
			writer.WriteEndDocument();
		}
	}


	public class XmlConstructor : XmlWriterBase {
		public static XmlWriterSettings DefaultSettings;

		static XmlConstructor() {
			DefaultSettings = new XmlWriterSettings();
			DefaultSettings.OmitXmlDeclaration = true;
		}

		public XmlConstructor( XmlWriter writer ) : base(writer) {}

		public XmlConstructor(StringBuilder sb) : base (sb, DefaultSettings) {}

		public XmlConstructor(StringBuilder sb, XmlWriterSettings settings) : base( XmlWriter.Create(sb, settings)) { }

		new public virtual void Begin(string element_name, params object[] attribs) {
			base.Begin(element_name, attribs);
		}

		new public virtual void End() {
			base.End();
		}

		new public virtual void EndAll() {
			base.EndAll();
		}

		new public virtual void Attribute(params object[] attribs) {
			base.Attribute(attribs);
		}

		public virtual void Write(string value) {
			writer.WriteRaw(value);
		}

		public void Close() {
			writer.Close();
		}
	}
}
