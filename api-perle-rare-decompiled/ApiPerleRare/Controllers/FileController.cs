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

	public FileController(IUploadFileUseCase upload, IDownloadFileUseCase download)
	{
		_upload = upload;
		_download = download;
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
}
