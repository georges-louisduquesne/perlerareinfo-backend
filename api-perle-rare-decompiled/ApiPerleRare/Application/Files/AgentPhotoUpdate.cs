using System;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Files;

public sealed class AgentPhotoRequest
{
	public string Url { get; set; }
}

public sealed class AgentPhotoResult
{
	public string CPhoto { get; set; }
}

/// <summary>
/// Enregistre ou retire l’URL de contact_intermediaire.C_Photo (varchar 300).
/// Pas de téléchargement : la fiche agent stocke le lien, pas un fichier logos/.
/// </summary>
public sealed class UpdateAgentPhotoUseCase
{
	private readonly ApplicationDbContext _db;

	public UpdateAgentPhotoUseCase(ApplicationDbContext db)
	{
		_db = db;
	}

	public async Task<AgentPhotoResult> ExecuteSet(uint agentId, string rawUrl, CancellationToken cancellationToken)
	{
		if (!AgentPhotoUrl.TryNormalize(rawUrl, out string url, out string error))
		{
			throw new InvalidOperationException(error);
		}
		ContactIntermediaire row = await Load(agentId, cancellationToken);
		row.CPhoto = url;
		await _db.SaveChangesAsync(cancellationToken);
		return new AgentPhotoResult { CPhoto = url };
	}

	public async Task ExecuteClear(uint agentId, CancellationToken cancellationToken)
	{
		ContactIntermediaire row = await Load(agentId, cancellationToken);
		row.CPhoto = null;
		await _db.SaveChangesAsync(cancellationToken);
	}

	private async Task<ContactIntermediaire> Load(uint agentId, CancellationToken cancellationToken)
	{
		ContactIntermediaire row = await _db.ContactIntermediaire
			.FirstOrDefaultAsync((ContactIntermediaire item) => item.CRefCtcInter == agentId, cancellationToken);
		if (row == null)
		{
			throw new InvalidOperationException("Agent introuvable");
		}
		return row;
	}
}

internal static class AgentPhotoUrl
{
	public const int MaxLength = 300;

	public static bool TryNormalize(string raw, out string url, out string error)
	{
		url = null;
		error = null;
		string value = (raw ?? "").Trim();
		if (value.Length == 0)
		{
			error = "Collez une URL http ou https.";
			return false;
		}
		if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			&& !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			value = "https://" + value.TrimStart('/');
		}
		if (value.Length > MaxLength || !Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
		{
			error = "Collez une URL http ou https.";
			return false;
		}
		if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
		{
			error = "Collez une URL http ou https.";
			return false;
		}
		if (string.IsNullOrEmpty(uri.Host) || uri.Host.IndexOf('.') < 0)
		{
			error = "Collez une URL http ou https.";
			return false;
		}
		url = value;
		return true;
	}
}
