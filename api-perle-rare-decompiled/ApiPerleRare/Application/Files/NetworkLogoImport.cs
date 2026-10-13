using System;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Files;

public sealed class NetworkLogoImportResult
{
	public string I2Icon { get; set; }
}

/// <summary>
/// Télécharge une image publique et l’enregistre sous logos/reseaux/{id}.ext.
/// </summary>
public sealed class ImportNetworkLogoUseCase
{
	private readonly ApplicationDbContext _db;

	private readonly IFileStorage _storage;

	public ImportNetworkLogoUseCase(ApplicationDbContext db, IFileStorage storage)
	{
		_db = db;
		_storage = storage;
	}

	public async Task ExecuteClear(uint networkId, CancellationToken cancellationToken)
	{
		IntermediairesIndirects row = await _db.IntermediairesIndirects
			.FirstOrDefaultAsync((IntermediairesIndirects item) => item.I2RefIntermIndirect == networkId, cancellationToken);
		if (row == null)
		{
			throw new InvalidOperationException("Réseau introuvable");
		}
		AgencyLogoAddress.DeleteLogoFiles(_storage, "logos/reseaux", networkId, null);
		// I2_Icon est NOT NULL : une chaîne vide retire l’image sans violer la colonne.
		row.I2Icon = "";
		await _db.SaveChangesAsync(cancellationToken);
	}

	public async Task<NetworkLogoImportResult> Execute(uint networkId, string rawUrl, CancellationToken cancellationToken)
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
		string relative = "logos/reseaux/" + networkId + "." + extension;
		if (!_storage.TryResolve(relative, out string fullPath, out string pathError))
		{
			throw new InvalidOperationException(pathError ?? "Chemin de logo invalide");
		}
		IntermediairesIndirects row = await _db.IntermediairesIndirects
			.FirstOrDefaultAsync((IntermediairesIndirects item) => item.I2RefIntermIndirect == networkId, cancellationToken);
		if (row == null)
		{
			throw new InvalidOperationException("Réseau introuvable");
		}
		AgencyLogoAddress.DeleteLogoFiles(_storage, "logos/reseaux", networkId, relative);
		_storage.WriteAllBytes(fullPath, bytes);
		row.I2Icon = relative;
		await _db.SaveChangesAsync(cancellationToken);
		return new NetworkLogoImportResult { I2Icon = relative };
	}
}
