// $Id: Convert.cs 2392 2006-09-15 16:10:02Z john $

using System;
using System.Data;
using System.ComponentModel.Design;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;
using System.Collections;

using Front.Globalization;
using Front.Data;
using Front.Text;
using Front;
using System.Text.RegularExpressions;

namespace Front.Converters {

	#region GenericConverter
	public class GenericConverter {
		
		public static Converter Converter;

		static GenericConverter() {
			Converter = new Converter();
			Converter.Converters.Add(MsSqlConverter.Converter);
			Converter.Converters.Add(OracleConverter.Converter);
		}
	

		public static string String(object o) {
			IndentedWriter w =new IndentedWriter();
			String(w, o);
			return w.ToString();
		}

		public static IndentedWriter String(IndentedWriter w, object o) {
			return Converter.String(w, o);
		}

		// TODO: работу с форматом или форматером нужно оформить таким образом, что бы
		// было несколько способов форматирования.
		// для этого прийдется вместо делегата конвертера хранить класс конвертера,
		// а для примитивных конвертеров использовать специальный класс, которой
		// инкапсулирует делегат
		public static IndentedWriter String(IndentedWriter w, string format, object o) {
			return Converter.String(w, format, o);
		}


		public static string FormatParameters(string format, params object[] lst) {
			StringBuilder sb = new StringBuilder();
			if (lst == null) lst = new object[0];

			string[] values = new string[lst.Length];
			for (int i = 0; i < lst.Length; i++) {
				values[i] = GenericConverter.String(lst[i]);
			}

			// Эскейпим @ через @@ыX
			//			string reg = "([^@]+|^)(@([0-9]+))(\\D{1}|\\W{1}|$)";
			string reg = "(^|[^@]{1})@([0-9]+)";
			Regex r = new Regex(reg);
			int start = 0;
			Match m = r.Match(format, start);
			// [processed string] | [string before @123] [@234] [string after @123]
			// заменяем @@ на @ в "string before @123"
			while (m.Success) {
				string mv = m.Groups[1].Value;
				string m1 = m.Groups[2].Value;
				int i = Convert.ToInt32(m1);
				// собираем новую строку
				sb.Append(format.Substring(start, m.Index - start + mv.Length).Replace("@@", "@"));
				sb.Append((i < values.Length) ? values[i] : "null");
				start = m.Index + m.Length; // отрезаем от старой строки начало
				m = r.Match(format, start);
			}
			sb.Append(format.Substring(start).Replace("@@", "@"));
			return sb.ToString();
		}

		public static string Format(string format, IDictionary values) {
			StringBuilder sb = new StringBuilder();
			
			IDictionary intValues = new Hashtable();

			// Эскейпим @ через @@ыX
			//			string reg = "([^@]+|^)(@([0-9]+))(\\D{1}|\\W{1}|$)";
			string reg = "(^|[^\\$]{1})\\$((\\w+)|\\{([^}]+)\\})";
			Regex r = new Regex(reg);
			int start = 0;
			Match m = r.Match(format, start);
			// [processed string] | [string before @123] [@234] [string after @123]
			// заменяем @@ на @ в "string before @123"
			while (m.Success) {
				string mv = m.Groups[1].Value; // строка до 1-го $
				string m1 = m.Groups[3].Value + m.Groups[4].Value; // значение ключа
				// собираем новую строку
				sb.Append(format.Substring(start, m.Index - start + mv.Length).Replace("$$", "$"));
				string value = null;
				if (intValues.Contains(m1))
					value = (string)intValues[m1];
				else {
					object o = values[m1];
					intValues[m1] = value = (o is string) ? (string)o : GenericConverter.String(o);
				}
				sb.Append((intValues.Contains(m1)) ? value : "");
				start = m.Index + m.Length; // отрезаем от старой строки начало
				m = r.Match(format, start);
			}
			sb.Append(format.Substring(start).Replace("$$", "$"));
			return sb.ToString();
		}
	}

	// база для наследования
	// в нем же и реализация основных конвертаций
	public class Converter : TypeDispatcher<Converter.ConvertDelegate> {
		public delegate IndentedWriter ConvertDelegate(IndentedWriter w, string format, object o);

		public ICollection<Converter> Converters = new Collection<Converter>();
		public ConvertDelegate DefaultConverter = null;

		protected IFormatProvider InnerFormatProvider = System.Threading.Thread.CurrentThread.CurrentCulture;
		public virtual IFormatProvider FormatProvider {
			get { return InnerFormatProvider; }
			set { InnerFormatProvider = value; }
		}

		public Converter() {
			NullValue = NullConverter;
			Initialize();
		}

		protected virtual void Initialize() {
			this[ typeof(object) ]         = ObjectToString;
			this[ typeof(string) ]         = EscapeString;
			this[ typeof(DateTime) ]       = DateToString;
			this[ typeof(Boolean) ]        = BoolToString;
			this[ typeof(Name) ]           = NameToString;
			this[ typeof(Decimal) ]        = DecimalToString;
			this[ typeof(Type)]			   = TypeToString;
			this[ typeof(DBNull) ]         = NullConverter;
			this[ typeof(QueryParameter) ] = ParameterToString;
			// Extra-конвертеры
			this[ typeof(ICollection) ]    = CollectionToString;
		}

		public virtual IndentedWriter String(IndentedWriter w, object o) {
			return String(w, null, o);
		}

		public virtual IndentedWriter String(IndentedWriter w, string format, object o) {
			ConvertDelegate cd = null;
			if (Converters != null)
				foreach (Converter c in Converters)
					if ((cd = c.IsSuitable(o)) != null) break;

			return ( cd == null ) ? Write(w, format, o) : cd(w, format, o);
		}


		// не использует внешние конвертеры
		public virtual IndentedWriter Write(IndentedWriter w, object o) {
			return Write(w, null, o);
		}

		public virtual IndentedWriter Write(IndentedWriter w, string format, object o) {
			ConvertDelegate cd = (o == null) ? NullValue : this[o.GetType()];
			if (cd == null)
				cd = DefaultConverter;

			if (cd == null)
				Error.Critical(new NotImplementedException("Converter not found."),typeof(Converter));

			// XXX может получиться не ххороший эффект - игнорирование.
			return (cd != null) ? cd(w, format, o) : w;
		}

		public virtual ConvertDelegate IsSuitable(object o) {
			if (o == null) return NullValue;
			if (Converters != null)
				foreach (Converter cv in Converters) {
					ConvertDelegate cd = cv.IsSuitable(o);
					if (cd != null) return cd;
				}
			return this[o.GetType()];
		}

		//..................................................................

		public virtual IndentedWriter NullConverter(IndentedWriter w, string format, object o) {
			return w.Write("NULL");
		}

		public virtual IndentedWriter ObjectToString(IndentedWriter w, string format, object o) {
			if (format != null && format != "")
				return w.Write( System.String.Format( this.FormatProvider, "{0:"+format+"}", o));

			return w.Write( o.ToString()) ;
		}

		public virtual string EscapeString(string o) {
			return o.Replace("'", "''");
		}

		public virtual IndentedWriter EscapeString(IndentedWriter w, object o) {
			return EscapeString(w, null, o);
		}

		public virtual IndentedWriter EscapeString(IndentedWriter w, string format, object o) {
			return w.Write("'") .Write(EscapeString((string)o)) .Write("'");
		}

		public virtual IndentedWriter EscapeName(IndentedWriter w, string name) {
			return EscapeName(w, null, name);
		}

		public virtual IndentedWriter EscapeName(IndentedWriter w, string format, string name) {
			return w.Write(name);
		}

		public virtual IndentedWriter DateToString(IndentedWriter w, string format, object o) {
			DateTime dt = (DateTime)o;
			return w.Write( (format != null)
								? dt.ToString(format, FormatProvider) 
								: dt.ToString(FormatProvider));
		}

		public virtual IndentedWriter TypeToString(IndentedWriter w, string format, object o) {
			Type dt = (Type)o;
			return w.Write(dt.Name);
		}

		public virtual IndentedWriter BoolToString(IndentedWriter w, string format, object o) {
			bool b =  (Boolean)o;
			// XXX: использовать format или FormatProvider
			return w.Write( b ? "true" : "false");
		}

		public virtual IndentedWriter NameToString(IndentedWriter w, object o) {
			return NameToString(w, null, o);
		}

		public virtual IndentedWriter NameToString(IndentedWriter w, string format, object o) {
			Name o1 = (Name)o;
			AliasList x = o1.AliasList;
			string last_name_alias = null;
			string al = null;

			if (x != null) {
				al = x.GetAlias(o1.OwnAlias);
				if (al == null)
					last_name_alias = x.GetAlias(o1.LastName);
			}

			if (last_name_alias == null)
				last_name_alias = o1.LastName;

			if (al == null) {
				if (o1.BaseName != null) {
					NameToString(w, o1.BaseName);
					w.Write(".");
				}
				EscapeName(w, last_name_alias);
			} else 
				EscapeName(w, al);

			return w;
		}

		public virtual IndentedWriter DecimalToString(IndentedWriter w, string format, object o) {
			Decimal d = (Decimal)o;
			return w.Write( (format != null) 
								? d.ToString(format, FormatProvider) 
								: d.ToString(FormatProvider));
		}

		public virtual IndentedWriter CollectionToString(IndentedWriter w, string format, object o) {
			ICollection o1 = (ICollection)o;
			// TODO: использовать формат!
			w.Write("(");
			bool coma = false;
			foreach (object o2 in o1) {
				if (coma) w.Write(", ");
				w.Write((object)o2);
				coma = true;
			}
			return w.Write(")");
		}

		

		// В базе принимает SQL-способ параметризации
		public virtual IndentedWriter ParameterToString(IndentedWriter w, string format, object o) {
			QueryParameter qp = (QueryParameter)o;
			if (qp == null)
				return w;
			//if (qp.Value != null) {
			//    w.Write(qp.Value);
			//    return w;
			//}
			// TODO: может потребоваться escaping
			// TODO: использовать формат!
			string pname = qp.Parameter;
			if (pname.StartsWith(":")) {
				pname = pname.Substring(1);
				w.Write("@").Write(pname);
			} else if (pname.StartsWith("@"))
				w.Write(pname);
			else
				w.Write("@").Write(pname);

			return w;
		}

	}
	#endregion

	
	#region MsSqlConverter
	public class MsSqlConverter : Converter {
		public static Converter Converter = new MsSqlConverter();
		public static readonly DateTime minDate = new DateTime(1753, 1, 1);
		public const Decimal minNumber = -922337203685477;
		public const Decimal maxNumber = 922337203685477;
		public static string DateFormat = "yyyy-MM-dd HH:mm:ss";

		public static Dictionary<Type, string> TypeMappingTable = new Dictionary<Type,string>();
		static MsSqlConverter() {
			TypeMappingTable.Add(typeof(int), "int");
			TypeMappingTable.Add(typeof(Byte), "tinyint");
			TypeMappingTable.Add(typeof(long), "bigint");
			TypeMappingTable.Add(typeof(Decimal), "money");
			TypeMappingTable.Add(typeof(Double), "float");
			TypeMappingTable.Add(typeof(float), "float");
			TypeMappingTable.Add(typeof(DateTime), "datetime");
			TypeMappingTable.Add(typeof(string), "nvarchar(256)");
			TypeMappingTable.Add(typeof(Guid), "uniqueidentifier");
			TypeMappingTable.Add(typeof(Boolean), "bit");
			TypeMappingTable.Add(typeof(Object), "sql_variant");
		
			Converter.DefaultConverter = (ConvertDelegate)delegate(IndentedWriter w, string format, object o) {
				// MsSqlConverter деградирует до GenericConverter.
				// используем Write, что бы не зациклиться.
				return GenericConverter.String(w, format, o);
			};
		}

		public MsSqlConverter() : base() {}

		protected override void Initialize() {
			this[ typeof(Decimal) ]		= DecimalToString;
			this[ typeof(DateTime) ]	= DateToString;
			this[ typeof(Name) ]		= NameToString;
			this[ typeof(Type)]			= TypeToString;			
		}

		public override Converter.ConvertDelegate IsSuitable(object o) {
			return (MSSQLDBContext.IsOn) ? 
				base.IsSuitable(o) : null;
		}

		public static string String(object o) {
			IndentedWriter w = new IndentedWriter();
			Converter.String(w, o);
			return w.ToString();
		}

		//..................................................................

		public override IndentedWriter DecimalToString(IndentedWriter w, string format, object o) {
			Decimal o1 = (Decimal)o;
			if (o1 < minNumber || o1 > maxNumber)
				// TODO: Нужно завести тип, для задания категории ошибок!
				// Ошибки нужно бросать с кодами или создавать специальніе типы, иначе впоследствии,
				// невозможно будет понять, причину и место возникновения ошибки!

				Error.Critical( new NotSupportedException("Decimal value out of MsSQL Numeric Range"), typeof(Converter));
			
			return w.Write(o1.ToString(format ?? "0.0000", this.FormatProvider));
		}

		public override IndentedWriter EscapeName(IndentedWriter w, string format, string name) {
			w.Write("[");
			base.EscapeName(w, format,name);
			w.Write("]");
			return w;
		}

		public override IndentedWriter DateToString(IndentedWriter w, string format, object o) {
			DateTime o1 = (DateTime)o;
			// В тех случаях, когда функции явно принимают DateTime, а нужно передать null 
			// мы считаем, что DateTime.MinValue == NULL
			if (o1 == DateTime.MinValue) return String(w, null);

			if (o1 < minDate)
				Error.Critical(new NotSupportedException("Date below MsSQL minimal date."), typeof(Converter));

			return w.Write("cast('").Write(o1.ToString( format ?? DateFormat )).Write("' as datetime)");
		}
		
		public override IndentedWriter TypeToString(IndentedWriter w, string format, object o) {
			Type o1 = (Type)o;

			if (MsSqlConverter.TypeMappingTable.ContainsKey(o1))
				w.Write(MsSqlConverter.TypeMappingTable[o1]);
			else {
				w.Write("varchar(100) -- ").Write(o1.ToString());

			}
			return w;
		}
	}

	
	#endregion
	
	#region OracleConverter
	public class OracleConverter : Converter {
		public static Converter Converter = new OracleConverter();
		public static string DateFormat = "yyyy-MM-dd HH:mm:ss";
		//public static string DateFormat = "YYYY-MM-DD HH24:MM:SS";

		public static string[] KeyWords = new string[] {
			"class", "select", "from", "where", "order", "distinct", "and", "or", "like", "level", "exists",
			"connect", "prior", "having", "asc", "desc", "as", "between", "begin", "end", "commit", "transaction",
			"rollback"
		};

		static OracleConverter() {
			Converter.DefaultConverter = (ConvertDelegate)delegate(IndentedWriter w, string format, object o) {
				// OracleConverter деградирует до GenericConverter.
				// используем Write, что бы не зациклиться.
				return GenericConverter.String(w, format, o);
			};

		}

		public OracleConverter(): base() {}

		protected override void Initialize() {
			this[typeof(Decimal)] = DecimalToString;
			this[typeof(DateTime)] = DateToString;
			this[typeof(Name)] = NameToString;
			this[typeof(QueryParameter)] = ParameterToString;
		}

		public override Converter.ConvertDelegate IsSuitable(object o) {
			return (OracleDBContext.IsOn) ? base.IsSuitable(o) : null;
		}

		public static string String(object o) {
			IndentedWriter w = new IndentedWriter();
			Converter.String(w, o);
			return w.ToString();
		}

		//..................................................................

		public override IndentedWriter DecimalToString(IndentedWriter w, string format, object o) {
			Decimal o1 = (Decimal)o;
			return w.Write(o1.ToString(format ?? "0", FormatProvider));
		}

		public override IndentedWriter EscapeName(IndentedWriter w, string format, string name) {
			// TODO: дополнить проверку правильного имени!
			if (name.StartsWith("_") || name.Contains(".") || name.Contains("'") || name.Contains(" ") || IsKeyword(name))
				w.Write("\"").Write(name).Write("\"");
			else
				w.Write(name);
			return w;

		}


		public virtual bool IsKeyword(string name) {
			name = name.ToLower();
			return Array.IndexOf(KeyWords, name) >= 0;
		}

		public override IndentedWriter DateToString(IndentedWriter w, string format, object o) {
			DateTime o1 = (DateTime)o;
			// В тех случаях, когда функции явно принимают DateTime, а нужно передать null 
			// мы считаем, что DateTime.MinValue == NULL
			if (o1 == DateTime.MinValue)
				return String(w, null);

			return w.Write("to_date('").Write(o1.ToString(DateFormat)).Write("', 'YYYY-MM-DD HH24:MI:SS')");
		}

		public override IndentedWriter ParameterToString(IndentedWriter w, string format, object o) {
			QueryParameter qp = (QueryParameter)o;
			if (qp == null) return w;
			string pname = qp.Parameter;
			if (pname.StartsWith("@"))
				pname = pname.Substring(1);

			if ("abcdefghijklmnopqrstuvwxyz".IndexOf(pname.Substring(0,1).ToLower()) <0)
				pname = "p" + pname;

			// TODO: может потребоваться escaping
			return w.Write(":").Write(pname);
		}
	}
	#endregion
}

