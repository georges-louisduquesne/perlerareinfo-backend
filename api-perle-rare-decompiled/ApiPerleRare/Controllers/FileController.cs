using ApiPerleRare.Application.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FileController : ControllerBase
{
	private readonly IUploadFileUseCase _upload;

	private readonly IDownloadFileUseCase _download;

	private readonly IFileStorage _storage;

	public FileController(IUploadFileUseCase upload, IDownloadFileUseCase download, IFileStorage storage)
	{
		_upload = upload;
		_download = download;
		_storage = storage;
	}

	[HttpPost]
	[Route("Upload")]
	public FileResponse Upload(FileUploadRequest fileUpload)
	{
		return _upload.Execute(fileUpload);
	}

	[HttpPost]
	[Route("Download")]
	public FileDownloadResponse Download(FileDownloadRequest request)
	{
		return _download.Execute(request);
	}

	/// <summary>Logo d’agence servi aux balises img, limité au dossier logos/agences.</summary>
	[HttpGet("public/logos/agences/{fileName}")]
	[AllowAnonymous]
	public IActionResult PublicAgencyLogo(string fileName)
	{
		if (!AgencyLogoAddress.TryPublicFileName(fileName, out string relative, out string contentType))
		{
			return NotFound();
		}
		if (!_storage.TryResolve(relative, out string fullPath, out _) || !_storage.Exists(fullPath))
		{
			return NotFound();
		}
		byte[] bytes = _storage.ReadAllBytes(fullPath);
		Response.Headers["X-Content-Type-Options"] = "nosniff";
		Response.Headers["Cache-Control"] = "public, max-age=86400";
		return File(bytes, contentType);
	}
}
