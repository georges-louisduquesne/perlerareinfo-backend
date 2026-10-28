using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using ApiPerleRare.Models;

namespace ApiPerleRare.ScheduledTasks;

/// <summary>
/// Mail « Nouveau formulaire de recrutement via réseaux sociaux » reçu sur contact@.
/// Les libellés sont ceux du corps du message.
/// </summary>
internal static class FormulaireCandidatMail
{
	internal const string SubjectMarker = "Nouveau formulaire de recrutement via réseaux sociaux";

	internal const short StatutAAttribuer = 1;

	internal const short StatutAttribue = 2;

	internal const short StatutRejete = 4;

	internal static readonly string[] CausesRejet = new string[3] { "Doublon", "Spam", "Autre" };

	private static readonly string[] Labels = new string[8]
	{
		"Votre prénom",
		"Votre nom",
		"Votre numtel",
		"Votre email",
		"Où habitez-vous",
		"Lien linkedIn",
		"Pourquoi rejoindre PR",
		"Délai de remplissage du formulaire"
	};

	internal static bool IsRecruitmentSubject(string subject)
	{
		if (string.IsNullOrWhiteSpace(subject))
		{
			return false;
		}
		string trimmed = subject.Trim();
		if (trimmed.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
			|| trimmed.StartsWith("TR:", StringComparison.OrdinalIgnoreCase)
			|| trimmed.StartsWith("Fwd:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return trimmed.IndexOf(SubjectMarker, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	internal static string ToPlain(string body)
	{
		if (string.IsNullOrEmpty(body))
		{
			return "";
		}
		if (body.IndexOf('<') < 0)
		{
			return body;
		}
		string text = Regex.Replace(body, "(?i)<br\\s*/?>", "\n");
		text = Regex.Replace(text, "(?i)</p>", "\n");
		text = Regex.Replace(text, "<[^>]+>", " ");
		return WebUtility.HtmlDecode(text);
	}

	internal static FormulairesCandidats Parse(string messageId, DateTime receivedParis, string from, string subject, string body)
	{
		string plain = ToPlain(body ?? "").Replace('\r', '\n');
		var hits = new List<(int Index, string Label)>();
		foreach (string label in Labels)
		{
			int at = plain.IndexOf(label, StringComparison.OrdinalIgnoreCase);
			if (at >= 0)
			{
				hits.Add((at, label));
			}
		}
		if (hits.Count == 0)
		{
			return null;
		}
		hits.Sort((a, b) => a.Index.CompareTo(b.Index));
		var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < hits.Count; i++)
		{
			int start = hits[i].Index + hits[i].Label.Length;
			int end = i + 1 < hits.Count ? hits[i + 1].Index : plain.Length;
			if (end < start)
			{
				end = start;
			}
			string chunk = plain.Substring(start, end - start).Trim();
			if (chunk.StartsWith("?") || chunk.StartsWith(":"))
			{
				chunk = chunk.Substring(1).Trim();
			}
			values[hits[i].Label] = Regex.Replace(chunk, "[ \\t]+", " ").Trim();
		}
		string prenom = Value(values, "Votre prénom");
		string nom = Value(values, "Votre nom");
		if (prenom.Length == 0 && nom.Length == 0)
		{
			return null;
		}
		return new FormulairesCandidats
		{
			FcMessageId = Cut(messageId, 255),
			FcDate = DateTime.SpecifyKind(receivedParis, DateTimeKind.Unspecified),
			FcFrom = Cut(from, 255),
			FcSubject = Cut(subject, 255),
			FcPrenom = Cut(prenom, 80),
			FcNom = Cut(nom, 80),
			FcTel = Cut(Value(values, "Votre numtel"), 40),
			FcEmail = Cut(Value(values, "Votre email"), 150),
			FcVille = Cut(Value(values, "Où habitez-vous"), 150),
			FcLinkedin = Cut(Value(values, "Lien linkedIn"), 300),
			FcPourquoi = Value(values, "Pourquoi rejoindre PR"),
			FcDelai = Cut(Value(values, "Délai de remplissage du formulaire"), 20),
			FcCorps = plain.Trim(),
			FcStatut = StatutAAttribuer
		};
	}

	internal static bool IsCauseRejet(string cause)
	{
		if (string.IsNullOrWhiteSpace(cause))
		{
			return false;
		}
		foreach (string known in CausesRejet)
		{
			if (string.Equals(known, cause.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	internal static string CanonicalCause(string cause)
	{
		foreach (string known in CausesRejet)
		{
			if (string.Equals(known, cause.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				return known;
			}
		}
		return null;
	}

	private static string Value(Dictionary<string, string> values, string label)
	{
		return values.TryGetValue(label, out string value) ? (value ?? "") : "";
	}

	private static string Cut(string value, int max)
	{
		string text = (value ?? "").Trim();
		return text.Length <= max ? text : text.Substring(0, max);
	}
}
