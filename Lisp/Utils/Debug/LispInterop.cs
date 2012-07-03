using System;
using System.Collections.Generic;
using System.Text;
using Front.Lisp.Debug;


namespace Front.Lisp {

	/// <summary>Интерфейс позволяющий скрыть внутреннюю организацию взаимодействия с Lisp-машиной.
	/// LispLoader делает подмену LispCmdExec на Debugger в случае add-in</summary>
	public interface ILispIterop {
		NodeDescriptor Eval(string str);
		NodeDescriptor EvalQ(string str);

		string SymbolValue(string name);
		/// <summary>список загруженых файлов</summary>
		string[] Files();
		/// <summary>соодержимое файла</summary>
		string File(string name);

		/// <summary>загружает файл</summary>
		void LoadFile(string path);

		/// <summary>готовность к работе</summary>
		bool CheckAvailability { get; }

		/// <summary>погружает объект в лисп</summary>
		void Intern(string symname, object symbol);

		/// <summary>Получить лист по ключу</summary>
		NodeDescriptor GetNodeByKey(long key);

		/// <summary>Получить субноды узла по ключу</summary>
		ArrayListSerialized GetAllChilds(long key);

		ArrayListSerialized TraceArc(long key, string arkName);
		
		// XXX: как мы сможем погрузить в лисп локальную переменную из
		// текущего StackFrame?

		// TODO: дописать методы обработки ошибок и стека!

		// TODO: понадобятся методы для упрощения разбора/просмотра структур данных
		// в том числе и сериализация/десериализация...
		// (!) для работы в DTE может понадобиться хелпер-класс, который будет работать в окружении
		// студии и будет разворачивать то, что вернет DTE
	}
	
}
