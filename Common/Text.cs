using System;
using System.Collections.Generic;
using System.Text;

using Front.Converters;

namespace Front.Text {

	// TODO: сделать что бы работал со StreamWriter'ом!
	// TODO: сделать его TextWriter
	public class IndentedWriter {
		protected StringBuilder InnerStringBuilder;
		protected bool InnerNewLine = true;
		protected int InnerIndentSize = 0;

		public Converter Converter = null;

		public IndentedWriter(int befferLength) : this(new StringBuilder(befferLength)) {
		}

		public IndentedWriter() : this(new StringBuilder()) {
		}

		public IndentedWriter(StringBuilder sb) {
			InnerStringBuilder = sb;
		}

		
		public int Length {
			get { return  InnerStringBuilder.Length; }
		}

		public int IndentSize { 
			get { return InnerIndentSize; }
			set { InnerIndentSize = value; }
		}
		

		public virtual IndentedWriter Write(string s) {
			if (InnerNewLine)
				WriteBlanks();
			InnerStringBuilder.Append(s);
			InnerNewLine = false;
			return this;
		}

		public virtual IndentedWriter Write(string format, object o) {
			// XXX формат со многоми объектами работает не так, как формат с одним объектом!
			return (format == null)
						? (Converter == null)
							? GenericConverter.String(this, o)
							: Converter.String(this, o)
						: (Converter == null)
							? GenericConverter.String(this, format, o)
							: Converter.String(this, format, o);
		}

		public virtual IndentedWriter Write(object o) {
			return (Converter == null)
					? GenericConverter.String(this, o)
					: Converter.String(this,o);
		}

		public virtual IndentedWriter Write(string format, params object[] args) {			
			if (InnerNewLine)
				WriteBlanks();
			InnerStringBuilder.AppendFormat(format, args);
			InnerNewLine = false;
			return this;
		}

		public virtual IndentedWriter WriteLine() {
			InnerStringBuilder.Append("\r\n");
			InnerNewLine = true;
			return this;
		}

		public virtual IndentedWriter WriteLine(object o) {
			return Write(o).WriteLine();

		}

		public virtual IndentedWriter WriteLine(string s) {
			return Write(s).WriteLine();
		}

		public virtual IndentedWriter WriteLine(string format, params object[] args) {
			Write(format, args);
			WriteLine();
			return this;
		}

		public virtual void Indent() {
			IndentSize ++;
		}

		public virtual void UnIndent() {
			if (IndentSize >0)
				IndentSize--;
		}

		public override string ToString() {
			return InnerStringBuilder.ToString();
		}

		protected virtual void WriteBlanks() {
			for (int i = 0; i < IndentSize; i++)
				InnerStringBuilder.Append('\t');
		}

	}

}
