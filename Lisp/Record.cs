using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;

namespace Front.Lisp {

	public class Record : HybridDictionary {

		#region Protected Fields
		//.........................................................................
		protected static HybridDictionary InnerRecordTypes = new HybridDictionary();
		protected static AssemblyBuilder InnerAssembly;
		protected static ModuleBuilder InnerModule;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public Record()	: base() { }
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public static Type CreateRecordType(String name, Type basetype) {
			Type t = (Type)InnerRecordTypes[name];
			if (t != null)
				return t;
			if (!typeof(Record).IsAssignableFrom(basetype))
				throw new LispException("Record types must be derived from Record or another Record type");
			InnerRecordTypes[name] = t = MakeRecord(name, basetype);
			return t;
		}

		public static Type MakeRecord(String name, Type basetype) {
			if (InnerAssembly == null) {
				AssemblyName assemblyName = new AssemblyName();
				assemblyName.Name = "RecordAssembly";
				InnerAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
				InnerModule = InnerAssembly.DefineDynamicModule("RecordModule");
			}

			TypeBuilder tb = InnerModule.DefineType(name, TypeAttributes.Class | TypeAttributes.Public, basetype);
			Type[] paramTypes = Type.EmptyTypes;
			ConstructorBuilder cb = tb.DefineConstructor(MethodAttributes.Public,
																		CallingConventions.Standard,
																		paramTypes);
			ILGenerator constructorIL = cb.GetILGenerator();
			constructorIL.Emit(OpCodes.Ldarg_0);
			ConstructorInfo superConstructor = basetype.GetConstructor(Type.EmptyTypes);
			constructorIL.Emit(OpCodes.Call, superConstructor);
			constructorIL.Emit(OpCodes.Ret);


			Type t = tb.CreateType();
			//Import.AddType(t); //must do in lisp
			return t;
		}
		//.........................................................................
		#endregion
	}
}