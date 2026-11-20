namespace ApiPerleRare.Application.Files;

/// <summary>
/// Port over the old-site folder. Application code never talks to the filesystem directly.
/// </summary>
public interface IFileStorage
{
	bool TryResolve(string relativeName, out string fullPath, out string error);

	bool TryResolveExisting(string relativeName, out string fullPath, out string error)
	{
		if (!TryResolve(relativeName, out fullPath, out error))
		{
			return false;
		}
		if (!Exists(fullPath))
		{
			error = "Fichier introuvable";
			return false;
		}
		return true;
	}

	void WriteAllBytes(string fullPath, byte[] content);

	bool Exists(string fullPath);

	byte[] ReadAllBytes(string fullPath);

	void Delete(string fullPath);
}
