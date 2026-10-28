using System;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Files;

public sealed class ConseillerPhotoRequest
{
	public string Url { get; set; }

	public string Path { get; set; }
}

public sealed class ConseillerPhotoResult
{
	public string CpPhoto { get; set; }
}

/// <summary>
/// Télécharge le portrait et l’enregistre sous cp_files/{id}.ext, puis CP_Photo (varchar 100).
/// </summary>
public sealed class ImportConseillerPhotoUseCase
{
	private readonly ApplicationDbContext _db;

	private readonly IFileStorage _storage;

	public ImportConseillerPhotoUseCase(ApplicationDbContext db, IFileStorage storage)
	{
		_db = db;
		_storage = storage;
	}

	public async Task<ConseillerPhotoResult> ExecuteSet(uint conseillerId, string rawUrl, CancellationToken cancellationToken)
	{
		if (!AgencyLogoAddress.TryCreate(rawUrl, out Uri uri, out string urlError))
		{
			throw new InvalidOperationException(urlError);
		}
		byte[] bytes = await AgencyLogoAddress.DownloadAsync(uri, cancellationToken);
		if (!AgencyLogoAddress.TryDetect(bytes, out string extension, out string detectError))
		{
			throw new InvalidOperationException(detectError);
		}
		string relative = ConseillerPhotoPath.Relative(conseillerId, extension);
		if (!ConseillerPhotoPath.Fits(relative))
		{
			throw new InvalidOperationException("Chemin de photo trop long.");
		}
		if (!_storage.TryResolve(relative, out string fullPath, out string pathError))
		{
			throw new InvalidOperationException(pathError ?? "Chemin de photo invalide");
		}
		ConseillersPersonnels row = await Load(conseillerId, cancellationToken);
		DeleteOwnedFiles(_storage, conseillerId, row.CpPhoto, relative);
		_storage.WriteAllBytes(fullPath, bytes);
		row.CpPhoto = relative;
		await _db.SaveChangesAsync(cancellationToken);
		return new ConseillerPhotoResult { CpPhoto = relative };
	}

	public async Task<ConseillerPhotoResult> ExecuteAdopt(uint conseillerId, string rawPath, CancellationToken cancellationToken)
	{
		string relative = Normalize(rawPath);
		if (!ConseillerPhotoPath.IsOwned(relative))
		{
			throw new InvalidOperationException("Chemin de photo invalide.");
		}
		if (!_storage.TryResolve(relative, out string fullPath, out string pathError))
		{
			throw new InvalidOperationException(pathError ?? "Chemin de photo invalide");
		}
		if (!_storage.Exists(fullPath))
		{
			throw new InvalidOperationException("Fichier introuvable");
		}
		ConseillersPersonnels row = await Load(conseillerId, cancellationToken);
		DeleteOwnedFiles(_storage, conseillerId, row.CpPhoto, relative);
		row.CpPhoto = relative;
		await _db.SaveChangesAsync(cancellationToken);
		return new ConseillerPhotoResult { CpPhoto = relative };
	}

	public async Task ExecuteClear(uint conseillerId, CancellationToken cancellationToken)
	{
		ConseillersPersonnels row = await Load(conseillerId, cancellationToken);
		DeleteOwnedFiles(_storage, conseillerId, row.CpPhoto, null);
		row.CpPhoto = "";
		await _db.SaveChangesAsync(cancellationToken);
	}

	private async Task<ConseillersPersonnels> Load(uint conseillerId, CancellationToken cancellationToken)
	{
		ConseillersPersonnels row = await _db.ConseillersPersonnels
			.FirstOrDefaultAsync((ConseillersPersonnels item) => item.CpRefConseiller == conseillerId, cancellationToken);
		if (row == null)
		{
			throw new InvalidOperationException("Conseiller introuvable");
		}
		return row;
	}

	public static void DeleteOwnedFiles(IFileStorage storage, uint id, string previous, string keepRelative)
	{
		if (storage == null)
		{
			return;
		}
		string keep = Normalize(keepRelative);
		foreach (string extension in AgencyLogoAddress.AgencyExtensions)
		{
			TryDelete(storage, ConseillerPhotoPath.Relative(id, extension), keep);
		}
		string stored = Normalize(previous);
		if (ConseillerPhotoPath.IsOwned(stored))
		{
			TryDelete(storage, stored, keep);
		}
	}

	private static void TryDelete(IFileStorage storage, string relative, string keep)
	{
		if (string.Equals(relative, keep, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		if (storage.TryResolve(relative, out string fullPath, out _) && storage.Exists(fullPath))
		{
			storage.Delete(fullPath);
		}
	}

	private static string Normalize(string value)
	{
		return (value ?? "").Replace('\\', '/').Trim().TrimStart('/');
	}
}

public static class ConseillerPhotoPath
{
	public const string Folder = "cp_files";

	public const int MaxLength = 100;

	public static string Relative(uint id, string extension)
	{
		return Folder + "/" + id + "." + (extension ?? "").Trim().TrimStart('.');
	}

	public static bool Fits(string relative)
	{
		return !string.IsNullOrEmpty(relative) && relative.Length <= MaxLength;
	}

	public static bool IsOwned(string relative)
	{
		string stored = (relative ?? "").Replace('\\', '/').Trim().TrimStart('/');
		if (stored.Length == 0 || stored.Length > MaxLength || stored.Contains(".."))
		{
			return false;
		}
		if (!stored.StartsWith(Folder + "/", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string name = stored.Substring(Folder.Length + 1);
		if (name.Length == 0 || name.Contains('/'))
		{
			return false;
		}
		string ext = System.IO.Path.GetExtension(name).TrimStart('.').ToLowerInvariant();
		return ext == "png" || ext == "jpg" || ext == "jpeg" || ext == "gif" || ext == "webp" || ext == "svg";
	}
}
