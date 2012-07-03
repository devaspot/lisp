using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;

namespace Front.Lisp.Debug {

	public delegate NodeDescriptor NodeDescriptorFactoryDelegate(NodesCollection col, params object[] args);
	
	public abstract class	NodeHandler {

		public abstract ArrayListSerialized Trace(NodesCollection c, NodeDescriptor d, string arcName);
	}

	public class ReflectionNodeHandler : NodeHandler {

		public override ArrayListSerialized Trace(NodesCollection c, NodeDescriptor d, string arcName) {
			arcName = (arcName == null) ? null : arcName.ToLower();

			ArrayListSerialized res = new ArrayListSerialized();
			int x = arcName.IndexOf(":");
			string ns = "";
			if (x >= 0) {
				ns = arcName.Substring(0, x).Trim();
				arcName = arcName.Substring(x + 1).Trim();
			}

			if (ns == "special") {
				if (arcName == "subnodes") {
					// TODO: рефлектим на запчасти!
					res = SubNodes(c, d);
				}
			} else {
				if (ns == "") {
					// TODO: ищем Member'ы c именем arcName
				} else {
					// TODO: пока ничего... потом придумаем :-)
				}
			}
			return res;
		}

		#region Reflection 
		//.........................................................................

		protected virtual ArrayListSerialized SubNodes(NodesCollection c, NodeDescriptor node) {
			if (node.MembersCount == 0) return null;
			if (node.NodeObject == null) return null;

			if (node.SubNodes == null) node.SubNodes = new ArrayListSerialized();
			if (node.SubNodes.Count > 0)
				return node.SubNodes;
			
			node.SubNodes.AddRange(GetPublicFields(c, node));
			node.SubNodes.AddRange(GetPrivateFields(c, node));
			node.SubNodes.AddRange(GetPublicProperties(c, node));
			node.SubNodes.AddRange(GetPrivateProperties(c, node));

			return node.SubNodes;
		}

		//Public'i Fields	
		protected virtual ArrayList GetPublicFields(NodesCollection c, NodeDescriptor node) {
			ArrayList publishedFields = new ArrayList();
			Type typeresolver = node.NodeObject.GetType();
			
			FieldInfo[] publicFields = typeresolver.GetFields(BindingFlags.Public | BindingFlags.Instance);
			if (publicFields.Length > 0) {
				foreach (FieldInfo field in publicFields) { 
					object obj = field.GetValue(node.NodeObject);
					NodeDescriptor subNode = c.GetDescriptor(obj);

					subNode.NodeName = field.Name;
					subNode.NodeMembership = NodeMemberships.isField;
					subNode.NodePublicity = NodePublicities.isPublic;

					node.SubNodes.Add(subNode);
				}
			}
			return publishedFields;
		}

		//Private'i Fields
		protected virtual ArrayList GetPrivateFields(NodesCollection c, NodeDescriptor node) {
			ArrayList publishedFields = new ArrayList();
			Type typeresolver = node.NodeObject.GetType();

			FieldInfo[] privateFields = typeresolver.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
			if (privateFields.Length > 0) {
				foreach (FieldInfo field in privateFields) {
					object obj = field.GetValue(node.NodeObject);
					NodeDescriptor subNode = c.GetDescriptor(obj);

					subNode.NodeName = field.Name;
					subNode.NodeMembership = NodeMemberships.isField;
					subNode.NodePublicity = NodePublicities.isPrivate;

					node.SubNodes.Add(subNode);
				}
			}
			return publishedFields;
		}

		protected virtual ArrayList GetPrivateProperties(NodesCollection c, NodeDescriptor node) {
			ArrayList publishedFields = new ArrayList();
			Type typeresolver = node.NodeObject.GetType();
			
			PropertyInfo[] publicProperties = typeresolver.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			if (publicProperties.Length > 0) {
				foreach (PropertyInfo property in publicProperties) {
					NodeDescriptor subNode = ExtractProperty(c, node, property);

					subNode.NodeName = property.Name;
					subNode.NodeMembership = NodeMemberships.isProperty;
					subNode.NodePublicity = NodePublicities.isPublic;

					node.SubNodes.Add(subNode);
				}
			}
			return publishedFields;
		}

		protected virtual ArrayList GetPublicProperties(NodesCollection c, NodeDescriptor node) {
			ArrayList publishedFields = new ArrayList();
			Type typeresolver = node.NodeObject.GetType();

			PropertyInfo[] publicProperties = typeresolver.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
			if (publicProperties.Length > 0) {
				foreach (PropertyInfo property in publicProperties) {
					NodeDescriptor subNode = ExtractProperty(c, node, property);

					subNode.NodeName = property.Name;
					subNode.NodeMembership = NodeMemberships.isProperty;
					subNode.NodePublicity = NodePublicities.isNonPublic;

					node.SubNodes.Add(subNode);
				}
			}
			return publishedFields;
		}

		protected virtual NodeDescriptor ExtractProperty(NodesCollection c, NodeDescriptor node, PropertyInfo property) {
			NodeDescriptor subNode;
			object obj = null;

			ParameterInfo[] parInfos = property.GetIndexParameters();
			if (parInfos.Length == 0) {
				try {
					obj = property.GetValue(node.NodeObject, null);
					subNode = c.GetDescriptor(obj);
				} catch (Exception ex){
					return c.GetDescriptor(ex);
				}

			} else {
				obj = property;
				subNode = c.GetDescriptor(obj);

				subNode.MembersCount = parInfos.Length;
				subNode.TypeName = property.ReflectedType.Name;

				subNode.Value = "";
				foreach (ParameterInfo parInfo in parInfos) {
					subNode.Value += string.Format("[{0} {1}]  ", parInfo.Name, parInfo.ParameterType.Name);
				}
			}
			return subNode;
		}

		//.........................................................................
		#endregion

		#region Supplementary Methods
		//.........................................................

		static readonly Type[] simpleTypes = {
			typeof(string), 
			typeof(byte), 
			typeof(int), 
			typeof(long), 
			typeof(decimal), 
			typeof(float),
			typeof(char) };

		public static NodeDescriptor ReflectedNodeFactory(NodesCollection col, params object[] args) {
			if (args != null && args.Length > 0)
				return ReflectionNodeHandler.DispatchNode(args[0]);
			return null;
		}

		/// <summary>Развертывание информации в NodeDescriptor из полученного экземпляра обьекта</summary>
		public static NodeDescriptor DispatchNode(object obj) {
			NodeDescriptor node = new NodeDescriptor();
			node.NodeObject = obj;

			if (obj == null) {
				node.NodeType = NodeTypes.IsNull;
				return node;
			}

			Type typeresolver = obj.GetType();
			node.TypeName = typeresolver.ToString();

			PropertyInfo[] fields = typeresolver.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (PropertyInfo pi in fields) {				
			}
			node.MembersCount = typeresolver.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length
			+ typeresolver.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length;
			


			node.NodeObject = obj;

			// строки считаем тоже "простым значением" (они всеравно immutable)
			foreach (Type tt in simpleTypes) {
				if (tt == typeresolver) {
					node.NodeType = NodeTypes.IsValue;
					node.MembersCount = 0;
					node.Value = obj.ToString();
					return node;
				}
			}

			if (obj is Exception) {
				node.NodeType = NodeTypes.IsException;
				node.Value = ((Exception)obj).Message;
				return node;
			}

			if (node.NodeObject is ValueType) {
				node.NodeType = NodeTypes.IsStruct;
			} else {
				node.NodeType = NodeTypes.IsObject;
			}
			node.Value = obj.ToString();
			return node;
		}
		//.........................................................
		#endregion
	}
}
