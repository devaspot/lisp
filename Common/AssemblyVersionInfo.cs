using System;
using System.Reflection;

namespace Front {

	/// <summary>
	/// Provides information about assembly version.
	/// </summary>
	public class AssemblyVersionInfo {
		#region Fields

		protected string InnerCompanyName;
		protected string InnerProductName;
		protected string InnerProductDescription;
		protected string InnerProductVersion;
		protected string InnerProductCopyright;

		#endregion

		#region Methods

		protected AssemblyVersionInfo(string path, int fieldCount) {
			Assembly assembly = Assembly.ReflectionOnlyLoadFrom(path);

			foreach (CustomAttributeData customAttributeData in CustomAttributeData.GetCustomAttributes(assembly)) {
				if (customAttributeData.Constructor.DeclaringType == typeof(AssemblyCompanyAttribute)) {
					this.InnerCompanyName = customAttributeData.ConstructorArguments[0].Value as string;
				} else if (customAttributeData.Constructor.DeclaringType == typeof(AssemblyProductAttribute)) {
					this.InnerProductName = customAttributeData.ConstructorArguments[0].Value as string;
				} else if (customAttributeData.Constructor.DeclaringType == typeof(AssemblyDescriptionAttribute)) {
					this.InnerProductDescription = customAttributeData.ConstructorArguments[0].Value as string;
				} else if (customAttributeData.Constructor.DeclaringType == typeof(AssemblyCopyrightAttribute)) {
					this.InnerProductCopyright = customAttributeData.ConstructorArguments[0].Value as string;
				}
			}

			this.InnerProductVersion = assembly.GetName().Version.ToString(fieldCount);
		}

		/// <summary>
		/// Returns assembly version information for an assembly with given path.
		/// </summary>
		/// <param name="path">Path to assembly file.</param>
		/// <param name="fieldCount">Number of fields in version string. Must be from 1 to 4.</param>
		/// <returns>AssemblyVersionInfo</returns>
		public static AssemblyVersionInfo GetVersionInfo(string path, int fieldCount) {
			return new AssemblyVersionInfo(path, fieldCount);
		}

		/// <summary>
		///  Returns assembly version information for an assembly with given path.
		/// </summary>
		/// <param name="path">Path to assembly file.</param>
		/// <returns></returns>
		public static AssemblyVersionInfo GetVersionInfo(string path) {
			return new AssemblyVersionInfo(path, 4);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Company name.
		/// </summary>
		public string CompanyName {
			get { return this.InnerCompanyName; }
		}

		/// <summary>
		/// Product name.
		/// </summary>
		public string ProductName {
			get { return this.InnerProductName; }
		}

		/// <summary>
		/// Product description.
		/// </summary>
		public string ProductDescription {
			get { return this.InnerProductDescription; }
		}

		/// <summary>
		/// Product version.
		/// </summary>
		public string ProductVersion {
			get { return this.InnerProductVersion; }
		}

		/// <summary>
		/// Product copyright.
		/// </summary>
		public string ProductCopyright {
			get { return this.InnerProductCopyright; }
		}

		#endregion
	}
}
