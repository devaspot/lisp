// $Id$

using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;

namespace Front {

	/// <summary> описание функциональной особенности</summary>
	// TODO DF0100: нужно бы сделать их нечувствительными к регистру.
	/*[Serializable]
	public struct Feature : INamed {
		public static readonly Feature Nothing = new Feature("nothing", Legibility.Indifferent);
		public static readonly Feature RunAtServer = new Feature("runat:server", Legibility.Strict);
		public static readonly Feature RunAtClient = new Feature("runat:client", Legibility.Strict);
		public static readonly Feature DbAccess = new Feature("dbaccess", Legibility.Strict);

		private string _description;
		private Legibility _legibility;
		private Name _name;

		public string Name			 { get { return _name.OwnAlias; } set { _name.OwnAlias = value; } }
		public Name FullName		 { get { return _name; } }
		public string Description	 { get { return _description; } set { _description = value; } }
		public Legibility Legibility { get { return _legibility; } set { _legibility = value; } }

		public Feature(string name, Legibility legibility, string description) {
			_name = new Name(name);
			_description = description;
			_legibility = legibility;
		}
		public Feature(string name, string description) : this (name, Legibility.Strict, description) {}
		public Feature(string name, Legibility legibility) : this (name, legibility, null) {}
		public Feature(string name) : this (name, Legibility.Strict, null) {}

		// проверяет соответствие декларации требованию (передаваемое значение) 
		public Legibility Match(string f) { return this.Match(f, Legibility.Strict); }
		public Legibility Match( Feature f) { return this.Match(f.Name, f.Legibility); }
		public Legibility Match(string f, Legibility l) {
		//			req
		//		 S O I D T        
		//	   +-----------
		//D	 S | S S I D T    2  2  0 -1 -2
		//e	 O | O O I D D    1  1  0 -1 -1
		//f	 I | I I I I I    0  0  0  0  0
		//	 D | D D I O O   -1 -1  0  1  1
		//	 T | T D I S S   -2 -1  0  2  2
  
		// (таблица совместимости)
			
			if (l == Legibility.Indifferent || f != this.Name) 
				return Legibility.Indifferent;

			switch (l) {
				case Legibility.Strict: 
						return this.Legibility;
				case Legibility.Optional: 
						return (this.Legibility == Legibility.Taboo)
							? Legibility.Deprecated : this.Legibility;
				case Legibility.Deprecated:
						return (this.Legibility == Legibility.Strict)
							? Legibility.Deprecated : (Legibility)(- (int)this.Legibility );
				case Legibility.Taboo:
						return (Legibility)(- (int)this.Legibility );
			}
			// Сюда попадать не должн!
			return Legibility.Indifferent;
		}

	}



	// степень соответствия(требования/предоставления) особенности
	// операции со степенью соответствия очень похожи на нечеткую логику :-)
	public enum Legibility {
		Strict = 2,			// строгое
		Optional = 1,		// необязательное
		Indifferent = 0,	// безразличное
		Deprecated = -1,	// не желательное
		Taboo = -2			// не совместимое
	}



	///<summary> список функциональный особенностей.</summary>
	[Serializable]
	public class FeatureList: NamedValueCollection< Feature > {

		public FeatureList(IEnumerable lst) {}
		public FeatureList(params object[] lst) { }

		// XXX позволительно ли иметь несколько фичь с одним ключем но разным соответствием?
		// пока вызодим из предположения, что не позволительно.
		public virtual Feature this[ string name ] {
			get {
				if (name != Feature.Nothing.Name) 
					foreach (Feature f in this) 
						if (f.Name == name) return f;
				return Feature.Nothing;
			}				
		}
		
		// проверяет соответствие декларации требованию (передаваемое значение) 
		public virtual Legibility Match( string f, Legibility l ) {
			// (безразличность - эквивалент отсутствию требования)
			if (l == Legibility.Indifferent) return Legibility.Indifferent;
			return this[f].Match( f, l );
		}
		public virtual Legibility Match( Feature f ) { 
			return this.Match(f.Name, f.Legibility); 
		}
		public virtual Legibility Match( string f ) { 
			return this.Match(f, Legibility.Strict );	
		}
		public virtual bool Match( ICollection<Feature> f, Legibility l) {
			return FeatureList.Match(this, f, l); 
		}
		public virtual bool Match(ICollection<Feature> f) { 
			return FeatureList.Match(this, f, Legibility.Strict); 
		}

		// Проверяет соответствие списка определений списку требования
		// ( l - лояльность требования. )
		public static bool Match( ICollection<Feature> req, ICollection<Feature> def , Legibility l) {
			if (l == Legibility.Indifferent) return true;
			FeatureList def1 = (def is FeatureList) ? (FeatureList)def : new FeatureList(def);
			
			int l1 = (int)l;
			foreach (Feature f in req) {
				Legibility m = def1.Match(f);

				if ( (l1 > 0 && (int)m < l1) || 
					 (l1 < 0 && (int)m > l1)) return false;
			}
			return true;
		}
	}



	/// <summary>Базовый аттрибут о списком <see cref="Feature">Фичь</see>.</summary>
	public abstract class FeatureListAttribute: Attribute {
		private FeatureList _features;

		// TODO: тут нужно еще подровнять
		public FeatureList Features { get { return _features; } }

		public FeatureListAttribute(IEnumerable lst) {
			this._features = (lst is FeatureList) ? (FeatureList)lst : new FeatureList(lst);
		}
		public FeatureListAttribute(string f, params string[] lst) : this (new FeatureList(f, lst)) { }
		public FeatureListAttribute(Feature f, params Feature[] lst) : this(new FeatureList(f, lst)) { }

	}



	
	public class ReqAttribute : FeatureListAttribute {
		public ReqAttribute(IEnumerable lst) :base(lst) { }
		public ReqAttribute(params object[] lst) : base (new FeatureList(lst)) { }
	}



	
	public class DefAttribute : FeatureListAttribute {
		public DefAttribute(IEnumerable lst) :base(lst) { }
		public DefAttribute(params object[] lst) : base (new FeatureList(lst)) { }
	}
*/

}
