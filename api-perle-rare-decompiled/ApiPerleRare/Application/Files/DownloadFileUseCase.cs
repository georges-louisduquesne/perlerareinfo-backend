using System;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Application.Files;

public sealed class DownloadFileUseCase : IDownloadFileUseCase
{
	private readonly IFileStorage _storage;

	public DownloadFileUseCase(IFileStorage storage)
	{
		_storage = storage;
	}

	public FileDownloadResponse Execute(FileDownloadRequest request)
	{
		try
		{
			if (request == null)
			{
				throw new InvalidOperationException("Requête invalide");
			}
			if (!_storage.TryResolve(request.FileName, out string fullPath, out string error))
			{
				throw new InvalidOperationException(error ?? "Requête invalide");
			}
			if (!_storage.Exists(fullPath))
			{
				throw new InvalidOperationException("Fichier introuvable");
			}
			return new FileDownloadResponse
			{
				IsSuccess = true,
				FileContent = Convert.ToBase64String(_storage.ReadAllBytes(fullPath))
			};
		}
		catch (Exception)
		{
			return new FileDownloadResponse
			{
				IsSuccess = false,
				Errors = ClientError.Generic
			};
		}
	}
}
