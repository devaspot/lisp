using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace Front {

	public delegate object FastMethodCallDelegate(object target, object[] args);

	public class FastMethodCallBuilder {

		#region Singleton
		//.........................................................................
		private static FastMethodCallBuilder _current = new FastMethodCallBuilder();
		public static FastMethodCallBuilder Current { get { return _current; } }
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public FastMethodCallBuilder() {
			InnerValueIndex.Add(-1, OpCodes.Ldc_I4_M1);
			InnerValueIndex.Add(0, OpCodes.Ldc_I4_0);
			InnerValueIndex.Add(1, OpCodes.Ldc_I4_1);
			InnerValueIndex.Add(2, OpCodes.Ldc_I4_2);
			InnerValueIndex.Add(3, OpCodes.Ldc_I4_3);
			InnerValueIndex.Add(4, OpCodes.Ldc_I4_4);
			InnerValueIndex.Add(5, OpCodes.Ldc_I4_5);
			InnerValueIndex.Add(6, OpCodes.Ldc_I4_6);
			InnerValueIndex.Add(7, OpCodes.Ldc_I4_7);
			InnerValueIndex.Add(8, OpCodes.Ldc_I4_8);
		}
		//.........................................................................
		#endregion


		#region Protected Fields
		//.........................................................................
		protected Dictionary<int, OpCode> InnerValueIndex = new Dictionary<int, OpCode>();
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		/// <summary>Создает делегат для быстрого вызова метода в methodInfo</summary>
		public virtual FastMethodCallDelegate Build(MethodInfo method) {
			ParameterInfo[] methodParameters;
			ILGenerator il = null;
			DynamicMethod optimizedDelegate = new DynamicMethod(string.Format("{0}@{1}", method.Name, method.DeclaringType.Name), 
				typeof(object), 
				new Type[] { typeof(object), typeof(object[]) }, 
				method.DeclaringType.Module, true);
			il = optimizedDelegate.GetILGenerator();
			methodParameters = method.GetParameters();

			LocalBuilder target = null;
			if (!method.IsStatic && !method.DeclaringType.IsClass && !method.DeclaringType.IsInterface) {
				target = il.DeclareLocal(method.DeclaringType);
				il.Emit(OpCodes.Ldarg_0);
				UnBox(il, method.DeclaringType);
				il.Emit(OpCodes.Stloc, target);
			}

			List<LocalBuilder> paramLocals = LoadParameters(il, methodParameters);
		
			if (!method.IsStatic) {
				if (method.DeclaringType.IsClass || method.DeclaringType.IsInterface)
					il.Emit(OpCodes.Ldarg_0);
				else
					il.Emit(OpCodes.Ldloca_S, target);
			}
			// Передача параметров (загрузка в стек)
			for (int i = 0; i < methodParameters.Length; i++) {
				if (methodParameters[i].ParameterType.IsByRef)
					il.Emit(OpCodes.Ldloca_S, paramLocals[i]);
				else
					il.Emit(OpCodes.Ldloc, paramLocals[i]);
			}

			if (method.IsStatic || !method.DeclaringType.IsClass)
				il.EmitCall(OpCodes.Call, method, null);
			else 
				il.EmitCall(OpCodes.Callvirt, method, null);

			if (method.ReturnType == typeof(void))
				il.Emit(OpCodes.Ldnull);
			else if (method.ReturnType.IsValueType)
				il.Emit(OpCodes.Box, method.ReturnType);

			UpdateByRefParameters(il, methodParameters, paramLocals);

			il.Emit(OpCodes.Ret);

			return (FastMethodCallDelegate)optimizedDelegate.CreateDelegate(typeof(FastMethodCallDelegate));
		}

		// обновление ref и out параметров
		protected virtual void UpdateByRefParameters(ILGenerator il, ParameterInfo[] methodParameters, List<LocalBuilder> paramLocals) {
			for (int i = 0; i < methodParameters.Length; i++) {
				ParameterInfo param = methodParameters[i];
				if (param.ParameterType.IsByRef) {
					il.Emit(OpCodes.Ldarg_1);
					if (param.Position >= -1 && param.Position <= 8)
						il.Emit(InnerValueIndex[param.Position]);
					else if (param.Position > -129 && param.Position < 128)
						il.Emit(OpCodes.Ldc_I4_S, (SByte)param.Position);
					else
						il.Emit(OpCodes.Ldc_I4, param.Position);
					il.Emit(OpCodes.Ldloc, paramLocals[i]);
					if (paramLocals[i].LocalType.IsValueType)
						il.Emit(OpCodes.Box, paramLocals[i].LocalType);
					il.Emit(OpCodes.Stelem_Ref);
				}
			}
		}

		// Загрузка параметров
		protected virtual List<LocalBuilder> LoadParameters(ILGenerator il, ParameterInfo[] methodParameters) {
			List<LocalBuilder> paramLocals = new List<LocalBuilder>();
			for (int i = 0; i < methodParameters.Length; i++) {
				ParameterInfo param = methodParameters[i];
				// Локальные параметры в делегате
				System.Type actType = null;
				LocalBuilder parameterLocal = null;
				if (param.ParameterType.IsByRef)
					actType = param.ParameterType.GetElementType();
				else
					actType = param.ParameterType;
				parameterLocal = il.DeclareLocal(actType);
				il.Emit(OpCodes.Ldarg_1);
				if (param.Position >= -1 && param.Position <= 8)
					il.Emit(InnerValueIndex[param.Position]);
				else if (param.Position > -129 && param.Position < 128)
					il.Emit(OpCodes.Ldc_I4_S, (SByte)param.Position);
				else
					il.Emit(OpCodes.Ldc_I4, param.Position);

				// Загружаем переданный параметр в локальный
				il.Emit(OpCodes.Ldelem_Ref);
				// Анбоксинг переданных object-ов
				UnBox(il, actType);
				il.Emit(OpCodes.Stloc, parameterLocal);
				paramLocals.Add(parameterLocal);
			}
			return paramLocals;
		}

		protected virtual void UnBox(ILGenerator il, System.Type actType) {
			if (actType.IsValueType)
				il.Emit(OpCodes.Unbox_Any, actType);
			else
				il.Emit(OpCodes.Castclass, actType);
		}
		//.........................................................................
		#endregion

	}
}
