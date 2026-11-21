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
/// Publie le parcours et le LinkedIn d'un conseiller sur https://perle-rare.com.
/// Le parcours va sur la page dédiée (FR ou EN). Le LinkedIn va sur cette page
/// et sur les cartes des autres pages qui pointent vers elle.
/// La base WordPress est sur le meme MariaDB. Un echec n'annule pas l'enregistrement CRM.
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

	public async Task PublishAsync(uint conseillerId, string previousUrl, string previousText, string nextUrl, string nextText, CancellationToken cancellationToken)
	{
		if (!ConseillerPublicParcours.ShouldPublish(previousUrl, previousText, nextUrl, nextText))
		{
			return;
		}
		if (!ConseillerPublicParcours.TryPerleRareSlug(nextUrl, out string slug))
		{
			return;
		}
		try
		{
			await UpdatePageAsync(slug, nextText ?? "", cancellationToken);
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
			await PublishLinkedInForPageAsync(conseillerId, nextPageFr, value, cancellationToken);
		}
		if (linkedInChanged || englishChanged)
		{
			await PublishLinkedInForPageAsync(conseillerId, nextPageEn, value, cancellationToken);
		}
	}

	private async Task PublishLinkedInForPageAsync(uint conseillerId, string pageUrl, string linkedIn, CancellationToken cancellationToken)
	{
		if (!ConseillerPublicParcours.TryPerleRarePage(pageUrl, out string language, out string slug))
		{
			return;
		}
		try
		{
			await UpdateLinkedInAsync(slug, linkedIn, cancellationToken);
			await UpdateLinkedInOnRelatedPagesAsync(language, slug, pageUrl, linkedIn, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Publication du LinkedIn impossible pour le conseiller {ConseillerId}, page {Slug}.", conseillerId, slug);
		}
	}

	private async Task UpdatePageAsync(string slug, string parcours, CancellationToken cancellationToken)
	{
		DbConnection connection = _context.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
		long? pageId = null;
		string postContent = null;
		int matches = 0;
		await using (DbCommand select = connection.CreateCommand())
		{
			select.CommandText = $"SELECT ID, post_content FROM `{DatabaseName}`.`{PostsTable}` WHERE post_name = @slug AND post_type = 'page' AND post_status = 'publish'";
			Add(select, "@slug", slug);
			await using DbDataReader reader = await select.ExecuteReaderAsync(cancellationToken);
			while (await reader.ReadAsync(cancellationToken))
			{
				matches++;
				pageId = Convert.ToInt64(reader.GetValue(0));
				postContent = reader.IsDBNull(1) ? "" : reader.GetString(1);
			}
		}
		if (matches != 1 || pageId == null)
		{
			_logger.LogWarning("Page publique introuvable ou ambigue pour {Slug} ({Count}).", slug, matches);
			return;
		}
		if (!ConseillerPublicParcours.TryReplaceBiography(postContent, parcours, out string updated))
		{
			_logger.LogWarning("La page {Slug} n'a pas le bloc parcours attendu.", slug);
			return;
		}
		if (string.Equals(updated, postContent, StringComparison.Ordinal))
		{
			return;
		}
		await using DbCommand update = connection.CreateCommand();
		update.CommandText = $"UPDATE `{DatabaseName}`.`{PostsTable}` SET post_content = @content, post_modified = NOW(), post_modified_gmt = UTC_TIMESTAMP() WHERE ID = @id AND post_type = 'page' AND post_status = 'publish'";
		Add(update, "@content", updated);
		Add(update, "@id", pageId.Value);
		int written = await update.ExecuteNonQueryAsync(cancellationToken);
		if (written == 1)
		{
			_logger.LogInformation("Parcours publie sur la page {Slug}.", slug);
		}
	}

	private async Task UpdateLinkedInAsync(string slug, string linkedIn, CancellationToken cancellationToken)
	{
		DbConnection connection = _context.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
		long? pageId = null;
		string postContent = null;
		int matches = 0;
		await using (DbCommand select = connection.CreateCommand())
		{
			select.CommandText = $"SELECT ID, post_content FROM `{DatabaseName}`.`{PostsTable}` WHERE post_name = @slug AND post_type = 'page' AND post_status = 'publish'";
			Add(select, "@slug", slug);
			await using DbDataReader reader = await select.ExecuteReaderAsync(cancellationToken);
			while (await reader.ReadAsync(cancellationToken))
			{
				matches++;
				pageId = Convert.ToInt64(reader.GetValue(0));
				postContent = reader.IsDBNull(1) ? "" : reader.GetString(1);
			}
		}
		if (matches != 1 || pageId == null)
		{
			_logger.LogWarning("Page publique introuvable ou ambigue pour {Slug} ({Count}).", slug, matches);
			return;
		}
		if (!ConseillerPublicParcours.TryReplaceLinkedIn(postContent, linkedIn, out string updated))
		{
			_logger.LogWarning("La page {Slug} n'a pas le lien LinkedIn attendu.", slug);
			return;
		}
		if (string.Equals(updated, postContent, StringComparison.Ordinal))
		{
			return;
		}
		await using DbCommand update = connection.CreateCommand();
		update.CommandText = $"UPDATE `{DatabaseName}`.`{PostsTable}` SET post_content = @content, post_modified = NOW(), post_modified_gmt = UTC_TIMESTAMP() WHERE ID = @id AND post_type = 'page' AND post_status = 'publish'";
		Add(update, "@content", updated);
		Add(update, "@id", pageId.Value);
		int written = await update.ExecuteNonQueryAsync(cancellationToken);
		if (written == 1)
		{
			_logger.LogInformation("LinkedIn publie sur la page {Slug}.", slug);
		}
	}

	private async Task UpdateLinkedInOnRelatedPagesAsync(string language, string slug, string profileUrl, string linkedIn, CancellationToken cancellationToken)
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
			LinkedInCardUpdate result = ConseillerPublicParcours.ReplaceLinkedInNearProfile(content, profileUrl, linkedIn, out string updated);
			if (result == LinkedInCardUpdate.Unsafe)
			{
				_logger.LogWarning("Carte LinkedIn ambigue, page {PageId} non modifiee pour {Slug}.", id, slug);
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
				_logger.LogInformation("LinkedIn publie sur une carte de la page {PageId} pour {Slug}.", id, slug);
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
