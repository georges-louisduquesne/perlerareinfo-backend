using System;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Application.Files;

public sealed class UploadFileUseCase : IUploadFileUseCase
{
	internal const int MaxBase64Length = 28_000_000;

	private readonly IFileStorage _storage;

	public UploadFileUseCase(IFileStorage storage)
	{
		_storage = storage;
	}

	public FileResponse Execute(FileUploadRequest request)
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
			if (string.IsNullOrEmpty(request.FileContent) || request.FileContent.Length > MaxBase64Length)
			{
				throw new InvalidOperationException("Fichier trop volumineux");
			}
			byte[] content = OodriveMacPdfRepair.Repair(Convert.FromBase64String(request.FileContent));
			_storage.WriteAllBytes(fullPath, content);
			return new FileResponse
			{
				IsSuccess = true,
				FullPath = request.FileName.Replace("\\", "/")
			};
		}
		catch (Exception)
		{
			return new FileResponse
			{
				IsSuccess = false,
				Errors = ClientError.Generic
			};
		}
	}
}
