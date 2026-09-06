using System;
using System.IO;

namespace ApiPerleRare.Helpers;

internal static class SafeFilePath
{
	private static readonly string[] DeniedExtensions = { ".php", ".phtml", ".exe", ".dll", ".config", ".bat", ".cmd", ".sh" };

	public static bool TryResolve(string root, string relative, out string fullPath, out string error)
	{
		fullPath = null;
		error = null;
		if (string.IsNullOrWhiteSpace(root))
		{
			error = "Dossier racine non spécifié";
			return false;
		}
		if (string.IsNullOrWhiteSpace(relative) || relative.IndexOf('\0') >= 0)
		{
			error = "Nom de fichier invalide";
			return false;
		}
		relative = relative.Replace('\\', '/').TrimStart('/');
		if (!relative.Contains('/'))
		{
			error = "Il faut obligatoirement spécifier un dossier";
			return false;
		}
		string ext = Path.GetExtension(relative).ToLowerInvariant();
		if (Array.IndexOf(DeniedExtensions, ext) >= 0)
		{
			error = "Extension interdite";
			return false;
		}
		string rootFull = Path.GetFullPath(root);
		if (!rootFull.EndsWith(Path.DirectorySeparatorChar.ToString()))
		{
			rootFull += Path.DirectorySeparatorChar;
		}
		fullPath = Path.GetFullPath(Path.Combine(rootFull, relative));
		if (!fullPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
		{
			error = "Chemin interdit";
			fullPath = null;
			return false;
		}
		return true;
	}
}
