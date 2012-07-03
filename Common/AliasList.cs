// $Id$

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Remoting.Messaging;

namespace Front {

	/// <summary>—ловарь коротких синонимов дл€ имен.</summary>
	/// <remarks>»м€ и его короткий вариант хран€тс€ в виде структур <see cref="Name"/></remarks>
	public class AliasList : MarshalByRefObject, IDisposable {
		bool _disposed = false;
		public bool IsDisposed { get {return _disposed;} }

		internal class Pair {
			public Name N;
			public Name A;
			public Pair(Name n, Name a) { N = n; A = a; }
			public Pair(string n, string a) : this(new Name(n), new Name(a)) {}
		}

		protected IDictionary d = new SortedList();
		protected object backup;
		public string Name; //XXX
		protected string KeyName;
		protected bool isPublished = true;

		protected AliasList Parent = null;
		protected bool falldown = true;

		public string Separator = "_";
		
		/// <summary>Ётот объект публикует себ€ в контекст вызова, а при уничтожении - убирает себ€.</summary>
		public AliasList(string name) : this(name, true) { }
		public AliasList(string name, bool publish) : this(name, null, publish) { }
		public AliasList(string name, AliasList parent) : this(name, parent, true) { }
		public AliasList(string name, AliasList parent, bool publish) : this(name, parent, false, publish) { }
		public AliasList(string name, AliasList parent, bool falldown, bool publish) {
			this.Name = name;
			this.Parent = parent;
			this.falldown = falldown;
			this.isPublished = publish;

			KeyName = "AliasList:" + Name;
			if (publish) {
				backup = CallContext.GetData(KeyName);
				CallContext.SetData(KeyName, this);
			}
		}

		public void Dispose() {
			_disposed = true;
			if (! isPublished) return;
			if (backup != null)
				CallContext.SetData(KeyName, backup);
			else 
				CallContext.FreeNamedDataSlot(KeyName);
		}

		public static AliasList Current( string name ) {
			string KeyName = "AliasList:" + name;
			AliasList a = CallContext.GetData(KeyName) as AliasList;
			return a;
		}

		/// <remarks>¬ некоторых RDBMS могут быть ограничени€ на длинну имени, по этому естественные имена
		/// не всегда могут быть использованы и нужно вводить дополнительные короткие синонимы.</remarks>
		public virtual string GetNewShortAlias(string name, string short_alias) {
			int i =0;
			string nn = short_alias;
			while( this[nn] != null )
				nn = short_alias + Separator + (i++).ToString();
			return nn;
		}

		public virtual string NewShortAlias(string name) {
			return NewShortAlias(name, name);
		}

		public virtual string NewShortAlias(string name, string short_alias) {
			return NewShortAlias(new Name(name), short_alias);
		}

		public virtual string NewShortAlias(Name name, string short_alias) {
			string res;
			lock (d) {
				res = GetNewShortAlias(name.OwnAlias, short_alias);
				this[res] = name;
			}
			return res;
		}

		/// <remarks>–азименование алиаса.</remarks>
		public virtual Name this[ string alias ] {
			get { 
				Pair res = d[alias] as Pair; 
				if (res != null) return res.N;
				if (Parent != null) return Parent[alias];
				return null;
			}
			set { 
				if (Parent != null && falldown)
					Parent[alias] = value;
				else {
					Pair p = d[alias] as Pair;
					if (p == null)
						d[alias] = new Pair(value, new Name(alias));
					else
						 p.N = value; 
				}
			}
		}

		public virtual string ChangeAlias( string alias, string new_alias ) {
			if (Parent != null && falldown) 
				return Parent.ChangeAlias(alias, new_alias);
			else
				lock(d) {
					Pair p = d[alias] as Pair;
					new_alias = GetNewShortAlias(new_alias, new_alias);
					if (p != null) {
						d.Remove(alias);
						p.A.Alias = new_alias;
					} else 
						p = new Pair(new Name(alias), new Name(new_alias));
					d[new_alias] = p;
					return new_alias;
				}
		}

		/// <remarks>–азименование алиаса.</remarks>
		public virtual void SetAlias( string alias, string value ) {
			if (Parent != null && falldown) 
				Parent.SetAlias(alias, value);
			else {
				Pair p = d[alias] as Pair;
				if (p == null)
					d[alias] = new Pair( value, alias );
				else
					p.N.Alias = value;
			}
		}

		public virtual void SetAlias( Name alias, Name value) {
			if (Parent != null && falldown)
				Parent.SetAlias(alias, value);
			else {
				Pair p = d[alias.Alias] as Pair;
				if (p == null)
					d[alias.Alias] = new Pair(value, alias);
				else {
					p.N = value;
					p.A = alias;
				}
			}
		}

		/// <remarks>¬озвращает алиас дл€ данного имени.</remarks>
		public virtual Name GetAlias(string name) {
			foreach (Pair p in d.Values)
				if (p.N.OwnAlias == name) return p.A;
			if (Parent != null) return Parent.GetAlias(name);
			return null;
		}
		// ищет алиас дл€ заданного имени, если не находит то создает его с указанным short_alias
		public virtual Name GetAlias(string name, string short_alias) {			
			Name res = null;
			foreach (Pair p in d.Values)
				if (p.N.OwnAlias == name) return p.A;
			if (Parent != null) res = Parent.GetAlias(name, short_alias);
			if (res == null) res = NewShortAlias(name, short_alias);
			return res;
		}

		public virtual Name GetName(string alias) {
			Pair p = d[alias] as Pair;
			if (p != null) return p.N;
			if (Parent != null) return Parent.GetName(alias);
			return null;
		}
		public virtual Name GetAliasName(string alias) {
			Pair p = d[alias] as Pair;
			if (p != null) return p.A;
			if (Parent != null) return Parent.GetAliasName(alias);
			return null;
		}

		public virtual IList Aliases { get { return new ArrayList(d.Keys); } }
		public virtual IList Names { 
			get { 
				ArrayList a = new ArrayList();
				foreach (Pair n in d.Values) a.Add(n.N.OwnAlias);
				return a; 
			} 
		}

		/// <summary>ќбъедина€ет списки имен. ƒл€ тех имен. ѕри совпвдении имен и различии сокращений,
		/// приоритет отдаетс€ собственным сокращени€м. </summary>
		public virtual IList MergeByNames(AliasList al) {
			ArrayList res = new ArrayList();
			lock (this) {
				IList c = Names;
				foreach (string s in al.Names) {
					Name his_alias = al.GetAlias(s);
					if (c.Contains(s)) {
						Name my_alias = GetAlias(s);
						// TODO: Ёто убивает переданый al насмерть!
						his_alias.Alias = my_alias.Alias;
					} else {
						Name his_name = al.GetName(his_alias.Alias);
						if (his_name != null) {
							string new_alias = GetNewShortAlias(his_name.OwnAlias, his_alias.Alias);
							his_alias.Alias = new_alias;
							d[new_alias] = new Pair(his_name, his_alias);
							res.Add(his_alias);
						}
					}
				}
			}
			return res;
		}
	}
}
