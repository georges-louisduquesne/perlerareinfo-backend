using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApiPerleRare.Models;

namespace ApiPerleRare.ScheduledTasks;

/// <summary>
/// Attribution d’un formulaire candidat : fiche Candidat, évènement MAIL IN, alerte A appeler.
/// La fiche n’est ni admin, ni négociateur, ni disponible (y compris mission).
/// Les recruteurs du 2e tableau excluent le statut Candidat.
/// </summary>
internal static class FormulaireCandidatAttribution
{
	/// <summary>
	/// CP_Dispo et CP_DispoMission valent 1 par défaut en base. EF n’écrit pas le 0
	/// (valeur par défaut du type) et MariaDB remet « Dispo ». Cet UPDATE force les quatre interrupteurs.
	/// </summary>
	internal const string PersistOffFlagsSql = "UPDATE conseillers_personnels SET CP_Admin = 0, CP_Negociateur = 0, CP_Dispo = 0, CP_DispoMission = 0 WHERE CP_RefConseiller = {0}";

	internal static void MarkNotStaff(ConseillersPersonnels row)
	{
		if (row == null)
		{
			return;
		}
		row.CpAdmin = false;
		row.CpNegociateur = false;
		row.CpDispo = 0;
		row.CpDispoMission = false;
	}

	internal const string EventType = "MAIL IN";

	internal const string EventComment = "formulaire réseaux sociaux";

	internal const string TaskType = "A appeler";

	internal const string TaskComment = "CANDIDAT";

	internal const string ConseillerOrigine = "LinkedIn";

	internal static readonly DateOnly EmptyDate = new DateOnly(1970, 1, 1);

	internal static bool IsRecruteur(sbyte? actif, bool admin, string statut, string prenom, string nom, string login)
	{
		if (actif != 1 || !admin)
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(login))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(prenom) && string.IsNullOrWhiteSpace(nom))
		{
			return false;
		}
		return !string.Equals((statut ?? "").Trim(), "Candidat", StringComparison.OrdinalIgnoreCase);
	}

	internal static string Commentaire(string linkedin, string pourquoi)
	{
		string link = (linkedin ?? "").Trim();
		string text = (pourquoi ?? "").Trim();
		if (link.Length == 0)
		{
			return text;
		}
		if (text.Length == 0)
		{
			return link;
		}
		return link + "\n" + text;
	}

	internal static string Ville(string habitation)
	{
		string text = (habitation ?? "").Trim();
		return text.Length <= 30 ? text : text.Substring(0, 30);
	}

	internal static ConseillersPersonnels CreateConseiller(FormulairesCandidats form, DateOnly today)
	{
		string phone = TypeformPartialLeadMapper.NormalizePhone(form.FcTel) ?? "";
		if (phone.Length > 30)
		{
			phone = phone.Substring(0, 30);
		}
		string email = (form.FcEmail ?? "").Trim();
		if (email.Length > 100)
		{
			email = email.Substring(0, 100);
		}
		return new ConseillersPersonnels
		{
			CpPrenom = Cut(Latin1(form.FcPrenom), 50),
			CpNomFamille = Cut(Latin1(form.FcNom), 50),
			CpTelPersonnel = phone,
			CpMel = "",
			CpMelPerso = email,
			CpCommentaire = Cut(Latin1(Commentaire(form.FcLinkedin, form.FcPourquoi)), 2000),
			CpStatut = "Candidat",
			CpOrigine = ConseillerOrigine,
			CpActif = 1,
			CpAdmin = false,
			CpDispo = 0,
			CpDispoMission = false,
			CpNegociateur = false,
			CpEnSociete = false,
			CpTva = false,
			CpSiret = 0,
			CpMandat = "",
			CpAdresse = "",
			CpVille = Ville(Latin1(form.FcVille)),
			CpPieceIdentite = "",
			CpPhoto = "",
			CpPhotoSignature = "",
			CpRsac = "",
			CpDateDebut = EmptyDate,
			CpDateFin = EmptyDate,
			CpDateCreation = today
		};
	}

	internal static ConseillersPersonnelsEvenements CreateEvent(FormulairesCandidats form, uint conseillerRef)
	{
		return new ConseillersPersonnelsEvenements
		{
			CpeTypeEvenement = EventType,
			CpeRefConseiller = conseillerRef,
			CpeDateCreation = DateOnly.FromDateTime(form.FcDate),
			CpeDateRealisation = form.FcDate,
			CpeCommentaire = EventComment,
			CpeStatut = 1
		};
	}

	internal static ConseillersPersonnelsTaches CreateTask(uint conseillerRef, string adminLogin, DateTime now)
	{
		return new ConseillersPersonnelsTaches
		{
			CptRefConseiller = conseillerRef,
			CptDateCreation = DateOnly.FromDateTime(now),
			CptDateRealisation = CallDate(now).ToDateTime(TimeOnly.MinValue),
			CptType = TaskType,
			CptQui = Cut(adminLogin, 100),
			CptCom = TaskComment,
			CptEtat = "",
			CptLien = ""
		};
	}

	/// <summary>Même règle que l’attribution d’un formulaire prospect : après 17 h, week-end ou férié → jour ouvré suivant.</summary>
	internal static DateOnly CallDate(DateTime now)
	{
		DateOnly day = DateOnly.FromDateTime(now);
		if (now.Hour >= 17 || IsClosed(day))
		{
			do
			{
				day = day.AddDays(1);
			}
			while (IsClosed(day));
		}
		return day;
	}

	internal static bool IsClosed(DateOnly day)
	{
		if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
		{
			return true;
		}
		foreach (DateOnly holiday in Holidays(day.Year))
		{
			if (holiday == day)
			{
				return true;
			}
		}
		return false;
	}

	internal static DateOnly Easter(int year)
	{
		int g = year % 19;
		int c = year / 100;
		int h = (c - c / 4 - (8 * c + 13) / 25 + 19 * g + 15) % 30;
		int i = h - h / 28 * (1 - h / 28 * (29 / (h + 1)) * ((21 - g) / 11));
		int j = (year + year / 4 + i + 2 - c + c / 4) % 7;
		int l = i - j;
		int month = 3 + (l + 40) / 44;
		int jour = l + 28 - 31 * (month / 4);
		return new DateOnly(year, month, jour);
	}

	private static IEnumerable<DateOnly> Holidays(int year)
	{
		DateOnly easter = Easter(year);
		return new DateOnly[13]
		{
			new DateOnly(year, 1, 1),
			easter,
			easter.AddDays(1),
			new DateOnly(year, 5, 1),
			new DateOnly(year, 5, 8),
			easter.AddDays(39),
			easter.AddDays(49),
			easter.AddDays(50),
			new DateOnly(year, 7, 14),
			new DateOnly(year, 8, 15),
			new DateOnly(year, 11, 1),
			new DateOnly(year, 11, 11),
			new DateOnly(year, 12, 25)
		};
	}

	private static readonly Encoding Latin1Encoding = Encoding.GetEncoding(28591);

	/// <summary>conseillers_personnels est en latin1 : apostrophes typographiques et le reste hors page.</summary>
	private static string Latin1(string value)
	{
		string text = value ?? "";
		text = text.Replace('\u2019', '\'').Replace('\u2018', '\'').Replace('\u201C', '"').Replace('\u201D', '"').Replace('\u2013', '-').Replace('\u2014', '-').Replace("\u2026", "...");
		return Latin1Encoding.GetString(Latin1Encoding.GetBytes(text));
	}

	private static string Cut(string value, int max)
	{
		string text = value ?? "";
		return text.Length <= max ? text : text.Substring(0, max);
	}
}

internal sealed class PersonSnapshot
{
	public uint Ref { get; set; }

	public string Kind { get; set; }

	public string Prenom { get; set; }

	public string Nom { get; set; }

	public string Statut { get; set; }

	public DateTime? Created { get; set; }

	public string[] Emails { get; set; } = Array.Empty<string>();

	public string[] Tels { get; set; } = Array.Empty<string>();
}

internal static class FormulaireCandidatMatches
{
	internal static FormulaireCandidatCorrespondance[] Match(string nom, string email, string tel, IEnumerable<PersonSnapshot> people)
	{
		string nomKey = (nom ?? "").Trim();
		string emailKey = (email ?? "").Trim();
		string telKey = TypeformPartialLeadMapper.NormalizePhone(tel) ?? "";
		var list = new List<FormulaireCandidatCorrespondance>();
		foreach (PersonSnapshot person in people ?? Array.Empty<PersonSnapshot>())
		{
			bool nomHit = nomKey.Length > 0 && string.Equals(nomKey, (person.Nom ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
			bool emailHit = emailKey.Length > 0 && (person.Emails ?? Array.Empty<string>()).Any((string value) => string.Equals(emailKey, (value ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
			bool telHit = telKey.Length > 0 && (person.Tels ?? Array.Empty<string>()).Any((string value) => telKey == (TypeformPartialLeadMapper.NormalizePhone(value) ?? ""));
			if (!nomHit && !emailHit && !telHit)
			{
				continue;
			}
			var row = new FormulaireCandidatCorrespondance
			{
				Type = person.Statut,
				Nom = ((person.Nom ?? "") + " " + (person.Prenom ?? "")).Trim(),
				Prenom = person.Prenom,
				NomFamille = person.Nom,
				DateCreation = person.Created,
				NomIdentique = nomHit,
				EmailIdentique = emailHit,
				TelIdentique = telHit
			};
			if (person.Kind == "contact")
			{
				row.CRefContact = person.Ref;
			}
			else
			{
				row.CRefConseiller = person.Ref;
			}
			list.Add(row);
		}
		return list.OrderBy((FormulaireCandidatCorrespondance row) => row.Nom).ToArray();
	}
}
