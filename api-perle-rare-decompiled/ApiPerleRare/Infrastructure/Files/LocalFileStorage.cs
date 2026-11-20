using System.IO;
using System.Linq;
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

	public bool TryResolveExisting(string relativeName, out string fullPath, out string error)
	{
		if (!SafeFilePath.TryResolve(_root, relativeName, out fullPath, out error))
		{
			return false;
		}
		if (File.Exists(fullPath))
		{
			return true;
		}
		string dir = Path.GetDirectoryName(fullPath);
		string requestedName = Path.GetFileName(fullPath);
		if (string.IsNullOrEmpty(dir)
			|| !SafeFilePath.NeedsExistingFallback(requestedName)
			|| !Directory.Exists(dir))
		{
			error = "Fichier introuvable";
			return false;
		}
		if (!SafeFilePath.TryMatchExistingFileName(
			requestedName,
			Directory.EnumerateFiles(dir).Select(Path.GetFileName),
			out string matchedName))
		{
			error = "Fichier introuvable";
			return false;
		}
		string candidateRelative = Path.GetRelativePath(_root, Path.Combine(dir, matchedName));
		if (!SafeFilePath.TryResolve(_root, candidateRelative, out string verifiedPath, out error)
			|| !File.Exists(verifiedPath))
		{
			fullPath = null;
			error ??= "Fichier introuvable";
			return false;
		}
		fullPath = verifiedPath;
		error = null;
		return true;
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

	public void Delete(string fullPath)
	{
		if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
		{
			File.Delete(fullPath);
		}
	}
}
