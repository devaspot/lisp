// $Id: ACL.cs 128 2006-04-06 11:55:30Z pilya $


using System;
using System.Collections;
using System.Collections.Specialized;

namespace Front.Security {

	/// <summary>Когда класс <see cref="Acl"/> запоминает список разрешений для каждого конкретного права,
	/// он использует значения <see cref="Permission"/> для указания того какое именно значение имеет право
	/// для конкретного SID-а.</summary>
	/// <remarks>Метод вычисления наличия указанного права у коллекции SID-ов основан на 2-х правилах:
	/// <para>1. Что не разрешено - то запрещено.</para>
	/// <para>2. Запрет имеет преимущество над разрешением.</para>
	/// <para>Это означает что если в эффективной коллекции SID-ов пользователя или группы нет такого SID-а,
	/// для которого в <see cref="Acl"/> как-то определено право определенное право, то в результате считается, что
	/// этот пользователь или группа такого права не имеет. Если же в <see cref="Acl"/> есть SID-ы, соответствующие
	/// коллекции, то право будет разрешено только в том случае, если среди зарегистрированных
	/// значений будет один или больше <see cref="Permission.Allow"/> и не будет <see cref="Permission.Deny"/>.</para>
	/// </remarks>
	[Serializable]
	public enum Permission {
		///<summary>Значение для данного SID-а не указанно.</summary>
		Unspecified = 30,
		///<summary>Право разрешено.</summary>
		Allow = 20,
		///<summary>Право запрещено.</summary>
		Deny = 10
	}
	
	/// <summary>Список прав доступа.</summary>
	/// <remarks>Этот класс не определяет списка возможных прав. Вместо этого права задаются строковыми
	/// идентификаторами, что дает возможность пользователю класса самому определить возможные типы доступа.
	/// </remarks>
	[Serializable]
	public class Acl : ICloneable {
		HybridDictionary rights;

		/// <summary>Создать новый пустой список прав доступа.</summary>
		public Acl():this(new HybridDictionary()) { }

		protected Acl(HybridDictionary hd) {
			rights = hd;
		}

		object ICloneable.Clone() {
			return this.Clone();
		}

		public virtual Acl Clone() {
			HybridDictionary hd = new HybridDictionary(rights.Count);
			foreach (DictionaryEntry de in rights)
				if (de.Value != null) hd.Add(de.Key, ((ArrayList)de.Value).Clone());
			return new Acl(hd);
		}

		/// <summary>Получить значение, указывающее имеет ли данная коллекция SID-ов указанное право.</summary>
		/// <value>true, если у указанной коллекции SID-ов есть данное право; иначе false.</value>
		/// <remarks>Метод вычисления наличия указанного права у коллекции SID-ов основан на 2-х правилах:
		/// <para>1. Что не разрешено - то запрещено.</para>
		/// <para>2. Запрет имеет преимущество над разрешением.</para>
		/// <para>Это означает что если в коллекции <paramref name="sids"/> нет такого SID-а, для которого
		/// в <see cref="Acl"/> как-то определено право <paramref name="accessType"/>, то в результате считается, что
		/// этот список SID-ов такого права не имеет. Если же в <see cref="Acl"/> есть SID-ы, соответствующие
		/// <paramref name="sids"/>, то право будет разрешено только в том случае, если среди зарегистрированных
		/// значений будет хотя бы один <see cref="Permission.Allow"/> и не будет <see cref="Permission.Deny"/>.</para>
		/// </remarks>
		/// <param name="accessType">Тип доступа, для которого следует проверить разрешения.</param>
		/// <param name="sids">Коллекция SID-ов, для которых будет расчитываться разрешение. Чаще всего, это
		/// SID пользователя и SID-ы всех групп, в которые пользователь входит.</param>
		public bool this[string accessType, ICollection sids] {
			get {
				if (accessType == null) throw new ArgumentNullException("accessType");
				if (sids == null) throw new ArgumentNullException("sids");
				ArrayList p = rights[accessType] as ArrayList;

				if (p != null) {
					ArrayList sidList = new ArrayList(sids);
					sidList.Sort();

					bool bAllow = false;
					bool bDeny = false;

					// scan permission items
					foreach(AcItem cp in p)
						if (sidList.BinarySearch(cp.Sid) >= 0) {
							if (cp.Permission == Permission.Deny) {
								bDeny = true; break;
							}
							if (cp.Permission == Permission.Allow)
								bAllow = true;
						}
					return bAllow && !bDeny;
				} else
					return false;
			}
		}

		/// <summary>Зарегистрировать для права <paramref name="accessType"/> и указанного SID-а
		/// разрешение <paramref name="permission"/>.</summary>
		/// <param name="sid">SID, для которого регистрируется право</param>
		/// <param name="accessType">Право (тип доступа)</param>
		/// <param name="permission">Значение.</param>
		/// <remarks>Если соответствующее право уже зарегистрированно, оно будет перезаписано.
		/// <para>Если значение <paramref name="permission"/> равно <see cref="Permission.Unspecified"/>, то регистрация
		/// удаляется.</para>
		/// </remarks>
		public void SetPermission(int sid, string accessType, Permission permission) {
			// get configured sids
			ArrayList list = rights[accessType] as ArrayList;
			if (list == null) list = new ArrayList();

			AcItem n = new AcItem(sid, permission);
			//
			// change occurrence of sid in list if exists
			int index = list.BinarySearch(n, new SidComparer());
			if (index >= 0) {
				if (permission == Permission.Unspecified)
					list.RemoveAt(index);				
				else {
					list[index] = n;
					list.Sort();
				}
			} else {
				list.Add(n);
				list.Sort();
			}
			// save to Acl for this access type
			rights[accessType] = list;
		}

		/// <summary>Получить спискок SID-ов, для которых зарегистрированы хоть какие-то права.</summary>
		/// <returns>Список SID-ов.</returns>
		public int[] GetConfiguredSids() {
			ArrayList temp = new ArrayList();
			foreach (ArrayList a in rights.Values)
				temp.AddRange(a);
			temp.Sort();
			ArrayList res = new ArrayList();
			AcItem prev = new AcItem(-1, Permission.Unspecified); // create stub permission
			foreach(AcItem item in temp) {
				if (prev.Sid != item.Sid)
					res.Add(item.Sid);
				prev = item;
			}
			return (int[])res.ToArray(typeof(int));
		}

		/// <summary>Получить список прав, которые зарегистрированы в <see cref="Acl"/>.</summary>
		/// <returns>Массив строк - список зарегистрированных прав.</returns>
		public string[] GetConfiguredRights() {
			string[] res = new string[rights.Count];
			rights.CopyTo(res, 0);
			return res;
		}
		
		/// <summary>Получить список SID-ов, для которых зарегистрировано конкретное право.</summary>
		/// <returns>Словарь <see cref="IDictionary"/>, который соделжит пары "SID"-"значение" для указанного права.</returns>
		/// <remarks>Если регистрируемое право для какого-то SID-а имеет значение <see cref="Permission.Unspecified"/>, то 
		/// этот метод может вернуть указанный SID со значением <see cref="Permission.Unspecified"/> или не
		/// возвращать его совсем.</remarks>
		/// <param name="right">Тип доступа (право).</param>
		public IDictionary GetSids(string right) {
			IDictionary res = new Hashtable();
			ArrayList a = (ArrayList)rights[right];
			if (a != null) {
				foreach (AcItem item in a)
					res[item.Sid] = item.Permission;
			}
			return res;
		}

		/// <summary>Получить список прав, которые зарегистрированы для конкретного SID-а.</summary>
		/// <returns>Словарь <see cref="IDictionary"/>, который соделжит пары "право"-"значение" для указанного SID-а.</returns>
		/// <remarks>Если регистрируемое право для какого-то SID-а имеет значение <see cref="Permission.Unspecified"/>, то 
		/// этот метод может вернуть указанное право со значением <see cref="Permission.Unspecified"/> или не
		/// возвращать его совсем.</remarks>
		/// <param name="sid">Security Identifier (SID).</param>
		public IDictionary GetRights(int sid) {
			IDictionary res = new ListDictionary();
			foreach (string key in rights.Keys) {
				ArrayList a = (ArrayList)rights[key];
				int index = -1;
				for (int ind=0; ind < a.Count; ind++)
					if (((AcItem)a[ind]).Sid == sid) {
						index = ind;
						break;
					}
				if (index < 0)
					res[key] = Permission.Unspecified;
				else 
					res[key] = ((AcItem)a[index]).Permission;
			}
			return res;			
		}

		[Serializable]
		internal class AcItem : IComparable {
			public readonly int			Sid;
			public readonly Permission	Permission;

			public AcItem(int sid, Permission p) {
				this.Sid = sid;
				this.Permission = p;
			}

			public int CompareTo(object obj) {
				AcItem other = (AcItem)obj;
				int res = this.Sid.CompareTo(other.Sid);
				if (res == 0)
					return this.Permission.CompareTo(other.Permission);
				else
					return res;
			}

			public override bool Equals(object obj) {
				if (obj is int) 
					return object.Equals(this.Sid, (int)obj);
				else
					return (this.CompareTo(obj) == 0);
			}

			public override int GetHashCode() {
				return Sid * (int)Permission;
			}
		}

		internal class SidComparer : IComparer {
			public int Compare(object x, object y) {
				AcItem a = (AcItem)x;
				AcItem b = (AcItem)y;
				return a.Sid.CompareTo(b.Sid);

			}
		}
	}
}

