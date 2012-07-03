using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Front.Lisp {
	
	// FLOS - .Front Lisp Object System

	// TODO Дописать GenericFunction
	// TODO Нарисовать макросов:
	//      1. дописать make-instance
	//		2. генерация аксесоров
	//		3. defgeneric
	//		4. defmethod
	//		5. wrapmethod
	//		6. find-class
	//		7. subclass?
	//		8. classof
	// TODO А что делать с нестандартными method-combination? - пока не будет их, нах надо!

	/// <summary>Описание слота класса</summary>
	public class SlotDefinition {

		#region Protected Fields
		//.........................................................................
		protected string InnerName;
		protected IFunction InnerDefaultValue;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public SlotDefinition(string name) {
			InnerName = name;
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public string Name {
			get { return InnerName; }
		}

		public IFunction DefaultValue {
			get { return InnerDefaultValue; }
			set { InnerDefaultValue = value; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override int GetHashCode() {
			return InnerName.GetHashCode();
		}
		
		public override bool Equals(object obj) {
			if (obj == null)
				return false;
			SlotDefinition sd = obj as SlotDefinition;
			if (sd == null)
				return false;

			return InnerName == sd.InnerName;
		}

		public object Evaluate() {
			object result = null;
			if (InnerDefaultValue != null)
				result = InnerDefaultValue.Invoke();

			return result;
		}
		//.........................................................................
		#endregion

	}


	/// <summary>описание класса</summary>
	public class ClassDefinition {

		#region Protected Fields
		//.........................................................................
		protected string InnerName;
		protected List<ClassDefinition> InnerDirectSuperClasses = new List<ClassDefinition>();
		protected List<SlotDefinition> InnerSlots = new List<SlotDefinition>();
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public ClassDefinition(string name) {
			InnerName = name;
		}
		//.........................................................................
		#endregion


		#region Public Propreties
		//.........................................................................
		public List<ClassDefinition> DirectSuperClasses {
			get { return InnerDirectSuperClasses; }
		}

		public List<SlotDefinition> Slots {
			get { return InnerSlots; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public override int GetHashCode() {
			return InnerName.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj == null)
				return false;
			ClassDefinition cd = obj as ClassDefinition;
			if (cd == null)
				return false;

			return InnerName == cd.InnerName;
		}

		public List<ClassDefinition> GetPrecedenceList() {
			List<ClassDefinition> list = new List<ClassDefinition>();
			AddClasses(list, this);
			return list;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected void AddClasses(List<ClassDefinition> list, ClassDefinition definition) {
			if (definition != null) {
				list.Add(definition);
				foreach (ClassDefinition cd in definition.DirectSuperClasses)
					AddClasses(list, cd);
			}
		}
		//.........................................................................
		#endregion
	}

	
	/// <summary>
	/// инстанс класса, хранит значения слотов 
	/// </summary>
	/// <remarks>
	/// при создании по definition-у, собираются все слоты, выстраивется линия наследования для GenericFunctions.
	/// </remarks>
	public class ClassInstance {

		#region Protected Fields
		//.........................................................................
		protected ClassDefinition InnerDefinition;
		protected Dictionary<string, object> InnerSlots = new Dictionary<string,object>();
		protected List<ClassDefinition> InnerClassPrecedenceList = new List<ClassDefinition>();
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		public ClassInstance(ClassDefinition definition) {
			if (definition == null)
				Error.Critical(new NullReferenceException("definition"), typeof(ClassInstance));

			InnerDefinition = definition;
			AllocateClass();
		}
		//.........................................................................
		#endregion


		#region Public Properties
		//.........................................................................
		public ClassDefinition Definition {
			get { return InnerDefinition; }
		}

		public Dictionary<string, object> Slots {
			get { return InnerSlots; }
		}

		public List<ClassDefinition> ClassPrecedenceList {
			get { return InnerClassPrecedenceList; }
		}

		public object this[string slotName] {
			get { return GetSlotValue(slotName); }
			set { SetSlotValue(slotName, value); }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual void ChangeType(ClassDefinition newcd) {
			InnerDefinition = newcd;
			AllocateClass();
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected virtual void AllocateClass() {
			// обходим дерево определений классов и строим class-precedence-list, по которому забираем себе слоты
			InnerClassPrecedenceList.Clear();
			AddClasses(InnerDefinition);
			foreach (ClassDefinition cd in InnerClassPrecedenceList) {
				foreach (SlotDefinition sd in cd.Slots)
					InitSlot(sd);
			}
		}

		protected void AddClasses(ClassDefinition definition) {
			if (definition == null)
				Error.Warning(new ArgumentNullException("definition"), typeof(ClassInstance));
			else {
				InnerClassPrecedenceList.AddRange(definition.GetPrecedenceList());
			}
		}

		protected virtual void InitSlot(SlotDefinition slot) {
			if (slot == null)
				Error.Warning(new ArgumentNullException("slot"), typeof(ClassInstance));
			else {
				if (GetSlotValue(slot.Name) == null)
					SetSlotValue(slot.Name, slot.Evaluate());
			}
		}

		protected virtual object GetSlotValue(string slotName) {
			object result = null;
			if (slotName != null) 			
				InnerSlots.TryGetValue(slotName, out result);
			return result;
		}

		protected virtual void SetSlotValue(string slotName, object slotValue) {
			if (slotName == null)
				Error.Warning(new ArgumentNullException("slotName"), typeof(ClassInstance));
			else
				InnerSlots[slotName] = slotValue; // разрешаем автоматом добавлять новые слоты
		}
		//.........................................................................
		#endregion
	}


	/// <summary>обобщенная функция</summary>
	public class GenericFunction : IFunction {

		#region Protected Fields
		//.........................................................................
		protected SimpleMethodDispatcher InnerBody = new SimpleMethodDispatcher();
		protected SimpleMethodDispatcher InnerAround = new SimpleMethodDispatcher();
		protected SimpleMethodDispatcher InnerBefore = new SimpleMethodDispatcher();
		protected SimpleMethodDispatcher InnerAfter = new SimpleMethodDispatcher();
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public SimpleMethodDispatcher Body {
			get { return InnerBody; }
		}

		public SimpleMethodDispatcher Around {
			get { return InnerAround; }
		}

		public SimpleMethodDispatcher Before {
			get { return InnerBefore; }
		}

		public SimpleMethodDispatcher After {
			get { return InnerAfter; }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual object Invoke(params object[] args) {
			List<IFunction> before = ComputeInvocationList(Before, args);
			List<IFunction> after = ComputeInvocationList(After, args);
			List<IFunction> around = ComputeInvocationList(Around, args);
			List<IFunction> body = ComputeInvocationList(Body, args);

			// TODO Начинаем шустрим по методам! метод будет не обычный. внего nextfunc будем передавать и наш контекст
			int count = 0;
			for (int i = 0; i < around.Count; i++) {
				//around[i].Invoke(
			}
			return null;
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected List<IFunction> ComputeInvocationList(SimpleMethodDispatcher smd, object[] args) {
			List<IFunction> list = null;
			object t = typeof(object);
			if (args != null && args.Length > 0 && args[0] != null) {
				if (args[0] is ClassInstance || args[0] is ClassDefinition) {
					t = args[0];
				} else
					t = args[0].GetType();
			}
			list = smd.ComputeMethodList(t);

			return list;
		}
		//.........................................................................
		#endregion
	}

	public class TypeSpecializer {
		protected int InnerHash = 0;
		protected ArrayList InnerTypes = new ArrayList();

		public TypeSpecializer() : this(null) { }
		public TypeSpecializer(IEnumerable e) {
			if (e != null)
				foreach (object o in e)
					Add(o);
		}

		public ArrayList Types {
			get { return InnerTypes; }
		}

		public void Add(object o) {
			if (o == null)
				Error.Warning(new ArgumentNullException("o"), typeof(TypeSpecializer));
			else {
				if (o is Type || o is ClassDefinition) {
					InnerTypes.Add(o);
					InnerHash += o.GetHashCode();
				} else if (o is ClassInstance)
					Add((o as ClassInstance).Definition);
				else
					Add(o.GetType());
			}
		}

		public override int GetHashCode() {
			return InnerHash;
		}

		public override bool Equals(object obj) {
			TypeSpecializer ts = obj as TypeSpecializer;
			return (ts != null && 
				ts.InnerHash == InnerHash && 
				ts.InnerTypes.Count == InnerTypes.Count) ;
		}
	}


	// Диспатч по одному типу
	public class SimpleMethodDispatcher {

		#region Protected Fields
		//.........................................................................
		protected FLOSTypeDispatcher<IFunction> InnerDispatcher = new FLOSTypeDispatcher<IFunction>();
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public void AddMethod(object type, IFunction func) {
			InnerDispatcher.SetValue(type, func);
		}

		public List<IFunction> ComputeMethodList(object o) {
			Type p = o as Type;
			if (p == null && !(o is ClassInstance))
				o = o.GetType();

			List<IFunction> list = new List<IFunction>();
			object nextType = o;
			IFunction func = null;
			do {
				func = InnerDispatcher.GetValue(o, ref nextType);
				if (func != null)
					list.Add(func);
			} while (func != null);

			return list;
		}
		//.........................................................................
		#endregion
	}

	// TODO Дописать! и сделать нормальный GenericFunction на его основе!
	public class GenericMethodDispatcher {

		#region Protected Fields
		//.........................................................................
		protected FLOSTypeDispatcher<GenericMethodDispatcher> InnerDispatcher 
			= new FLOSTypeDispatcher<GenericMethodDispatcher>();
		protected IFunction InnerFunction;
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public void AddMethod(TypeSpecializer ts, IFunction func) {
			GenericMethodDispatcher gcm = this;
			object next = null;
			foreach (object type in ts.Types) {
				bool contains = gcm.InnerDispatcher.Contains(type);
				GenericMethodDispatcher temp = (contains)
					? gcm.InnerDispatcher.GetValue(type, ref next)
					: new GenericMethodDispatcher();
				if (!contains)
					gcm.InnerDispatcher.SetValue(type, temp);
				gcm = temp;
			}
			gcm.InnerFunction = func;
		}

		public IFunction FindMethod(params object[] args) {
			GenericMethodDispatcher gcm = this;
			object temp = null;
			foreach (object o in args) {
				gcm = gcm.InnerDispatcher.GetValue(o, ref temp);
				if (gcm == null)
					return null;
			}

			return gcm.InnerFunction;
		}
		//.........................................................................
		#endregion
	}

	public class LispTypeDispatcher<T> {

		#region Protected Fields
		//.........................................................................
		protected Dictionary<ClassDefinition, T> InnerValues = new Dictionary<ClassDefinition, T>();
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public void SetValue(ClassDefinition cd, T value) {
			InnerValues[cd] = value;
		}

		public T GetValue(ClassInstance ci, ref int nextTypeIndex) {
			T result = default(T);
			for (int i = nextTypeIndex; i < ci.ClassPrecedenceList.Count; i++) {
				ClassDefinition cd = ci.ClassPrecedenceList[i];
				if (InnerValues.ContainsKey(cd)) {
					result = InnerValues[cd];
					nextTypeIndex = i + 1;
					if (nextTypeIndex > ci.ClassPrecedenceList.Count)
						nextTypeIndex = -1;
					break;
				}

			}

			return result;
		}

		public bool Contains(ClassDefinition cd) {
			return InnerValues.ContainsKey(cd);
		}
		//.........................................................................
		#endregion

	}

	public class FLOSTypeDispatcher<T> {

		#region Protected Fields
		//.........................................................................
		protected TypeDispatcher<T> InnerNativeTypes = new TypeDispatcher<T>();
		protected LispTypeDispatcher<T> InnerLispTypes = new LispTypeDispatcher<T>();
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public void SetValue(object o, T value) {
			if (o == null)
				Error.Warning(new ArgumentNullException("o"), GetType());
			else {
				if (o is Type)
					SetValue(o as Type, value);
				else if (o is ClassDefinition)
					SetValue(o as ClassDefinition, value);
				else
					SetValue(o.GetType(), value);
			}
		}

		public void SetValue(Type t, T value) {
			InnerNativeTypes[t] = value;
		}

		public void SetValue(ClassDefinition cd, T value) {
			InnerLispTypes.SetValue(cd, value);
		}

		public T GetValue(object type, ref object nextType) {
			T result = default(T);
			if (type == null)
				Error.Warning(new ArgumentNullException("o"), GetType());
			else {
				if (type is Type)
					result = GetValue(type as Type, ref nextType);
				else if (type is ClassDefinition)
					result = GetValue(type as ClassDefinition, ref nextType);
				else
					result = GetValue(type.GetType(), ref nextType);
			}

			return result;
		}

		public T GetValue(ClassInstance ci, ref object nextTypeIndex) {
			int ind = 0;
			if (nextTypeIndex is int) {
				ind = (int)nextTypeIndex;
			}
			T result = InnerLispTypes.GetValue(ci, ref ind);
			nextTypeIndex = ind;
			return result;
		}

		public T GetValue(Type t, ref object nextType) {
			T result = default(T);
			Type newt = nextType as Type;
			if (newt != null)
				t = newt;
			if (t != null) {
				result = InnerNativeTypes.TryGetValue(t, out newt);
				nextType = newt.BaseType;
			}

			return result;
		}

		public bool Contains(object o) {
			bool result = false;
			if (o is Type) {
				Type rt;
				InnerNativeTypes.TryGetValue(o as Type, out rt);
				result = rt.Equals(o);
			} else if (o is ClassDefinition) {
				result = InnerLispTypes.Contains(o as ClassDefinition);
			}

			return result;
		}
		//.........................................................................
		#endregion
	}
}
