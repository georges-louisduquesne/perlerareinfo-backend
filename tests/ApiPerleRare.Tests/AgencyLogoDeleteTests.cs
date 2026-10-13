using System.Collections.Generic;
using ApiPerleRare.Application.Files;
using Xunit;

namespace ApiPerleRare.Tests;

public class AgencyLogoDeleteTests
{
	[Fact]
	public void Clear_removes_every_extension_for_that_agency_only()
	{
		var storage = new MemoryFileStorage();
		storage.Allow("logos/agences/6962.png", "/tmp/logos/agences/6962.png");
		storage.Allow("logos/agences/6962.jpg", "/tmp/logos/agences/6962.jpg");
		storage.Allow("logos/agences/6963.png", "/tmp/logos/agences/6963.png");
		storage.Files["/tmp/logos/agences/6962.png"] = new byte[] { 1 };
		storage.Files["/tmp/logos/agences/6962.jpg"] = new byte[] { 2 };
		storage.Files["/tmp/logos/agences/6963.png"] = new byte[] { 3 };

		AgencyLogoAddress.DeleteAgencyFiles(storage, 6962, null);

		Assert.False(storage.Files.ContainsKey("/tmp/logos/agences/6962.png"));
		Assert.False(storage.Files.ContainsKey("/tmp/logos/agences/6962.jpg"));
		Assert.Equal(new byte[] { 3 }, storage.Files["/tmp/logos/agences/6963.png"]);
	}

	[Fact]
	public void Replace_keeps_the_new_extension_and_drops_the_previous_file()
	{
		var storage = new MemoryFileStorage();
		storage.Allow("logos/agences/6962.png", "/tmp/logos/agences/6962.png");
		storage.Allow("logos/agences/6962.jpg", "/tmp/logos/agences/6962.jpg");
		storage.Files["/tmp/logos/agences/6962.png"] = new byte[] { 1 };
		storage.Files["/tmp/logos/agences/6962.jpg"] = new byte[] { 9 };

		AgencyLogoAddress.DeleteAgencyFiles(storage, 6962, "logos/agences/6962.jpg");

		Assert.False(storage.Files.ContainsKey("/tmp/logos/agences/6962.png"));
		Assert.Equal(new byte[] { 9 }, storage.Files["/tmp/logos/agences/6962.jpg"]);
	}

	private sealed class MemoryFileStorage : IFileStorage
	{
		public Dictionary<string, string> Resolutions { get; } = new();

		public Dictionary<string, byte[]> Files { get; } = new();

		public void Allow(string relative, string fullPath) => Resolutions[relative] = fullPath;

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
