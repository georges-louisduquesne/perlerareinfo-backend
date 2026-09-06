using System;
using System.IO;
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
			if (string.IsNullOrWhiteSpace(_oldWebSiteFolder))
			{
				throw new Exception("'OldWebSiteFolder' non spécifié");
			}
			fileUpload.FileName = fileUpload.FileName.Replace("\\", "/");
			if (!fileUpload.FileName.Contains("/"))
			{
				throw new Exception("Il faut obligatoirement spécifier un dossier");
			}
			if (Path.GetExtension(fileUpload.FileName).ToLower() == ".php")
			{
				throw new Exception("Interdit d'uploader un fichier PHP");
			}
			string fullPath = Path.Combine(_oldWebSiteFolder, fileUpload.FileName);
			string folder = Path.GetDirectoryName(fullPath);
			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}
			System.IO.File.WriteAllBytes(fullPath, Convert.FromBase64String(fileUpload.FileContent));
			return new FileResponse
			{
				IsSuccess = true,
				FullPath = fullPath
			};
		}
		catch (Exception ex)
		{
			return new FileResponse
			{
				IsSuccess = false,
				Errors = ex.ToString()
			};
		}
	}

	[HttpPost]
	[Route("Download")]
	public FileDownloadResponse Download(FileDownloadRequest request)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_oldWebSiteFolder))
			{
				throw new Exception("'OldWebSiteFolder' non spécifié");
			}
			request.FileName = request.FileName.Replace("\\", "/");
			string fullPath = Path.Combine(_oldWebSiteFolder, request.FileName);
			if (!System.IO.File.Exists(fullPath))
			{
				throw new Exception("Fichier '" + fullPath + "' introuvable");
			}
			if (Path.GetExtension(request.FileName).ToLower() == ".php")
			{
				throw new Exception("Interdit de télécharger un fichier PHP");
			}
			return new FileDownloadResponse
			{
				IsSuccess = true,
				FileContent = Convert.ToBase64String(System.IO.File.ReadAllBytes(fullPath))
			};
		}
		catch (Exception ex)
		{
			return new FileDownloadResponse
			{
				IsSuccess = false,
				Errors = ex.ToString()
			};
		}
	}
}
