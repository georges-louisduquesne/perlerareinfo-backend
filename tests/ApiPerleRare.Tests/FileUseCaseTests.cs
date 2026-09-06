using System;
using System.Collections.Generic;
using ApiPerleRare.Application.Files;
using ApiPerleRare.Helpers;
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
	}
}
