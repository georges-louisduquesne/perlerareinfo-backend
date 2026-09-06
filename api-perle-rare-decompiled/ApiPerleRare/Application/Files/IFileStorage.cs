namespace ApiPerleRare.Application.Files;

/// <summary>
/// Port over the old-site folder. Application code never talks to the filesystem directly.
/// </summary>
public interface IFileStorage
{
	bool TryResolve(string relativeName, out string fullPath, out string error);

	void WriteAllBytes(string fullPath, byte[] content);

	bool Exists(string fullPath);

	byte[] ReadAllBytes(string fullPath);
}
