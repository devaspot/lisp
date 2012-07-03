using System;
using Front.Collections;
using System.Collections.Generic;

namespace Front.ObjectModel {
	
	///<summary>Базовый класс для элементов метаинформации, обеспечивает проверку "редактируемости"</summary>
	// TODO: возможно следует сюда же внести версионирование схемы.
	public class SchemeNode {

		protected bool InnerIsSchemeEditable = true;
		protected SchemeNode InnerParentNode = null;
		protected SchemeNode InnerRootNode = null;


		#region Constructors
		//................................................................................
		public SchemeNode() : this( false ) {}

		public SchemeNode(SchemeNode parentNode) {
			InnerParentNode = parentNode;
			InnerRootNode = (InnerParentNode != null) ? InnerParentNode.RootNode : null;
		}

		public SchemeNode(bool isEditable) {
			InnerIsSchemeEditable = isEditable;
		}
		//................................................................................
		#endregion


		// TODO: как-то много получится "наблюдателей", если схема будет сильно ветвиться
		// например, если все пропертя, биххейверы и объекты будут смотреть на класс...
		// (Съедим всю память! и будем медленно ворочаться... очитывая, что
		//  реально мониторинг изменений нужен только в некоторых случаях!)

		// Вся эта суматоха только потому, что нода не знает свои "под-ноды"...
		// хотя в каждом конкретном случае - таки знает!
		public event EventHandler AfterParentChanged;
		public event EventHandler AfterRootChanged;


		#region Public Properties
		//...............................................................................
		public SchemeNode ParentNode {
			get { return InnerParentNode; }
			set { InnerSetParentNode(value); }
		}

		public SchemeNode RootNode {
			get { return InnerRootNode; }
		}

		public bool IsSchemeEditable { 
			get { return !CheckReadOnlyScheme(); }
		}
		//...............................................................................
		#endregion


		// TODO: есть смысл подумать о том, что бы как-то сигнализировать об изменениях в метаинформации
		//		как должен обрабатываться этот сигнал? всплывать вврех или опускаться вниз?
		//		это может зависеть от характера сигнала.
		//		(смена Parent/Root уже значительно все усложнила... не хочется громоздить здеть Rocket Science)
		public virtual void SetSchemeEditable(bool editable) {
			InnerIsSchemeEditable = editable;
		}


		#region Protected Methods
		//...............................................................................
		protected virtual SchemeNode InnerSetParentNode(SchemeNode node) {
			lock (this) {
				if (!CheckReadOnlyScheme()) {
					DetachParent();

					InnerParentNode = node;
					SchemeNode sn = InnerRootNode;
					InnerRootNode = (InnerParentNode != null && InnerRootNode != InnerParentNode.RootNode)
								? InnerParentNode.RootNode : null;

					AttachParent(node);

					OnAfterParentChanged();

					if (sn != InnerRootNode) // не маячим зазря сменой рута, если небыло фактической смены!
						OnAfterRootChanged();
				}
			}
			return InnerParentNode;
		}

		protected virtual bool CheckReadOnlyScheme() {
			// TODO: дописать!
			if (InnerParentNode != null)
				return InnerParentNode.IsSchemeEditable;
			return InnerIsSchemeEditable;
		}

		/// <summary>Снимает обработчики событий с родительской ноды</summary>
		protected virtual void DetachParent() {
			SchemeNode n = InnerParentNode;
			if (n == null) return;
			n.AfterParentChanged -= ParentChangedEventHandler;
			n.AfterRootChanged -= RootChangedEventHandler;
		}

		/// <summary>Устанавливает обработчики событий на родительскую ноду</summary>
		protected virtual void AttachParent(SchemeNode pnode) {
			if (pnode == null) return;
			pnode.AfterParentChanged += ParentChangedEventHandler;
			pnode.AfterRootChanged += RootChangedEventHandler;
		}

		protected virtual void OnAfterParentChanged() {
			EventHandler h = AfterParentChanged;
			if (h != null)
				h(this, EventArgs.Empty);
		}

		protected virtual void OnAfterRootChanged() {
			EventHandler h = AfterRootChanged;
			if (h != null)
				h(this, EventArgs.Empty);
		}

		protected virtual void ParentChangedEventHandler(object sender, EventArgs args) {
		}

		protected virtual void RootChangedEventHandler(object sender, EventArgs args) {
			// XXX код дублируется с InnerSetParentNode с отличием в вызове OnParentChanged...

			SchemeNode sn = InnerRootNode;
			InnerRootNode = (InnerParentNode != null && InnerRootNode != InnerParentNode.RootNode)
						? InnerParentNode.RootNode : null;

			if (sn != InnerRootNode) // не маячим зазря сменой рута, если небыло фактической смены!
				OnAfterRootChanged();
		}
		//...............................................................................
		#endregion
	}

}
