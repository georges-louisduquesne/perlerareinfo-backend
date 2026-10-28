using System.Collections.Generic;
using ApiPerleRare.Application.Files;
using Xunit;

namespace ApiPerleRare.Tests;

public class ConseillerPhotoPathTests
{
	[Fact]
	public void Portrait_path_stays_inside_cp_files_and_fits_CP_Photo()
	{
		string path = ConseillerPhotoPath.Relative(194, "jpg");
		Assert.Equal("cp_files/194.jpg", path);
		Assert.StartsWith("cp_files/", path);
		Assert.True(ConseillerPhotoPath.Fits(path));
		Assert.True(path.Length <= ConseillerPhotoPath.MaxLength);
		Assert.Equal(100, ConseillerPhotoPath.MaxLength);
		Assert.False(ConseillerPhotoPath.Fits(new string('a', 101)));
		Assert.False(ConseillerPhotoPath.Fits(""));
		Assert.True(ConseillerPhotoPath.IsOwned("cp_files/photo.jpeg"));
		Assert.False(ConseillerPhotoPath.IsOwned("https://cdn.example/a.jpg"));
		Assert.False(ConseillerPhotoPath.IsOwned("cp_files/../secret.jpg"));
		Assert.False(ConseillerPhotoPath.IsOwned("logos/agences/12.png"));
		Assert.False(ConseillerPhotoPath.IsOwned(""));
	}

	[Fact]
	public void Delete_removes_only_this_conseiller_files_under_cp_files()
	{
		MemoryStorage storage = new MemoryStorage();
		storage.Files.Add("cp_files/12.jpg");
		storage.Files.Add("cp_files/12.png");
		storage.Files.Add("cp_files/portrait.png");
		storage.Files.Add("cp_files/99.jpg");
		storage.Files.Add("logos/agences/12.png");
		ImportConseillerPhotoUseCase.DeleteOwnedFiles(storage, 12, "cp_files/portrait.png", "cp_files/12.jpg");
		Assert.Contains("cp_files/12.jpg", storage.Files);
		Assert.DoesNotContain("cp_files/12.png", storage.Files);
		Assert.DoesNotContain("cp_files/portrait.png", storage.Files);
		Assert.Contains("cp_files/99.jpg", storage.Files);
		Assert.Contains("logos/agences/12.png", storage.Files);
	}

	private sealed class MemoryStorage : IFileStorage
	{
		public HashSet<string> Files { get; } = new HashSet<string>();

		public bool TryResolve(string relativeName, out string fullPath, out string error)
		{
			error = null;
			fullPath = (relativeName ?? "").Replace('\\', '/').Trim().TrimStart('/');
			return fullPath.Length > 0 && !fullPath.Contains("..");
		}

		public void WriteAllBytes(string fullPath, byte[] content)
		{
			Files.Add(fullPath);
		}

		public bool Exists(string fullPath)
		{
			return Files.Contains(fullPath);
		}

		public byte[] ReadAllBytes(string fullPath)
		{
			return System.Array.Empty<byte>();
		}

		public void Delete(string fullPath)
		{
			Files.Remove(fullPath);
		}
	}
}
