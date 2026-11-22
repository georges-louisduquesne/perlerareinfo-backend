using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Application.Conseillers;

/// <summary>
/// Publie les données maîtresses d'un conseiller (conseillers_personnels) sur https://perle-rare.com :
/// prénom/nom, parcours FR/EN, numéro RSAC, e-mail, LinkedIn et fiche Google.
/// Le parcours et le RSAC vont sur la page dédiée (FR/EN).
/// Le prénom/nom, l'e-mail, LinkedIn et Google vont sur cette page et sur les cartes qui y renvoient.
/// La base WordPress est sur le même MariaDB. Un échec n'annule jamais l'enregistrement CRM.
/// </summary>
public sealed class PublicParcoursPublisher
{
	internal const string DatabaseName = "perle-rarecom-v2";

	internal const string PostsTable = "kdg_posts";

	private readonly ApplicationDbContext _context;

	private readonly ILogger<PublicParcoursPublisher> _logger;

	public PublicParcoursPublisher(ApplicationDbContext context, ILogger<PublicParcoursPublisher> logger)
	{
		_context = context;
		_logger = logger;
	}

	/// <summary>
	/// Point d'entrée unique appelé après enregistrement d'une fiche conseiller.
	/// Charge chaque page WordPress une seule fois et n'exécute un UPDATE que si le contenu a changé.
	/// </summary>
	public async Task PublishProfileChangesAsync(ConseillersPersonnels previous, ConseillersPersonnels next, CancellationToken cancellationToken)
	{
		if (previous == null || next == null)
		{
			return;
		}
		bool pageFrChanged = ConseillerPublicParcours.ShouldPublishText(previous.CpPageWebFr, next.CpPageWebFr);
		bool pageEnChanged = ConseillerPublicParcours.ShouldPublishText(previous.CpPageWebEn, next.CpPageWebEn);
		bool parcoursFrChanged = ConseillerPublicParcours.ShouldPublish(previous.CpPageWebFr, previous.CpParcoursFr, next.CpPageWebFr, next.CpParcoursFr);
		bool parcoursEnChanged = ConseillerPublicParcours.ShouldPublish(previous.CpPageWebEn, previous.CpParcoursEn, next.CpPageWebEn, next.CpParcoursEn);
		string prevName = ConseillerPublicParcours.BuildFullName(previous.CpPrenom, previous.CpNomFamille);
		string nextName = ConseillerPublicParcours.BuildFullName(next.CpPrenom, next.CpNomFamille);
		bool nameChanged = !string.Equals(prevName, nextName, StringComparison.Ordinal);
		bool emailChanged = ConseillerPublicParcours.ShouldPublishText(previous.CpMel, next.CpMel);
		string prevRsac = ConseillerPublicParcours.EffectiveRsac(previous.CpStatut, previous.CpNumrsac);
		string nextRsac = ConseillerPublicParcours.EffectiveRsac(next.CpStatut, next.CpNumrsac);
		bool rsacChanged = !string.Equals(prevRsac ?? "", nextRsac ?? "", StringComparison.Ordinal);
		bool linkedInChanged = ConseillerPublicParcours.ShouldPublishLinkedIn(previous.CpLinkedIn, next.CpLinkedIn);
		bool googleChanged = ConseillerPublicParcours.ShouldPublishText(previous.CpGoogle, next.CpGoogle);

		await PublishLanguageAsync(
			next.CpRefConseiller,
			next.CpPageWebFr,
			next.CpParcoursFr,
			nextName,
			next.CpMel,
			next.CpStatut,
			next.CpNumrsac,
			next.CpLinkedIn,
			next.CpGoogle,
			updateParcours: parcoursFrChanged,
			updateName: nameChanged || pageFrChanged,
			updateEmail: emailChanged || pageFrChanged,
			updateRsac: rsacChanged || pageFrChanged,
			updateLinkedIn: linkedInChanged || pageFrChanged,
			updateGoogle: googleChanged || pageFrChanged,
			cancellationToken);

		await PublishLanguageAsync(
			next.CpRefConseiller,
			next.CpPageWebEn,
			next.CpParcoursEn,
			nextName,
			next.CpMel,
			next.CpStatut,
			next.CpNumrsac,
			next.CpLinkedIn,
			next.CpGoogle,
			updateParcours: parcoursEnChanged,
			updateName: nameChanged || pageEnChanged,
			updateEmail: emailChanged || pageEnChanged,
			updateRsac: rsacChanged || pageEnChanged,
			updateLinkedIn: linkedInChanged || pageEnChanged,
			updateGoogle: googleChanged || pageEnChanged,
			cancellationToken);
	}

	public async Task PublishAsync(uint conseillerId, string previousUrl, string previousText, string nextUrl, string nextText, CancellationToken cancellationToken)
	{
		if (!ConseillerPublicParcours.ShouldPublish(previousUrl, previousText, nextUrl, nextText))
		{
			return;
		}
		if (!ConseillerPublicParcours.TryPerleRarePage(nextUrl, out string language, out string slug))
		{
			return;
		}
		try
		{
			await UpdateProfilePageAsync(
				conseillerId,
				language,
				slug,
				nextText ?? "",
				fullName: null,
				email: null,
				statut: null,
				numRsac: null,
				linkedIn: null,
				google: null,
				updateParcours: true,
				updateName: false,
				updateEmail: false,
				updateRsac: false,
				updateLinkedIn: false,
				updateGoogle: false,
				cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Publication du parcours impossible pour le conseiller {ConseillerId}, page {Slug}.", conseillerId, slug);
		}
	}

	/// <summary>
	/// Recopie CP_LinkedIn sur les pages FR et EN du conseiller, et sur les cartes
	/// qui citent ces pages. Un échec n'annule pas l'enregistrement CRM.
	/// </summary>
	public async Task PublishLinkedInAsync(uint conseillerId, string previousLinkedIn, string nextLinkedIn, string previousPageFr, string nextPageFr, string previousPageEn, string nextPageEn, CancellationToken cancellationToken)
	{
		bool linkedInChanged = ConseillerPublicParcours.ShouldPublishLinkedIn(previousLinkedIn, nextLinkedIn);
		bool frenchChanged = !string.Equals(previousPageFr ?? "", nextPageFr ?? "", StringComparison.Ordinal);
		bool englishChanged = !string.Equals(previousPageEn ?? "", nextPageEn ?? "", StringComparison.Ordinal);
		if (!linkedInChanged && !frenchChanged && !englishChanged)
		{
			return;
		}
		string value = nextLinkedIn ?? "";
		if (linkedInChanged || frenchChanged)
		{
			await PublishLanguageAsync(conseillerId, nextPageFr, null, null, null, null, null, value, null, false, false, false, false, true, false, cancellationToken);
		}
		if (linkedInChanged || englishChanged)
		{
			await PublishLanguageAsync(conseillerId, nextPageEn, null, null, null, null, null, value, null, false, false, false, false, true, false, cancellationToken);
		}
	}

	private async Task PublishLanguageAsync(
		uint conseillerId,
		string pageUrl,
		string parcours,
		string fullName,
		string email,
		string statut,
		string numRsac,
		string linkedIn,
		string google,
		bool updateParcours,
		bool updateName,
		bool updateEmail,
		bool updateRsac,
		bool updateLinkedIn,
		bool updateGoogle,
		CancellationToken cancellationToken)
	{
		bool anyProfile = updateParcours || updateName || updateEmail || updateRsac || updateLinkedIn || updateGoogle;
		ConseillerCardChanges cardChanges = new ConseillerCardChanges
		{
			UpdateName = updateName && !string.IsNullOrWhiteSpace(fullName),
			FullName = fullName,
			UpdateEmail = updateEmail,
			Email = email ?? "",
			UpdateLinkedIn = updateLinkedIn,
			LinkedIn = linkedIn ?? "",
			UpdateGoogle = updateGoogle,
			Google = google ?? ""
		};
		if (!anyProfile && !cardChanges.Any)
		{
			return;
		}
		if (!ConseillerPublicParcours.TryPerleRarePage(pageUrl, out string language, out string slug))
		{
			return;
		}
		try
		{
			if (anyProfile)
			{
				await UpdateProfilePageAsync(
					conseillerId,
					language,
					slug,
					parcours ?? "",
					fullName,
					email ?? "",
					statut,
					numRsac,
					linkedIn ?? "",
					google ?? "",
					updateParcours,
					updateName,
					updateEmail,
					updateRsac,
					updateLinkedIn,
					updateGoogle,
					cancellationToken);
			}
			if (cardChanges.Any)
			{
				await UpdateRelatedCardsAsync(conseillerId, language, slug, pageUrl, cardChanges, cancellationToken);
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Publication publique impossible pour le conseiller {ConseillerId}, page {Slug}.", conseillerId, slug);
		}
	}

	private async Task UpdateProfilePageAsync(
		uint conseillerId,
		string language,
		string slug,
		string parcours,
		string fullName,
		string email,
		string statut,
		string numRsac,
		string linkedIn,
		string google,
		bool updateParcours,
		bool updateName,
		bool updateEmail,
		bool updateRsac,
		bool updateLinkedIn,
		bool updateGoogle,
		CancellationToken cancellationToken)
	{
		DbConnection connection = _context.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
		long? pageId = null;
		string postTitle = null;
		string postContent = null;
		int matches = 0;
		await using (DbCommand select = connection.CreateCommand())
		{
			select.CommandText = $"SELECT ID, post_title, post_content FROM `{DatabaseName}`.`{PostsTable}` WHERE post_name = @slug AND post_type = 'page' AND post_status = 'publish'";
			Add(select, "@slug", slug);
			await using DbDataReader reader = await select.ExecuteReaderAsync(cancellationToken);
			while (await reader.ReadAsync(cancellationToken))
			{
				matches++;
				pageId = Convert.ToInt64(reader.GetValue(0));
				postTitle = reader.IsDBNull(1) ? "" : reader.GetString(1);
				postContent = reader.IsDBNull(2) ? "" : reader.GetString(2);
			}
		}
		if (matches != 1 || pageId == null)
		{
			_logger.LogWarning("Page publique introuvable ou ambigue pour le conseiller {ConseillerId}, slug {Slug} ({Count}).", conseillerId, slug, matches);
			return;
		}

		string nextContent = postContent;
		string nextTitle = postTitle;
		List<string> applied = new List<string>();

		if (updateName && !string.IsNullOrWhiteSpace(fullName))
		{
			if (ConseillerPublicParcours.TryReplaceProfileName(nextContent, fullName, out string withName))
			{
				nextContent = withName;
				nextTitle = fullName.Trim();
				applied.Add("nom");
			}
			else
			{
				_logger.LogWarning("La page {Slug} n'a pas le bloc nom attendu.", slug);
			}
		}
		if (updateParcours)
		{
			if (ConseillerPublicParcours.TryReplaceBiography(nextContent, parcours, out string withBio))
			{
				nextContent = withBio;
				applied.Add("parcours");
			}
			else
			{
				_logger.LogWarning("La page {Slug} n'a pas le bloc parcours attendu.", slug);
			}
		}
		if (updateRsac)
		{
			if (ConseillerPublicParcours.TryReplaceRsac(nextContent, language, statut, numRsac, out string withRsac))
			{
				nextContent = withRsac;
				applied.Add("rsac");
			}
			else
			{
				_logger.LogWarning("Impossible de mettre a jour le RSAC sur la page {Slug}.", slug);
			}
		}
		if (updateEmail)
		{
			if (ConseillerPublicParcours.TryReplaceEmail(nextContent, email, out string withEmail))
			{
				nextContent = withEmail;
				applied.Add("email");
			}
			else
			{
				_logger.LogWarning("Impossible de mettre a jour l'e-mail sur la page {Slug}.", slug);
			}
		}
		if (updateLinkedIn)
		{
			if (ConseillerPublicParcours.TryReplaceLinkedIn(nextContent, linkedIn, out string withLinkedIn))
			{
				nextContent = withLinkedIn;
				applied.Add("linkedin");
			}
			else
			{
				_logger.LogWarning("Impossible de mettre a jour le LinkedIn sur la page {Slug}.", slug);
			}
		}
		if (updateGoogle)
		{
			if (ConseillerPublicParcours.TryReplaceGoogle(nextContent, google, out string withGoogle))
			{
				nextContent = withGoogle;
				applied.Add("google");
			}
			else
			{
				_logger.LogWarning("Impossible de mettre a jour la fiche Google sur la page {Slug}.", slug);
			}
		}

		bool contentChanged = !string.Equals(nextContent, postContent, StringComparison.Ordinal);
		bool titleChanged = !string.Equals(nextTitle, postTitle, StringComparison.Ordinal);
		if (!contentChanged && !titleChanged)
		{
			return;
		}
		await using DbCommand update = connection.CreateCommand();
		update.CommandText = $"UPDATE `{DatabaseName}`.`{PostsTable}` SET post_title = @title, post_content = @content, post_modified = NOW(), post_modified_gmt = UTC_TIMESTAMP() WHERE ID = @id AND post_type = 'page' AND post_status = 'publish'";
		Add(update, "@title", nextTitle);
		Add(update, "@content", nextContent);
		Add(update, "@id", pageId.Value);
		int written = await update.ExecuteNonQueryAsync(cancellationToken);
		if (written == 1)
		{
			_logger.LogInformation("Fiche publique {Slug} mise a jour ({Fields}).", slug, string.Join(", ", applied));
		}
	}

	private async Task UpdateRelatedCardsAsync(uint conseillerId, string language, string slug, string profileUrl, ConseillerCardChanges changes, CancellationToken cancellationToken)
	{
		DbConnection connection = _context.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
		List<(long Id, string Content)> pages = new List<(long, string)>();
		await using (DbCommand select = connection.CreateCommand())
		{
			select.CommandText = $"SELECT ID, post_content FROM `{DatabaseName}`.`{PostsTable}` WHERE post_type = 'page' AND post_status = 'publish' AND post_name <> @slug AND post_content LIKE @needle";
			Add(select, "@slug", slug);
			Add(select, "@needle", "%/" + language + "/" + slug + "%");
			await using DbDataReader reader = await select.ExecuteReaderAsync(cancellationToken);
			while (await reader.ReadAsync(cancellationToken))
			{
				long id = Convert.ToInt64(reader.GetValue(0));
				string content = reader.IsDBNull(1) ? "" : reader.GetString(1);
				pages.Add((id, content));
			}
		}
		foreach ((long id, string content) in pages)
		{
			LinkedInCardUpdate result = ConseillerPublicParcours.ReplaceCardFieldsNearProfile(content, profileUrl, changes, out string updated);
			if (result == LinkedInCardUpdate.Unsafe)
			{
				_logger.LogWarning("Carte conseiller ambigue, page {PageId} non modifiee pour {Slug} (conseiller {ConseillerId}).", id, slug, conseillerId);
				continue;
			}
			if (result != LinkedInCardUpdate.Updated || string.Equals(updated, content, StringComparison.Ordinal))
			{
				continue;
			}
			await using DbCommand update = connection.CreateCommand();
			update.CommandText = $"UPDATE `{DatabaseName}`.`{PostsTable}` SET post_content = @content, post_modified = NOW(), post_modified_gmt = UTC_TIMESTAMP() WHERE ID = @id AND post_type = 'page' AND post_status = 'publish'";
			Add(update, "@content", updated);
			Add(update, "@id", id);
			int written = await update.ExecuteNonQueryAsync(cancellationToken);
			if (written == 1)
			{
				_logger.LogInformation("Carte publique mise a jour sur la page {PageId} pour {Slug}.", id, slug);
			}
		}
	}

	private static void Add(DbCommand command, string name, object value)
	{
		DbParameter parameter = command.CreateParameter();
		parameter.ParameterName = name;
		parameter.Value = value;
		command.Parameters.Add(parameter);
	}
}
