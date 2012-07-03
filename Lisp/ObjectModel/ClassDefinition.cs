using System;
using System.Runtime.Serialization;
using Front.Collections;
using System.Collections.Generic;
using System.Collections;

namespace Front.ObjectModel {

	/// <summary>Описание класса</summary>
	// TODO: может унаследовать от List<SlotDefinition> ?
	// хорошая была мысль NamedValueCollection....

	// TODO: нужно научить класс убирать Extension'ы, предков и переставлять их местами в любом порядке
	// TODO: нужно хранить список "своих" слотов, иначе при изменении порядка наследования,
	// они потяряются безвозвратно

	public class ClassDefinition : SchemeNode, ISerializable {

		#region Protected Fields
		//.........................................................................
		protected bool CopyExtensions = true;
		protected string InnerName; // имя с учетом Namespace?
		protected string InnerClassName; // чистое имя
		protected string InnerFullName; // version
		protected Guid InnerVersion = Guid.NewGuid();

		// полный список деклараций, в котором находимся и мы!
		// приоритет: E3 -> E2 -> E1 -> мы -> S3 -> S2 -> S1		
		protected FixedOrderDictionary InnerFullInheritanceList = null;

		// соодержит список непосредственных расширений и себя
		protected FixedOrderDictionary InnerInheritanceList = new FixedOrderDictionary();

		// TODO: нужно где-то хранить список собственных слотов!
		// Иначе после расширения (или изменения порядка наследования мы не сможем 
		// восстановить правильный список
		protected Hashtable InnerSlots = new Hashtable(); // -> SlotDefinition
		protected Hashtable InnerMethods = new Hashtable(); // -> MethodDefinition

		protected BehaviorDispatcher InnerBehavior;
		//.........................................................................
		#endregion


		#region Constructors
		//.........................................................................
		protected ClassDefinition() {}

		public ClassDefinition(SchemeNode parent, string name, bool copyExtensions, ClassDefinition[] extensions, params SlotDefinition[] slots) 
			: base(parent) {

			if (parent == null)
				InnerIsSchemeEditable = false;

			CopyExtensions = copyExtensions;

			InnerName = name;
			InnerFullName = Version.ToString("D");

			if (extensions != null && extensions.Length > 0)
				foreach (ClassDefinition ext in extensions)
					if (!InnerInheritanceList.Contains(ext.Name))
						InnerExtend(ext, false, true);
			
			InnerInheritanceList[KeyName] = this;
				// Если в InheritanceList-е будут FullName, то потом сложно производить 
				// диспач для бихейверов.
				// всущности, фул-нейм нужен, что бы сигнализировать о наличии "динамических" классов всписке
				// но не понятно, зачем это нужно!
			
			// XXX был Add, поменял на [] (Pilya)
			//KeyName,
			//	this);

			if (slots != null && slots.Length > 0)
				foreach (SlotDefinition slot in slots)
					if (slot != null)
						InnerAddSlot(slot, true, true);

			if (CopyExtensions)
				InnerBehavior = new BehaviorDispatcher(this);
			else
				;// ?? надо думать!
		}

		public ClassDefinition(SchemeNode parent, string name, ClassDefinition[] extensions, params SlotDefinition[] slots) 
			: this(parent, name, true, extensions, slots) { }

		public ClassDefinition(string name, bool copyExtensions, ClassDefinition[] extensions, params SlotDefinition[] slots)
			:this(null, name, copyExtensions, extensions, slots) { }

		public ClassDefinition(string name, bool copyExtensions, params ClassDefinition[] extensions)
			: this(null, name, copyExtensions, extensions, null) { }

		public ClassDefinition(string name, ClassDefinition[] extensions, params SlotDefinition[] slots) 
			: this(null, name, extensions, slots) { }

		public ClassDefinition(string name, ClassDefinition extension, params SlotDefinition[] slots) 
			: this(name, new ClassDefinition[] { extension }, slots) { }

		public ClassDefinition(string name, params SlotDefinition[] slots) 
			: this(null, name, (ClassDefinition[])null, slots) { }

		public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
			// TODO: написать!
		}
		//.........................................................................
		#endregion


		// XXX возможно вырисуется общая концепция сигнализирования об изменении схемы
		// (но всеравно прийжется как-то передавать характер изменений!)
		public event EventHandler<SlotListChangedEventArgs> AfterSlotListChanged;
		public event EventHandler<MethodListChangedEventArgs> AfterMethodListChanged;
		public event EventHandler<ExtensionListChangedEventArgs> AfterExtensionListChanged;


		#region Public Propreties
		//.........................................................................
		public string ClassName {
			// TODO этого метода не должно быть! но у инстансов имя класса запорчено! :-(
			// как вариант, можем рассматривать "сложное имя": префикс + имя + версия
			get { return GetClassName(); }
		}

		public string Name {
			get { return GetName(); }
		}

		public string FullName {
			get { return GetFullName(); }
		}

		public Guid Version {
			get { return InnerVersion; }
		}

		public FixedOrderDictionary InheritanceList {
			// TODO: тут нужно выдавать копию списка либо ReadOnly-обертку!
			get {
				if (InnerFullInheritanceList == null)
					InnerFullInheritanceList = PopulateInheritanceList(new FixedOrderDictionary(), -1);
				return InnerFullInheritanceList;
			}
		}

		public List<SlotDefinition> Slots {
			get { return GetSlots(); }
		}

		public SlotDefinition this[string name] {
			get { return GetSlot(name); }
		}

		public BehaviorDispatcher Behavior {
			get { return InnerBehavior; }
		}

		public List<MethodDefinition> Methods {
			get { return GetMethods(); }
		}
		//.........................................................................
		#endregion


		#region Public Methods
		//.........................................................................
		public virtual void AttachBehavior(BehaviorDispatcher bd) {
			if (bd != null) {
				bd.Parent = InnerBehavior;
				InnerBehavior = bd;
			}
		}

		public virtual BehaviorDispatcher DetachBehavior() {
			if (InnerBehavior != null && InnerBehavior.Parent != null)
				InnerBehavior = InnerBehavior.Parent;

			return InnerBehavior;
		}

		public override int GetHashCode() {
			return InnerVersion.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj == null)
				return false;
			ClassDefinition cd = obj as ClassDefinition;
			if (cd == null)
				return false;

			// TODO: сомнительно! расходится с GetHashCode...
			// нужна более детальная спецификация на то, как пользоваться классами и что означает версионирование!
			// (пока идея в том, что бы класс мог выступать оберткой имея то же имя)
			return (InnerVersion == cd.Version);
		}

		public virtual SlotDefinition AddSlot(SlotDefinition slot) {
			return InnerAddSlot(slot, true, true);
		}

		public virtual SlotDefinition RemoveSlot(string slotName) {
			return InnerRemoveSlot(slotName, true, true);
		}

		public virtual List<SlotDefinition> GetSlots() {
			List<SlotDefinition> res = new List<SlotDefinition>();

			Hashtable slts = InnerSlots;
			// XXX что-то не правильно работает! 
			if (!CopyExtensions) {
				slts = new Hashtable();
				GetSlots(slts);
			}

			foreach (SlotDefinition sd in slts.Values)
				res.Add( sd );

			return res;
		}

		protected virtual Hashtable GetSlots(Hashtable slts) {
			//if (CopyExtensions) {
			//	foreach (DictionaryEntry de in InnerSlots)
			//		slts[de.Key] = de.Value;
			//
			//} else {
				foreach (ClassDefinition cls in InnerInheritanceList.Values) {
					if (cls == this) {
						foreach (DictionaryEntry de in InnerSlots)
							slts[de.Key] = de.Value;
					} else
						cls.GetSlots(slts);
				}
			//}
			return slts;
		}

		public virtual List<SlotDefinition> GetOwnSlots() {
			List<SlotDefinition> res = new List<SlotDefinition>();
			
			// такой же сигнатурой метятся слоты при добавлении!
			string name = (CopyExtensions) ? Name : FullName;

			foreach (SlotDefinition sd in InnerSlots.Values)
				if (sd.DeclaredClass == name)
					res.Add(sd);
			return res;
		}

		public virtual SlotDefinition GetSlot(Name slotName) {
			return GetSlot(slotName);
			// TODO: нужно трассировать "путь"
		}

		public virtual SlotDefinition GetSlot(string slotName) {
			// TODO: как бороться с конфликтами?

			SlotDefinition res = null;

			if (!CopyExtensions) {
				// TODO: правильнее искать: у Extension'ов, у себя, у родителей
				// тоесть список нужно пробегать в обратном порядке,

				foreach(ClassDefinition cd in InnerInheritanceList.Values) {					
					res = (cd == this)
								? (SlotDefinition)InnerSlots[slotName]
								: cd.GetSlot(slotName);

					if (res != null) return res;
				}
			} else
				res = (SlotDefinition)InnerSlots[slotName];

			return res;
		}

		public virtual MethodDefinition AddMethod(MethodDefinition method) {
			return InnerAddMethod(method, true, true);
		}

		public virtual void AddMethods(IEnumerable<MethodDefinition> methods) {
			if (methods != null)
				foreach (MethodDefinition md in methods)
					AddMethod(md);
		}

		public virtual MethodDefinition RemoveMethod(string methodName) {
			return InnerRemoveMethod(methodName, true, true);
		}

		/// <summary>Список собственных методов класса. Кому нужен полный - см. BehaviorDispatcher</summary>
		public virtual List<MethodDefinition> GetMethods() {
			List<MethodDefinition> res = new List<MethodDefinition>();
			foreach (MethodDefinition m in InnerMethods.Values)
				res.Add(m);

			// TODO: можно закешировать, что бы не генерить каждый раз...
			return res;
		}

		public virtual MethodDefinition GetMethod(string methodName) {
			MethodDefinition res = (MethodDefinition)InnerMethods[methodName];
			return res;
		}

		public virtual void Extend(params ClassDefinition[] extensions) {
			if (extensions != null && extensions.Length > 0)
				Extend(false, extensions);
		}

		public virtual void Extend(bool parent, params ClassDefinition[] extensions) {
			if (extensions == null || extensions.Length == 0) return;

			ArrayList a = new ArrayList();
			foreach (ClassDefinition ext in extensions) {
				if (InnerExtend(ext, false, parent))
					a.Add(ext);
			}
			if (a.Count > 0) {
				OnExtension(ListChangeType.Add, (ClassDefinition[])a.ToArray(typeof(ClassDefinition)));
			}
		}

		public virtual FixedOrderDictionary PopulateInheritanceList(FixedOrderDictionary ext, int index) {
			if (ext == null)
				ext = new FixedOrderDictionary();

			string name = KeyName;

			// XXX так изголялись, что бы менять порядок прямо в списке, а теперь тупо список перестраиваем с 0...
			if (index >= 0)
				ext.InsertAt(index, name, this);

			//for (int i = 0; i < InnerInheritanceList.Count; i++) {
			foreach (string cname in InnerInheritanceList.Keys) {
				ClassDefinition cd = (ClassDefinition)InnerInheritanceList[cname];
				if (cd != this)
					cd.PopulateInheritanceList(ext, (index < 0) ? index : ext.GetKeyIndex(name));
				else if (index < 0)
					ext[name] = this;
			}

			return ext;
		}

		public override string ToString() {
			return string.Format("{0} Class: {1}. {2} Slots. {3} Methods", GetType().Name, Name, Slots.Count, Methods.Count);
		}
		//.........................................................................
		#endregion


		#region Protected Methods
		//.........................................................................
		protected string KeyName {
			get { return (InnerName != null && InnerName != "") ? InnerName : InnerFullName; }
		}

		protected virtual string GetName() {
			// если имя у класса не задано, то он берет имя первого родителя
			// если родителей нету - то имя = версия

			if (InnerName != null && InnerName != "" ) return InnerName;
			if (!CopyExtensions && InnerInheritanceList.Count > 0) {
				ClassDefinition cd = InnerInheritanceList[0] as ClassDefinition;;
				if (cd != null && cd != this) {
					string cd_name = cd.Name;
					string cd_f_name = cd.FullName;
					if (cd_name != cd_f_name) return cd_name;
				}
			}
			return InnerFullName;
		}

		protected virtual string GetClassName() {
			if (InnerClassName != null && InnerClassName !="") return InnerClassName;
			if (InnerName != null) {
				int x = InnerName.IndexOf("-instance");
				if (x >=0)
					InnerClassName = InnerName.Substring(0, x);
				else
					InnerClassName = InnerName;
			}
			return InnerClassName;	
		}

		protected virtual string GetFullName() {
			string name = GetName();
			if (name != InnerFullName)
				return name + "-" + InnerFullName;
			return InnerFullName;
		}


		protected virtual bool InnerExtend(ClassDefinition extension, bool raiseEvent, bool parent) {
			if (extension == null || extension == this) return false;

			string name = KeyName;

			int ext_index = InnerInheritanceList.GetKeyIndex(extension.Name);
			int my_index = (parent) ? InnerInheritanceList.GetKeyIndex( name ) : -1;

			// все алгоритмы должны работать для "my_index", 
			// тогда можно будет менять порядок своих предков
			if (ext_index > 0 && my_index > 0 && my_index >= ext_index) 
				// класс уже есть в списке наследования перед нами
				return false;

			InnerFullInheritanceList = null; // что бы пересчитать потом

			if (CopyExtensions) {
				// добавить список наследования "перед" указанным индексом
				// (если my_index < 0, то добавим в конец)

				//extension.PopulateInheritanceList( InnerInheritanceList, my_index);
				// { для варианта "хранить только список непосредственных родителей"
				if (parent && my_index >= 0)
					InnerInheritanceList.InsertAt(my_index, extension.Name, extension);
				else 
					InnerInheritanceList[extension.Name] =  extension;
				// }

				if (ext_index < 0) {
					UpdateSlotListAfterExtention(ListChangeType.Add, extension);

				} else {
					// TODO: если extension переместили, то список определения слотов тоже
					// должен перестроиться..
					// и методов...
				}

			} else {
				if (parent && my_index >= 0)
					InnerInheritanceList.InsertAt(my_index, extension.Name, extension);
				else
					InnerInheritanceList[extension.Name] = extension;
				// XXX точно не нужно навещивать обработчики?
				// extension.AfterSlotListChanged += OnExtensionSlotListChanged;
			}

			if ( raiseEvent )
				OnExtension(ListChangeType.Add, extension);

			return true;
		}

		protected virtual SlotDefinition InnerAddSlot(SlotDefinition slot, bool riseEvent, bool replace) {
			if (CheckReadOnlyScheme())
				return null;

			if (slot != null && slot.Name != null && slot.Name != "") {
				bool f = false;
				lock (InnerSlots) {
					if (!InnerSlots.ContainsKey(slot.Name)) {
						InnerSlots.Add(slot.Name, slot);
						f = true;
					} else if (replace) {
						InnerSlots[slot.Name] = slot;
						f = true;
					}
				}
				if (f) { // производим вызов события вне критической секции!
					if (slot.DeclaredClass == null) {
						// это потом как проверять? может в DeclaredClass писать сугубо Version?
						slot.DeclaredClass = (CopyExtensions) ? Name : FullName;
						slot.ParentNode = this;
					}
					if (slot.ParentNode == null)
						slot.ParentNode = this; // XXX но до этого не должно дойти!

					if (riseEvent)
						OnSlotListChanged(ListChangeType.Add, slot);
				}
				return slot;
			}
			return null;
		}

		/// <summary>возвращает новый слот, если удаление привело к "открытию слота другого родителя"</summary> 
		protected virtual SlotDefinition InnerRemoveSlot(string slotName, bool riseEvent, bool analize) {
			if (slotName == null || slotName.Trim() == "") return null;
			SlotDefinition slot = null;
			SlotDefinition res = null;
			lock (InnerSlots) {
				slot = InnerSlots[slotName] as SlotDefinition;
				if (slot != null)
					InnerSlots.Remove(slotName);
			}
			if (slot != null) {
				if (analize) 
					// TODO: дописать!
					;

				if (riseEvent) // не вызываем собятия, если небыло фактического удаления
					OnSlotListChanged(ListChangeType.Remove, slot);
			}
			return res;
		}

		protected virtual MethodDefinition InnerAddMethod(MethodDefinition method, bool riseEvent, bool replace) {
			if (method == null) return null;

			InnerMethods[method.Name] = method;
			if (method.DeclaredClass == null || method.DeclaredClass == "")
				method.DeclaredClass = (CopyExtensions) ? Name : InnerFullName;

			MethodDefinition res = InnerBehavior.AttachMethod(this, method);
			if (riseEvent)
				OnMethodListChanged(ListChangeType.Add, method);

			return res;
		}

		protected virtual MethodDefinition InnerRemoveMethod(string methodName, bool riseEvent, bool replace) {
			throw new NotImplementedException();
		}





		protected virtual void OnSlotListChanged(ListChangeType ct, params SlotDefinition[] slt) {
			EventHandler<SlotListChangedEventArgs> h = AfterSlotListChanged;
			if (h != null)
				h(this, new SlotListChangedEventArgs(ct, slt));
		}

		protected virtual void OnMethodListChanged(ListChangeType ct, params MethodDefinition[] methods) {
			EventHandler<MethodListChangedEventArgs> h = AfterMethodListChanged;
			if (h != null)
				h(this, new MethodListChangedEventArgs(ct, methods));
		}

		protected virtual void OnExtension(ListChangeType ct, params ClassDefinition[] extensions) {
			EventHandler<ExtensionListChangedEventArgs> h = AfterExtensionListChanged;
			if (h != null) {
				ExtensionListChangedEventArgs args = new ExtensionListChangedEventArgs(ct, extensions);
				h(this, args);
			}
		}

		/// <summary>TODO: много вопросов с перекрытием - пока не трогаем!</summary>
		protected virtual void OnExtensionSlotListChanged(object sender, SlotListChangedEventArgs args) {

			// TODO: много пересозданий EventArgs'ов - нах нужно
			if (!CopyExtensions) {
				if (args.ChangeType == ListChangeType.Add || args.ChangeType == ListChangeType.Inherit)
					OnSlotListChanged(ListChangeType.Inherit, args.Slots);
				else
					OnSlotListChanged(args.ChangeType, args.Slots);

			} else if (args.ChangeType == ListChangeType.Add || args.ChangeType == ListChangeType.Inherit) {
				foreach (SlotDefinition s in args.Slots)
					// TODO: новые слоты перекрывают старые. это может оказаться неправильным!
					// если добавился слот в класс, который в списке наследования ниже того
					// класса, чей метод перекрывается (кто-то что-то понял?)
					InnerAddSlot(s, false, true); 

				OnSlotListChanged(ListChangeType.Inherit, args.Slots);

			} else if (args.ChangeType == ListChangeType.Remove) {
				foreach (SlotDefinition s in args.Slots)
					// TODO: удаление слота у одного родителя может означать "проявление" другого 
					// перекрытого слота от другого родителя.
					InnerRemoveSlot(s.Name, false, true);
			}
		}

		protected virtual void OnExtensionMethodListChanged(object sender, MethodListChangedEventArgs args) {
			// актуализировать Dispatcher
		}

		protected virtual void OnExtensionExtend(object sender, ExtensionListChangedEventArgs args) {
			// TODO: перестроить списки слотов и методов...
			InnerFullInheritanceList = null;
			UpdateSlotListAfterExtention(args.ChangeType, args.Extensions);
			OnExtension(ListChangeType.Update, args.Extensions);
		}

		protected virtual void UpdateSlotListAfterExtention(ListChangeType ct, params ClassDefinition[] exts) {
			if (exts == null || exts.Length == 0) return;

			// собираем у себя все слоты родителей
		foreach (ClassDefinition extension in exts) {
				if (ct == ListChangeType.Remove) {
					// TODO: дописать (см. так же RemoveSlot
					;
				} else {
					foreach (SlotDefinition sd in extension.Slots) {
						// TODO: что делать с конфликтами?

						// XXX Не правильно! нужно проверять, где находится extention 
						// в списке наследования, до или после cd.ClassDeclared...
						InnerSlots[sd.Name] = sd;
					}

					// сначала снимаем, потом навешиваем обработчики, что бы
					// не было двойного навешивания...

					extension.AfterSlotListChanged -= OnExtensionSlotListChanged;
					extension.AfterMethodListChanged -= OnExtensionMethodListChanged;
					extension.AfterExtensionListChanged -= OnExtensionExtend;

					// TODO: объединить диспечеры!
					extension.AfterSlotListChanged += OnExtensionSlotListChanged;
					extension.AfterMethodListChanged += OnExtensionMethodListChanged;
					extension.AfterExtensionListChanged += OnExtensionExtend;
				}
			}
		}
		//.........................................................................
		#endregion


		#region Supplementary Methods
		//.........................................................................
		//public static List<ClassDefinition> MakePrecedenceList(List<ClassDefinition> list, ClassDefinition definition) {
		//    if (list == null)
		//        list = new List<ClassDefinition>();

		//    if (definition != null) {
		//        list.Add(definition);
		//        foreach (ClassDefinition cd in definition.InheritanceList)
		//            MakePrecedenceList(list, cd);
		//    }

		//    return list;
		//}
		//.........................................................................
		#endregion

	}


	public enum ListChangeType {
		None,    // ничего не произошло - так, балуемся
		Add,     // добавилось
		Remove,  // убавилось
		Update,  // что-то обновилось 
		Inherit  // появился новый предок (типа добавилось)
	}


}
