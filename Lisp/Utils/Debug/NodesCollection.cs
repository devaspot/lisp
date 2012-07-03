using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Soap;
using System.IO;
using System.Collections;
using Front.Lisp.Utils.Debug;


namespace Front.Lisp.Debug {

	[Serializable]
	/// <summary>Хранилище NodeDescriptor'ов, осуществляет посредническую роль для NodeFactory, NodeHandlers которые погружают обьект в NodeDescriptor</summary>
	public class NodesCollection : Dictionary<long, NodeDescriptor> {
		protected long InnerTopKey;

		public TypeDispatcher<NodeDescriptorFactoryDelegate> NodeFactory;
		public TypeDispatcher<NodeHandler> NodeHandlers;

		public long TopKey {
			get {
				return InnerTopKey;
			}
		}

		#region Constructors
		//.........................................................................

		public NodesCollection() {
			InnerTopKey = 0;
			NodeFactory = new TypeDispatcher<NodeDescriptorFactoryDelegate>();
			NodeFactory[typeof(object)] = ReflectionNodeHandler.ReflectedNodeFactory;
			NodeFactory[typeof(Exception)] = ExeptionNodeHandler.ExeptionNodeFactory;
			NodeFactory[typeof(QueryLispEnvironment)] = LispEnvironmentInspector.LispEnvNodeFactory;
			
			NodeFactory.NullValue = delegate { return new NodeDescriptor(null, null, NodeTypes.IsNull); };

			NodeHandlers = new TypeDispatcher<NodeHandler>();
			NodeHandlers[typeof(NodeDescriptor)] = new ReflectionNodeHandler();
			NodeHandlers[typeof(ExceptionNode)] = new ExeptionNodeHandler();
			NodeHandlers[typeof(LispObjectNodeDescriptor)] = new LispEnvironmentInspector();
			NodeHandlers[typeof(LispEnvNodeDescriptor)] = new LispEnvironmentInspector();
		}

		public NodesCollection(long startInd): this()
		{
			InnerTopKey = startInd;
		}

		//.........................................................................
		#endregion

		public virtual long Add(NodeDescriptor node) {
			InnerTopKey++;
			this.Add(TopKey, node);
			node.Key = InnerTopKey;
			return TopKey;
		}

		public virtual NodeDescriptor GetDescriptor(object obj) {
			NodeDescriptorFactoryDelegate f = (obj != null)
													? NodeFactory[obj.GetType()]
													: NodeFactory.NullValue;
			NodeDescriptor d = (f != null) ? f(this, obj) : null;
			if (d != null)
				Add(d);
			return d;
		}

		public virtual ArrayListSerialized TraceArc(long nodeKey, string arcName) {
			NodeDescriptor node = this[nodeKey];
			if (node != null) {
				NodeHandler h = NodeHandlers[node.GetType()]; // XXX вероятно тут скоро прийдется перейти на диспечерезицию по Label, а не по Type
				if (h != null)
					return h.Trace(this, node, arcName);
			}
			return null;
		}

		public virtual ArrayListSerialized GetAllChilds(long key) {
			return TraceArc(key, "special:subnodes");
		}

		public virtual void DeleteItem(long key) {
			if (this.ContainsKey (key)){
				NodeDescriptor node = this[key];
				foreach (long subkey in node.SubNodes) {
					DeleteItem(subkey);
				}
				this.Remove(key);
			}
		}

		public override string ToString() {
			MemoryStream memStream = new MemoryStream();
			SoapFormatter soapSerializer = new SoapFormatter();
			soapSerializer.Serialize(memStream, this);
			StreamReader txtReader = new StreamReader(memStream);
			string serialized = txtReader.ReadToEnd();
			memStream.Dispose();
			return serialized;
		}
	}

	/// <summary>Кастомно сериализуемый потомок ArrayList </summary>
	public class ArrayListSerialized : ArrayList {
		#region Serialization
		//.........................................................................

		public override string ToString() {
			string result = "";
			foreach (object node in this) {
				if (node is NodeDescriptor) {
					result += string.Format(",{0}", ((NodeDescriptor)node).Key);
				} else {
					result += string.Format(",{0}", node);
				}
			}
			if (result.Length > 1)
				result = result.Substring (1);
			
			return result;
		}

		public static ArrayListSerialized Deserialize(string serialized) {
			ArrayListSerialized newArraylist = new ArrayListSerialized();
			if (serialized.Length == 0)
				return null;
			string[] strArray = serialized.Split(',');
			if (strArray.Length == 0)
				return null;
			long val;
			foreach (string strToLong in strArray) {
				if (long.TryParse(strToLong, out val)) 
					newArraylist.Add(val);
			}
			if (newArraylist.Count == 0)
				return null;
			return newArraylist;
		}
		//.........................................................................
		#endregion
	}
}
