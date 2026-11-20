using System;
using System.Collections.Generic;
using System.IO;
using ApiPerleRare.Application.Files;
using ApiPerleRare.Helpers;
using ApiPerleRare.Infrastructure.Files;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ApiPerleRare.Tests;

public class FileUseCaseTests
{
	[Fact]
	public void Upload_writes_bytes_and_returns_relative_fullPath()
	{
		var storage = new MemoryFileStorage();
		storage.AllowResolve("docs\\note.txt", "/tmp/docs/note.txt");
		var useCase = new UploadFileUseCase(storage);

		FileResponse result = useCase.Execute(new FileUploadRequest
		{
			FileName = "docs\\note.txt",
			FileContent = Convert.ToBase64String(new byte[] { 1, 2, 3 })
		});

		Assert.True(result.IsSuccess);
		Assert.Equal("docs/note.txt", result.FullPath);
		Assert.Null(result.Errors);
		Assert.Equal(new byte[] { 1, 2, 3 }, storage.Files["/tmp/docs/note.txt"]);
	}

	[Fact]
	public void Upload_keeps_generic_error_on_traversal()
	{
		var storage = new MemoryFileStorage();
		var useCase = new UploadFileUseCase(storage);

		FileResponse result = useCase.Execute(new FileUploadRequest
		{
			FileName = "../secret.txt",
			FileContent = "Zg=="
		});

		Assert.False(result.IsSuccess);
		Assert.Equal(ClientError.Generic, result.Errors);
		Assert.Empty(storage.Files);
	}

	[Fact]
	public void Download_returns_base64_without_leaking_os_path()
	{
		var storage = new MemoryFileStorage();
		storage.AllowResolve("docs/note.txt", "/tmp/docs/note.txt");
		storage.Files["/tmp/docs/note.txt"] = new byte[] { 9, 8 };
		var useCase = new DownloadFileUseCase(storage);

		FileDownloadResponse result = useCase.Execute(new FileDownloadRequest { FileName = "docs/note.txt" });

		Assert.True(result.IsSuccess);
		Assert.Equal(Convert.ToBase64String(new byte[] { 9, 8 }), result.FileContent);
		Assert.Null(result.FullPath);
	}

	[Fact]
	public void Download_keeps_generic_error_when_missing()
	{
		var storage = new MemoryFileStorage();
		storage.AllowResolve("docs/missing.txt", "/tmp/docs/missing.txt");
		var useCase = new DownloadFileUseCase(storage);

		FileDownloadResponse result = useCase.Execute(new FileDownloadRequest { FileName = "docs/missing.txt" });

		Assert.False(result.IsSuccess);
		Assert.Equal(ClientError.Generic, result.Errors);
		Assert.Null(result.FileContent);
	}

	[Fact]
	public void Download_resolves_nfd_file_when_db_replaced_combining_mark_with_question_mark()
	{
		string root = Path.Combine(Path.GetTempPath(), "pr-download-nfd-" + Guid.NewGuid().ToString("N"));
		try
		{
			string folder = Path.Combine(root, "cp_files");
			Directory.CreateDirectory(folder);
			byte[] pdfBytes = { 0x25, 0x50, 0x44, 0x46, 0x2d, 0x31, 0x2e, 0x37 };
			File.WriteAllBytes(Path.Combine(folder, "CV_Anai\u0308s_Delattaignant (1).pdf"), pdfBytes);

			IConfiguration config = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string> { ["AppSettings:OldWebSiteFolder"] = root })
				.Build();
			var useCase = new DownloadFileUseCase(new LocalFileStorage(config));

			FileDownloadResponse result = useCase.Execute(new FileDownloadRequest
			{
				FileName = "cp_files/CV_Anai?s_Delattaignant (1).pdf"
			});

			Assert.True(result.IsSuccess);
			Assert.Equal(Convert.ToBase64String(pdfBytes), result.FileContent);
		}
		finally
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
		}
	}

	[Fact]
	public void Download_resolves_nfc_nfd_mismatch_in_same_folder()
	{
		string root = Path.Combine(Path.GetTempPath(), "pr-download-nfc-" + Guid.NewGuid().ToString("N"));
		try
		{
			string folder = Path.Combine(root, "cp_files");
			Directory.CreateDirectory(folder);
			byte[] pdfBytes = { 0x25, 0x50, 0x44, 0x46 };
			File.WriteAllBytes(Path.Combine(folder, "CV_Anai\u0308s.pdf"), pdfBytes);

			IConfiguration config = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string> { ["AppSettings:OldWebSiteFolder"] = root })
				.Build();
			var useCase = new DownloadFileUseCase(new LocalFileStorage(config));

			FileDownloadResponse result = useCase.Execute(new FileDownloadRequest
			{
				FileName = "cp_files/CV_Ana\u00efs.pdf"
			});

			Assert.True(result.IsSuccess);
			Assert.Equal(Convert.ToBase64String(pdfBytes), result.FileContent);
		}
		finally
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
		}
	}

	[Fact]
	public void Download_does_not_match_ascii_characters_with_question_mark()
	{
		string root = Path.Combine(Path.GetTempPath(), "pr-download-ascii-" + Guid.NewGuid().ToString("N"));
		try
		{
			string folder = Path.Combine(root, "cp_files");
			Directory.CreateDirectory(folder);
			File.WriteAllBytes(Path.Combine(folder, "CV_1234.pdf"), new byte[] { 1, 2, 3 });

			IConfiguration config = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string> { ["AppSettings:OldWebSiteFolder"] = root })
				.Build();
			var useCase = new DownloadFileUseCase(new LocalFileStorage(config));

			FileDownloadResponse result = useCase.Execute(new FileDownloadRequest
			{
				FileName = "cp_files/CV_123?.pdf"
			});

			Assert.False(result.IsSuccess);
			Assert.Null(result.FileContent);
		}
		finally
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
		}
	}

	[Fact]
	public void Download_rejects_ambiguous_question_mark_matches()
	{
		string root = Path.Combine(Path.GetTempPath(), "pr-download-ambig-" + Guid.NewGuid().ToString("N"));
		try
		{
			string folder = Path.Combine(root, "cp_files");
			Directory.CreateDirectory(folder);
			File.WriteAllBytes(Path.Combine(folder, "CV_Anai\u0308s.pdf"), new byte[] { 1 });
			File.WriteAllBytes(Path.Combine(folder, "CV_Anai\u0301s.pdf"), new byte[] { 2 });

			IConfiguration config = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string> { ["AppSettings:OldWebSiteFolder"] = root })
				.Build();
			var useCase = new DownloadFileUseCase(new LocalFileStorage(config));

			FileDownloadResponse result = useCase.Execute(new FileDownloadRequest
			{
				FileName = "cp_files/CV_Anai?s.pdf"
			});

			Assert.False(result.IsSuccess);
			Assert.Null(result.FileContent);
		}
		finally
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
		}
	}

	private sealed class MemoryFileStorage : IFileStorage
	{
		public Dictionary<string, string> Resolutions { get; } = new();

		public Dictionary<string, byte[]> Files { get; } = new();

		public void AllowResolve(string relative, string fullPath) => Resolutions[relative] = fullPath;

		public bool TryResolve(string relativeName, out string fullPath, out string error)
		{
			if (relativeName != null && Resolutions.TryGetValue(relativeName, out fullPath))
			{
				error = null;
				return true;
			}
			fullPath = null;
			error = "Chemin interdit";
			return false;
		}

		public void WriteAllBytes(string fullPath, byte[] content) => Files[fullPath] = content;

		public bool Exists(string fullPath) => Files.ContainsKey(fullPath);

		public byte[] ReadAllBytes(string fullPath) => Files[fullPath];

		public void Delete(string fullPath) => Files.Remove(fullPath);
	}
}
