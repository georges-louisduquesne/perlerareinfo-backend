using System;
using System.IO;
using ApiPerleRare.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FileController : ControllerBase
{
	public class FileUploadRequest
	{
		public string FileName { get; set; }

		public string FileContent { get; set; }
	}

	public class FileResponse
	{
		public bool IsSuccess { get; set; }

		public string Errors { get; set; }

		public string FullPath { get; set; }
	}

	public class FileDownloadRequest
	{
		public string FileName { get; set; }
	}

	public class FileDownloadResponse : FileResponse
	{
		public string FileContent { get; set; }
	}

	private readonly string _oldWebSiteFolder;

	public FileController(IConfiguration configuration)
	{
		_oldWebSiteFolder = configuration.GetSection("AppSettings")["OldWebSiteFolder"];
	}

	[HttpPost]
	[Route("Upload")]
	public FileResponse Upload(FileUploadRequest fileUpload)
	{
		try
		{
			if (!SafeFilePath.TryResolve(_oldWebSiteFolder, fileUpload.FileName, out string fullPath, out string error))
			{
				throw new Exception(error);
			}
			if (string.IsNullOrEmpty(fileUpload.FileContent) || fileUpload.FileContent.Length > 28_000_000)
			{
				throw new Exception("Fichier trop volumineux");
			}
			string folder = Path.GetDirectoryName(fullPath);
			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}
			System.IO.File.WriteAllBytes(fullPath, Convert.FromBase64String(fileUpload.FileContent));
			return new FileResponse
			{
				IsSuccess = true,
				FullPath = fileUpload.FileName.Replace("\\", "/")
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

	[HttpPost]
	[Route("Download")]
	public FileDownloadResponse Download(FileDownloadRequest request)
	{
		try
		{
			if (!SafeFilePath.TryResolve(_oldWebSiteFolder, request.FileName, out string fullPath, out string error))
			{
				throw new Exception(error);
			}
			if (!System.IO.File.Exists(fullPath))
			{
				throw new Exception("Fichier introuvable");
			}
			return new FileDownloadResponse
			{
				IsSuccess = true,
				FileContent = Convert.ToBase64String(System.IO.File.ReadAllBytes(fullPath))
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
