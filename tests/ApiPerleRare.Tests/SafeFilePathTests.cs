using System.IO;
using ApiPerleRare.Helpers;
using Xunit;

namespace ApiPerleRare.Tests;

public class SafeFilePathTests
{
	[Fact]
	public void Rejects_path_traversal_and_php()
	{
		string root = Path.Combine(Path.GetTempPath(), "pr-safe-path");
		Directory.CreateDirectory(root);

		Assert.False(SafeFilePath.TryResolve(root, "../secret.txt", out _, out string traversal));
		Assert.Equal("Chemin interdit", traversal);

		Assert.False(SafeFilePath.TryResolve(root, "docs/x.php", out _, out string php));
		Assert.Equal("Extension interdite", php);

		Assert.False(SafeFilePath.TryResolve(root, "alone.txt", out _, out string folder));
		Assert.Equal("Il faut obligatoirement spécifier un dossier", folder);
	}

	[Fact]
	public void Accepts_relative_file_under_root()
	{
		string root = Path.Combine(Path.GetTempPath(), "pr-safe-path-ok");
		Directory.CreateDirectory(root);

		Assert.True(SafeFilePath.TryResolve(root, "docs/note.txt", out string full, out string error));
		Assert.Null(error);
		Assert.StartsWith(Path.GetFullPath(root), full);
		Assert.EndsWith(Path.Combine("docs", "note.txt"), full);
	}
}
