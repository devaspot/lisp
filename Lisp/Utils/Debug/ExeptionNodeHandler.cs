using System;
using System.Collections.Generic;
using System.Text;
using Front.Lisp.Debug;
using System.Reflection;

namespace Front.Lisp.Debug {

	public class ExeptionNodeHandler : NodeHandler {

		public override ArrayListSerialized Trace(NodesCollection c, NodeDescriptor d, string arcName) {
			ArrayListSerialized res = new ArrayListSerialized();
			return res;
		}

		public static ExceptionNode ExeptionNodeFactory(NodesCollection col, params object[] args) {
			if (args != null && args.Length > 0)
				return ExeptionNodeHandler.DispatchNode(args[0]);
			return null;
		}

		/// <summary>Развертывание информации в ExceptionNode из полученного экземпляра обьекта</summary>
		public static ExceptionNode DispatchNode(object obj) {
			ExceptionNode node = new ExceptionNode();
			//TODO: дописать разбор объекта
			node.Value = obj.ToString();
			return node;
		}
		
	}


}
