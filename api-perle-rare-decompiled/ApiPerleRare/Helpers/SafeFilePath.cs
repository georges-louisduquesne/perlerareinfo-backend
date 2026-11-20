using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

	/// <summary>
	/// Indique si un nom de fichier absent à l'identique peut correspondre à un écart NFC/NFD
	/// ou à un caractère hors latin1 remplacé par '?' en base MariaDB.
	/// </summary>
	public static bool NeedsExistingFallback(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
		{
			return false;
		}
		for (int i = 0; i < fileName.Length; i++)
		{
			char c = fileName[i];
			if (c == '?' || c > '\u007f')
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Retrouve un fichier existant dans le même dossier malgré un écart NFC/NFD ou un '?'
	/// issu d'une colonne MariaDB latin1 (ex. marque combinante NFD macOS). Ne matche jamais
	/// '?' contre un caractère ASCII et refuse toute ambiguïté.
	/// </summary>
	public static bool TryMatchExistingFileName(string requestedFileName, IEnumerable<string> siblingFileNames, out string matchedFileName)
	{
		matchedFileName = null;
		if (string.IsNullOrEmpty(requestedFileName) || siblingFileNames == null)
		{
			return false;
		}

		string requestedNfc = requestedFileName.Normalize(NormalizationForm.FormC);
		bool hasQuestionMark = requestedFileName.IndexOf('?') >= 0;

		string nfcMatch = null;
		int nfcCount = 0;
		string nonLatin1Match = null;
		int nonLatin1Count = 0;
		string nonAsciiMatch = null;
		int nonAsciiCount = 0;

		foreach (string candidate in siblingFileNames)
		{
			if (string.IsNullOrEmpty(candidate))
			{
				continue;
			}
			if (string.Equals(candidate, requestedFileName, StringComparison.Ordinal))
			{
				matchedFileName = candidate;
				return true;
			}
			if (string.Equals(candidate.Normalize(NormalizationForm.FormC), requestedNfc, StringComparison.Ordinal))
			{
				nfcMatch = candidate;
				nfcCount++;
				continue;
			}
			if (!hasQuestionMark)
			{
				continue;
			}
			if (MatchesLossyQuestionMark(requestedFileName, candidate, minCodePoint: '\u0100'))
			{
				nonLatin1Match = candidate;
				nonLatin1Count++;
			}
			else if (MatchesLossyQuestionMark(requestedFileName, candidate, minCodePoint: '\u0080'))
			{
				nonAsciiMatch = candidate;
				nonAsciiCount++;
			}
		}

		if (nfcCount == 1)
		{
			matchedFileName = nfcMatch;
			return true;
		}
		if (nfcCount > 1)
		{
			return false;
		}
		if (nonLatin1Count == 1)
		{
			matchedFileName = nonLatin1Match;
			return true;
		}
		if (nonLatin1Count > 1)
		{
			return false;
		}
		if (nonAsciiCount == 1)
		{
			matchedFileName = nonAsciiMatch;
			return true;
		}
		return false;
	}

	private static bool MatchesLossyQuestionMark(string requested, string candidate, char minCodePoint)
	{
		if (requested.Length != candidate.Length)
		{
			return false;
		}
		bool matchedQuestion = false;
		for (int i = 0; i < requested.Length; i++)
		{
			char r = requested[i];
			char c = candidate[i];
			if (r == '?')
			{
				if (c < minCodePoint)
				{
					return false;
				}
				matchedQuestion = true;
			}
			else if (r != c)
			{
				return false;
			}
		}
		return matchedQuestion;
	}
}
