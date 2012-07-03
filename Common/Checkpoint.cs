using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;


// TODO:
//   1) Писать чекпоинты в XML-формате.  <checkpoint Name="..." Tick="..."/>
//	  2) Выводить значения переменных в рамках чекпоинта.  <p1>value</p1>
//	  3) Производить агрегирование переменных в чекпоинтах.
//	  4) Исключать время вычисления переменных в чекпоинтах.
//	  5) Возможность задания анализируемых чекпоинтов.
//	  6) Возможность проведения вложенных тестов.

namespace Front.Diagnostics {

	public enum UnitOfMeasure {
		Ticks,
		Microseconds,
		Milliseconds,
		Seconds
	}
	

	//..................................................................................
	public struct Checkpoint {
		internal static Log Log;

		static Checkpoint() {
			Log = new Log(new TraceSwitch("Front.Diagnostics.Checkpoint", "Front.Diagnostics.Checkpoint", "Verbose"));
		}

		public static void Write( string label ) {
			Log.Verb("[CP] {0}: {1}", label, Stopwatch.GetTimestamp());
			//Trace.WriteLine(Stopwatch.GetTimestamp(), "[CP] " + label);
		}
		

		public Checkpoint( string label, long tick ) {
			Label = label;
			Tick = tick;
		}

		public string Label;
		public long   Tick;
	}

	//..................................................................................
	public class PerfTest {
		private TextWriterTraceListener  traceListener;
		private string		               traceFileName;
		private bool                     isRunning = false;
		private PerfTestResult				testResult;
		private UnitOfMeasure				unitOfMeasure;

		public PerfTest( string name, string descr ) : this(name, descr, UnitOfMeasure.Milliseconds) {}
		public PerfTest( string name, string descr, UnitOfMeasure unit ) {
			unitOfMeasure = unit;
			traceFileName = GetNextFileName(name);

			FileStream traceLog = new FileStream(traceFileName, FileMode.Create);
			traceListener = new TextWriterTraceListener(traceLog);
			Trace.Listeners.Add(traceListener);
			Trace.AutoFlush = true;
			Trace.WriteLine(DateTime.Now);
			Trace.WriteLine(name);
			Trace.WriteLine( (descr != null) ? descr : "" );
			isRunning = true;
		}

		public void  Finish() {
			Trace.Listeners.Remove(traceListener);
			traceListener.Flush();
			traceListener.Close();
			isRunning = false;
			testResult = new PerfTestResult(traceFileName, unitOfMeasure);
		}

		public PerfTestResult  Result {
			get {
				if( isRunning ) 
					throw new ApplicationException( "The test is not finished." );
				else {
					return testResult;
				};
			}
		}

		public UnitOfMeasure  Unit {
			get { return unitOfMeasure; }
			set { unitOfMeasure = value; }
		}

		/// <summary> Выводит среднюю статистику по всем итерациям </summary>
		public void  PrintResult() {
			Result.Print();
		}

		/// <summary>Выводит статистику по каждой итерации</summary>
		public void  PrintValues() {
			Result.PrintValues();
		}

		public static string  GetNextFileName( string prefix ) {
			int testNumber = 0;

			DirectoryInfo dir = new DirectoryInfo(".");
			foreach( FileSystemInfo fsi in dir.GetFileSystemInfos() ) 
			{
				if( fsi is FileInfo )
				{
					FileInfo f = (FileInfo)fsi;
					int pos = f.Name.IndexOf( prefix, 0 );
					if( pos != -1 && f.Name.Length >= prefix.Length + 8 )
					{
						string str_num = f.Name.Substring( prefix.Length, 4 );
						int num;
						if( Int32.TryParse( str_num, out num) && num >= testNumber )
							testNumber = num + 1;
					}
				}
			}

			string str1 = testNumber.ToString().PadLeft(4, '0');
			return String.Format("{0}{1}.txt", prefix, str1);
		}
	}

	//..................................................................................
	public class PerfTestResult {
		/// <summary> Хеш-таблица: (имя метки, среднее расстояние от предыдущей) </summary>
		private Dictionary<string,long>  AverageValues = new Dictionary<string,long>();
		private List<Dictionary<string,long>>  AllValues = new List<Dictionary<string,long>>();
		/// <summary>Список уникальных имен меток </summary>
		private List<string>  labels = new List<string>();
		private string name;
		private string descr;
		private DateTime time;
		private static UnitOfMeasure unit = UnitOfMeasure.Milliseconds;

		public PerfTestResult()  {}
		public PerfTestResult( string fileName ) : this(fileName, unit) {}
		public PerfTestResult( string fileName, UnitOfMeasure unit ) {
			Load(fileName, unit);
		}

		public long this[string index] {
			get { return AverageValues[index]; }
		}

		public void  Load( string fileName ) {
			Load(fileName, unit);
		}

		public void  Load( string fileName, UnitOfMeasure unitOfMeasure ) {
			AverageValues.Clear();
			AllValues.Clear();
			labels.Clear();

			//  Чтение файла
			List<Checkpoint>  points = new List<Checkpoint>();
			using (StreamReader sr = new StreamReader(fileName)) {
				string str = sr.ReadLine();              // time
				DateTime.TryParse(str, out time);
				name = sr.ReadLine();					// name
				descr = sr.ReadLine();					// description

				string line;
				while ((line = sr.ReadLine()) != null)
				{
					string[] split = line.Split( new char[]{' '} );
					if( split[0] == "[CP]" )
					{
						string label = split[1].Trim( new char[]{':'} );
						string tick = split[2].Trim();
						long longTick = Int64.Parse(tick);

						points.Add( new Checkpoint(label, longTick) );
					}
				}
			}

			string startLabel = points[0].Label;
			points.Add( new Checkpoint(startLabel, 0) );  // завершающая метка
			
			//  Вычисление статистики
			//  Создание списка меток без повторений.
			AverageValues[startLabel] = 0;
			Dictionary<string,long>  CurrentValues = new Dictionary<string,long>();
			CurrentValues[startLabel] = 0;

			// Цикл по блокам, от startLabel до startLabel
			int num_blocks = 0;
			for (int i = 1; i < points.Count; i++) {
				if( points[i].Label == startLabel ) { // конец старого и начало нового блока
					num_blocks++;
					AllValues.Add(CurrentValues);
					CurrentValues = new Dictionary<string,long>();
					CurrentValues[startLabel] = 0;

				} else {
					if( !AverageValues.ContainsKey( points[i].Label ) )
						AverageValues.Add( points[i].Label, 0);
					if( !CurrentValues.ContainsKey( points[i].Label ) )
						CurrentValues.Add( points[i].Label, 0);
					
					AverageValues[ points[i].Label ] += points[i].Tick - points[i-1].Tick;
					CurrentValues[ points[i].Label ] += points[i].Tick - points[i-1].Tick;
				}
			}

			//  Заполнение таблицы расстояний между метками
			foreach (string key in AverageValues.Keys)
				labels.Add(key);

			foreach (Dictionary<string,long> values in AllValues) {
				NormalizeValues(values, unitOfMeasure);
			}

			NormalizeValues(AverageValues, unitOfMeasure);
			AverageValues.Add("#Count", num_blocks);
			foreach (string key in labels) {
				AverageValues[key] = AverageValues[key] / num_blocks;
			}
			AverageValues[startLabel] = AverageValues["#Total"] / AverageValues["#Count"];
		}

		private void  NormalizeValues( Dictionary<string,long> values, UnitOfMeasure unitOfMeasure ) {
			long total_time = 0;
			foreach (string key in labels) {
				if( !values.ContainsKey(key) )
					continue;

				switch (unitOfMeasure) {
					case UnitOfMeasure.Ticks:
						break;
					case UnitOfMeasure.Microseconds:
						values[key] = (long)(values[key] * 1000000.0 / Stopwatch.Frequency);
						break;
					case UnitOfMeasure.Milliseconds:
						values[key] = (long)(values[key] * 1000.0 / Stopwatch.Frequency);
						break;
					case UnitOfMeasure.Seconds:
						values[key] = (long)(values[key] / Stopwatch.Frequency);
						break;
				}
				total_time += values[key];
			}
			values.Add("#Total", total_time);
		}

		public void  Print() {
			if( labels.Count <= 1)
				return;

			Console.WriteLine(("").PadRight(57, '-'));
			Console.WriteLine( "{0,-45} {1,11}", "Block Average:", InsertCommas( AverageValues[labels[0]] ) );
			Console.WriteLine(("").PadRight(57, '-'));

			for (int i = 1; i < labels.Count; i++)  {
				long x = AverageValues[ labels[i] ];
				string label = String.Format("{0} - {1}:", labels[i-1], labels[i]);
				Console.WriteLine( "{0, -45} {1,11}", label, InsertCommas(x) );
			}
			Console.WriteLine(("").PadRight(57, '-'));
			Console.WriteLine( "{0,-45} {1,11}", "Total:", InsertCommas( AverageValues["#Total"] ));
			Console.WriteLine( "{0,-45} {1,11}", "Count:", AverageValues["#Count"] );
			Console.WriteLine();
		}

		public void  PrintValues() {
			int iter = 0;
			foreach (Dictionary<string,long> values in AllValues) {
				Console.WriteLine(("").PadRight(57, '-'));
				Console.WriteLine("Iteration {0}", iter++);
				Console.WriteLine(("").PadRight(57, '-'));
				for (int i = 1; i < labels.Count; i++) {
					if( !values.ContainsKey( labels[i] ) )
						continue;
					long x = values[ labels[i] ];
					string label = String.Format("{0} - {1}:", labels[i-1], labels[i]);
					Console.WriteLine( "{0, -45} {1,11}", label, InsertCommas(x) );
				}
				//Console.WriteLine();
				Console.WriteLine( "{0,-45} {1,11}", "Total:", InsertCommas( values["#Total"] ));
			}
			Console.WriteLine();
		}

		/// <summary> расставление запятых в выводимых числах </summary>
		public static string  InsertCommas(long x) {
			string s = x.ToString();
			int numCommas = (s.Length - 1)/3;
			char[] s1 = new char[s.Length + numCommas];

			for (int i = s.Length-1, j = s1.Length-1, k = 0; i >= 0; ) {
				if( k == 3 ) {
					s1[j--] = ',';
					k = 0;
				}
				s1[j--] = s[i--];
				k++;
			}
			return new string(s1);
		}
	}
}
