using System.IO;
using ApiPerleRare.Application.Files;
using ApiPerleRare.Helpers;
using Microsoft.Extensions.Configuration;

namespace ApiPerleRare.Infrastructure.Files;

public sealed class LocalFileStorage : IFileStorage
{
	private readonly string _root;

	public LocalFileStorage(IConfiguration configuration)
	{
		_root = configuration.GetSection("AppSettings")["OldWebSiteFolder"];
	}

	public bool TryResolve(string relativeName, out string fullPath, out string error)
	{
		return SafeFilePath.TryResolve(_root, relativeName, out fullPath, out error);
	}

	public void WriteAllBytes(string fullPath, byte[] content)
	{
		string folder = Path.GetDirectoryName(fullPath);
		if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
		{
			Directory.CreateDirectory(folder);
		}
		File.WriteAllBytes(fullPath, content);
	}

	public bool Exists(string fullPath) => File.Exists(fullPath);

	public byte[] ReadAllBytes(string fullPath) => File.ReadAllBytes(fullPath);
}
