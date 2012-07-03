using System.IO;
using System.Text;

namespace Front {

	public class FileUtils {

		public static string EvaluateRelativePath(string mainDirPath, string absoluteFilePath) {
			string first = mainDirPath.ToUpper().Substring(0, 2);
			if (first == "\\\\" || first != absoluteFilePath.ToUpper().Substring(0, 2))
				return absoluteFilePath;
			if (mainDirPath[mainDirPath.Length - 1] != '\\')
				mainDirPath += "\\";
			StringBuilder sb = new StringBuilder();
			while (mainDirPath.Length > 0) {
				if (!absoluteFilePath.ToUpper().StartsWith(mainDirPath.ToUpper())) {
					sb.Append("..\\");
					int i = mainDirPath.LastIndexOf("\\");
					if (i == mainDirPath.Length - 1) {
						mainDirPath = mainDirPath.Remove(i, 1);
						i = mainDirPath.LastIndexOf("\\");
					}
					mainDirPath = mainDirPath.Remove(i + 1, mainDirPath.Length - i - 1);
				}
				else {
					absoluteFilePath = absoluteFilePath.Remove(0, mainDirPath.Length);
					mainDirPath = "";
				}
			}

			sb.Append(absoluteFilePath);
			return sb.ToString();
		}

		public static string EvaluateAbsolutePath(string mainDirPath, string relativeFilePath) {
			return Path.GetFullPath(Path.Combine(mainDirPath, relativeFilePath));
		}
	}
}