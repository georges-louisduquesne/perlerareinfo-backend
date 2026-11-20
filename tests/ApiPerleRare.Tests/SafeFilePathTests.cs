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

	[Fact]
	public void Matches_existing_filename_across_nfc_nfd_and_lossy_question_marks()
	{
		Assert.False(SafeFilePath.NeedsExistingFallback("CV_414_abcd1234.pdf"));
		Assert.True(SafeFilePath.NeedsExistingFallback("CV_Anai?s_Delattaignant (1).pdf"));
		Assert.True(SafeFilePath.NeedsExistingFallback("CV_Ana\u00efs.pdf"));

		Assert.True(SafeFilePath.TryMatchExistingFileName(
			"CV_Anai?s_Delattaignant (1).pdf",
			new[] { "other.pdf", "CV_Anai\u0308s_Delattaignant (1).pdf" },
			out string nfdMatch));
		Assert.Equal("CV_Anai\u0308s_Delattaignant (1).pdf", nfdMatch);

		Assert.True(SafeFilePath.TryMatchExistingFileName(
			"CV_Ana?s.pdf",
			new[] { "CV_Ana\u00efs.pdf" },
			out string nonAsciiMatch));
		Assert.Equal("CV_Ana\u00efs.pdf", nonAsciiMatch);

		Assert.True(SafeFilePath.TryMatchExistingFileName(
			"CV_Ana\u00efs.pdf",
			new[] { "CV_Anai\u0308s.pdf" },
			out string nfcMatch));
		Assert.Equal("CV_Anai\u0308s.pdf", nfcMatch);

		Assert.False(SafeFilePath.TryMatchExistingFileName(
			"CV_123?.pdf",
			new[] { "CV_1234.pdf" },
			out _));

		Assert.False(SafeFilePath.TryMatchExistingFileName(
			"CV_Anai?s.pdf",
			new[] { "CV_Anai\u0308s.pdf", "CV_Anai\u0301s.pdf" },
			out _));
	}
}
