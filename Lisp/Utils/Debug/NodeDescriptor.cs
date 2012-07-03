#region Using
//..................................................................................
using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Serialization.Formatters.Soap;
using System.IO;
using System.Collections;
using Front.Lisp.Debug;
using System.Runtime.Serialization;

using System.Reflection;
//..................................................................................
#endregion

namespace Front.Lisp.Debug {

	/// <summary>ƒл€ обеспечени€ передачи данных между Widget-level и Lisp-level</summary>
	[Serializable]
	public class NodeDescriptor {
		#region Public fields
		//.........................................................................
		public long Key;

		[NonSerialized]
		public long LastSubNodeKey;

		public NodePublicities NodePublicity;
		public NodeMemberships NodeMembership;

		public string NodeType;
		public string TypeName;
		public string NodeName;

		/// <summary>ѕуть к значению в цепочке объектов (или команда получени€ значени€)</summary>
		public string NodePath;
		public int MembersCount;

		public string Value;

		[NonSerialized]
		public ArrayListSerialized SubNodes;

		[NonSerialized]
		public object NodeObject;
		//.........................................................................
		#endregion

		#region Constructors
		//.........................................................................
		public NodeDescriptor() { }

		/// <summary>ƒл€ тех случаев, когда тип значени€ может быть "ожидаем" извне, но не может быть получен из самого значени€ (например, »з null).</summary>
		public NodeDescriptor(Type valueType, string nodePath, string nodeType) : this( valueType, nodePath, nodeType, null) {
		}

		public NodeDescriptor(Type valueType, string nodePath, string nodeType, object value) {
			if (value != null && valueType == null)
				valueType = value.GetType();
			TypeName = (valueType != null) ? valueType.ToString() : "<type unknown>";
			NodePath = nodePath;
			NodeName = nodePath;
			NodeType = nodeType;
			if (nodeType == null) {
				NodeType = (value == null)
								? NodeTypes.IsNull
								: (value is Exception)
									? NodeTypes.IsException
									: NodeTypes.IsObject;
				// TODO: а как на самом деле нужнео производить этот разбор?
				// пон€тно, что это дело Handler'а, но тогда его нужно как-то сделать публично-доступным...
			}
			NodeObject = value;
			if (NodeObject != null)
				Value = NodeObject.ToString();

		}

		//.........................................................................
		#endregion

		#region Public methods 
		//.........................................................................

		//public List<NodeDescriptor> GetChildsByName(string name) {
		//    return null;
		//}

		//.........................................................................
		#endregion

		#region Reflection
		//.........................................................................

		/// <summary>ѕолучение следующего ребенка</summary>
		//public NodeDescriptor ResolveNextChild() {
		//    if (NodeObject == null) {
		//        return new NodeDescriptor(new Exception("Evaluation has determined null refference to object definition"));
		//    }
		//    Type typeresolver = NodeObject.GetType();

		//    MemberInfo[] members = typeresolver.GetMembers(BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance);


		//    long index = SubNodes.Count;						

		//    FieldInfo[] publicFields = typeresolver.GetFields(BindingFlags.Public | BindingFlags.Instance);
		//    if (publicFields.Length > index) {
		//        //Public'i Fields				
		//        object obj = publicFields[index].GetValue(NodeObject);
		//        NodeDescriptor node = new NodeDescriptor(obj);
		//        node.NodeName = publicFields[index].Name;
		//        node.NodeMembership = NodeMemberships.isField;
		//        node.NodePublicity = NodePublicities.isPublic;
		//        return node;
		//    }

		//    index = index - publicFields.Length;
		//    FieldInfo[] privateFields = typeresolver.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

		//    if (privateFields.Length > index) {
		//        //Private'i Fields				
		//        object obj = privateFields[index].GetValue(NodeObject);
		//        NodeDescriptor node = new NodeDescriptor(obj);
		//        node.NodeName = privateFields[index].Name;
		//        node.NodeMembership = NodeMemberships.isField;
		//        node.NodePublicity = NodePublicities.isNonPublic;
		//        return node;
		//    }

		//    index = index - privateFields.Length;
		//    PropertyInfo[] publicProperties = typeresolver.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		//    if (publicProperties.Length > index) {
		//        //Public'i Properties		
		//        NodeDescriptor node = ExtractProperty(index, publicProperties);

		//        node.NodeMembership = NodeMemberships.isProperty;
		//        node.NodePublicity = NodePublicities.isPublic;
		//        return node;
		//    }

		//    index = index - publicProperties.Length;
		//    PropertyInfo[] privateProperties = typeresolver.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);

		//    if (privateProperties.Length > index) {
		//        //Public'i Properties				
		//        NodeDescriptor node = ExtractProperty(index, privateProperties);

		//        node.NodeMembership = NodeMemberships.isProperty;
		//        node.NodePublicity = NodePublicities.isNonPublic;
		//        return node;
		//    }
		//    return null;
		//}

		//protected NodeDescriptor ExtractProperty(long index, PropertyInfo[] publicProperties) {
		//    PropertyInfo pip = publicProperties[index];
		//    object obj = null;
		//    NodeDescriptor node = null;

		//    ParameterInfo[] parInfos = pip.GetIndexParameters();
		//    if (parInfos.Length == 0) {
		//        obj = pip.GetValue(NodeObject, null);
		//        node = new NodeDescriptor(obj);
		//        node.NodeName = pip.Name;
		//    } else {
		//        obj = pip;
		//        node = new NodeDescriptor(obj);
		//        node.MembersCount = parInfos.Length;
		//        node.NodeName = pip.Name;
		//        node.TypeName = pip.ReflectedType.Name;

		//        node.Value = "";
		//        foreach (ParameterInfo parInfo in parInfos) {
		//            node.Value += string.Format("[{0} {1}]  ", parInfo.Name, parInfo.ParameterType.Name);
		//        }
		//    }
		//    return node;
		//}

		//.........................................................................
		#endregion

		#region Public properties
		//.........................................................................

		//public List<NodeDescriptor> SubNodes {
		//    get {
		//        if (InnerSubNodes == null) {
		//            InnerSubNodes = new List<NodeDescriptor>();
		//        }
		//        return InnerSubNodes;
		//    }
		//}

		/// <summary>¬озвращает количество полей у обьекта описываемого дескриптором</summary>
		//public int ChildsCount {
		//    get {
		//        if (NodeObject == null) {
		//            return 0;
		//        }
		//        Type typeresolver = NodeObject.GetType();
		//        MemberInfo[] members = typeresolver.GetMembers(BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance);
		//        return members.Length;

		//        #region Old Code
		//        //FieldInfo[] publicFields = typeresolver.GetFields(BindingFlags.Public | BindingFlags.Instance);
		//        //int count = publicFields.Length;
		//        //FieldInfo[] privateFields = typeresolver.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
		//        //count += privateFields.Length;

		//        //PropertyInfo[] pupi = typeresolver.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		//        //count += pupi.Length;

		//        //return count;
		//        #endregion
		//    }
		//}

		//.........................................................................
		#endregion

		#region Serialization
		//.........................................................................

		public override string ToString() {

			MemoryStream memStream = new MemoryStream();
			SoapFormatter formatter = new SoapFormatter();

			formatter.Serialize(memStream, this);

			memStream.Position = 0;
			StreamReader txtReader = new StreamReader(memStream);
			string serialized = txtReader.ReadToEnd();
			memStream.Close();

			return serialized;
		}

		public string ToString(bool testing) {
			return string.Format("Key {0},\n NodeName {1},\n NodeType {2},  TypeName {3},\n " +
				"MembersCount {4},\n Value {5},\n NodePublicity {6},\n NodeMembership {7}",
				Key, NodeName, NodeType, TypeName, MembersCount, Value, NodePublicity, NodeMembership);
		}

		public static NodeDescriptor Deserialize(string serialized) {
			NodeDescriptor apIn = null;
			MemoryStream memStream = new MemoryStream();
			try {
				SoapFormatter formatter = new SoapFormatter();
				StreamWriter txtWriter = new StreamWriter(memStream);

				txtWriter.Write(serialized);
				txtWriter.Flush();
				memStream.Position = 0;

				apIn = (NodeDescriptor)formatter.Deserialize(memStream);
				return apIn;
			} catch (SerializationException e) {
				Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
				throw;
			} finally {
				memStream.Close();
			}
		}

		//.........................................................................
		#endregion

	}

	#region Spec enums
	//.........................................................................

	public static class NodeTypes {
		public const string IsValue = "value";
		public const string IsObject = "object";
		public const string IsStruct = "struct";
		public const string IsException = "exception";
		public const string IsNull = "null";
	}

	public enum NodePublicities {
		isPublic,
		isNonPublic,
		isPrivate,
		isProtected
	}

	public enum NodeMemberships {
		isMember,
		isProperty,
		isField,
		isMethod,
		isParameter
	}

	//.........................................................................
	#endregion
}
