//$Id: CSVTools.cs 129 2006-04-06 12:00:46Z pilya $

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Front.Tools {

	/// <summary>”тилитарный класс дл€ чтени€ CSV-строк (Comma Separated Values).</summary>
	public class CSVReader {
		public readonly char Separator;

		public CSVReader() : this(',') {}
		public CSVReader(char sep) {
			Separator = sep;
		}

	    public virtual string[] ParseCSVLine(string data) {
			if (data == null) return null;
			if (data.Length == 0) return new string[0];

			ArrayList result = new ArrayList();
			ParseCSVFields(result, data);
			return (string[])result.ToArray(typeof(string));
		}

		public virtual void ParseCSVFields(ArrayList result, string data) {
			int pos = -1;
			while (pos < data.Length)
			result.Add(ParseCSVField(data, ref pos));
		}

		// Parses the field at the given position of the data, modified pos to match
		// the first unparsed position and returns the parsed field
		public virtual string ParseCSVField(string data, ref int startSeparatorPosition) {
			if (startSeparatorPosition == data.Length-1) {
				startSeparatorPosition++;
				// The last field is empty
				return "";
 			}

			int fromPos = startSeparatorPosition + 1;
			// Determine if this is a quoted field
			if (data[fromPos] == '"') {
				// If we're at the end of the string, let's consider this a field that
				// only contains the quote
				if (fromPos == data.Length-1) {
					fromPos++;
					return "\"";
				}

				// Otherwise, return a string of appropriate length with double quotes collapsed
				// Note that FSQ returns data.Length if no single quote was found
				int nextSingleQuote = FindSingleQuote(data, fromPos+1);
				startSeparatorPosition = nextSingleQuote+1;
				return data.Substring(fromPos+1, nextSingleQuote-fromPos-1).Replace("\"\"", "\"");
			}

			// The field ends in the next comma or EOL
			int nextComma = data.IndexOf(this.Separator, fromPos);
			if (nextComma == -1) {
				startSeparatorPosition = data.Length;
				return data.Substring(fromPos);
			} else {
				startSeparatorPosition = nextComma;
				return data.Substring(fromPos, nextComma-fromPos);
			}
		}

		// Returns the index of the next single quote mark in the string 
		// (starting from startFrom)
		protected int FindSingleQuote(string data, int startFrom) {
			int i;
			for (i = startFrom; i < data.Length; i++) {
				// If this is a double quote, bypass the chars
				if (data[i] == '"') {
					if (i < data.Length-1 && data[i+1] == '"') i++; else break;
				}
			}
      		return i;
		}
	}


	/// <summary>”тилитарный класс дл€ записи значений в SCV виде (Comma Separated Values).</summary>
	public static class CSVWriter {
		
		/// <summary>ѕредставл€ет словарь в виде строки "key1"="value1";"key2"="value2";"key3"=null;...</summary>
		/// <remarks><para>ѕри этом производитс€ защита строки от специальных символов.</para>
		/// <para>≈сли словарь не задан (null) или пустой - возвращает пустую строку.</para></remarks>
		public static string DictionaryCSV( IDictionary d ) {
			if (d == null) return "";
			
			// спекулируем на апроксимации длинны пары {ключь:значени} в 64 символа
			System.Text.StringBuilder res = new System.Text.StringBuilder( d.Keys.Count * 64 );
			foreach (object key in d.Keys) {
				string k = EscapeString( key.ToString() );
				string v = "null";
				if ( d[key] != null )
					v = EscapeString( d[key].ToString() );
				if (res.Length > 0) res.Append(";");
				// TODO DF0015: избирательно добавл€ть кавычки
				res.Append("\"");
				res.Append(k);
				res.Append("\"=\"");
				res.Append(v);
				res.Append("\"");
			}
			return res.ToString();
		}
		
		/// <summary>защищает строку от специальных символов (" \ \n \r )</summary>
		public static string EscapeString(string value) {
			return (value != null)
				? value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r","\\r") 
				: "null";
		}
		
		
		//TODO DF0016: перенести в CSVReader (»ли вообще убрать) 
		/*
		public static IDictionary StringToDictionary(string str) {
			IDictionary d = new SortedList();
			if (str == null || str == "") return d;
			ArrayList pairs = StringSplit(str, ';');
			foreach(string pair in pairs){
				ArrayList keyval = StringSplit(pair, '=');
				if (keyval.Count>=2){
					d.Add( UnEscape(keyval[0].ToString()), UnEscape(keyval[1].ToString()));
				}
			}
			return d;
		}

		
		// split строки, с учетом ескейпа
		private static ArrayList StringSplit(string str, char separate_simbol){
			ArrayList list = new ArrayList();
			if (str == null || str.Trim() == "") return list;
			int  search_start=0;
			int  copy_start=0;
			while ( copy_start < str.Length && search_start < str.Length){
				int ind = str.IndexOf(separate_simbol, search_start);
				if (ind==-1){ list.Add(str.Substring(copy_start)); break;}
				//int last_slesh = str.LastIndexOf( "\\", ind - 1, ind - search_start ); 
				int slesh_count = 0;
				if (ind>0 && str[ind-1]=='\\'){
					int first_slesh = ind-1;
					// бежим в обратном пор€дке и ищем начало группы слешей
					while (first_slesh>=0 && str[first_slesh--] == '\\')	slesh_count++;
				}
				if ((slesh_count%2) == 1){ // количество слешей непарное, т.е. один из них ескейпит separate_simbol
					search_start = ind+1;
				}else{
					list.Add(str.Substring(copy_start, ind-copy_start));
					copy_start = search_start = ind+1;
				}
			}
			return list;
		}

		
		// удал€ютс€ слеши, а каждый символ, который был после слеша, остаетс€ 
		// (даже если это тоже был слеш)
		private static string UnEscape(string str){
			if (str==null) return "";
			for (int i=0; i<str.Length; i++)
				if(str[i]=='\\') str = str.Remove(i,1);
			return str;
		}
		*/
	}
}

