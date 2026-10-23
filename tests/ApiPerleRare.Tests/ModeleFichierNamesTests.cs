using ApiPerleRare.Application.Modeles;
using Xunit;

namespace ApiPerleRare.Tests;

public class ModeleFichierNamesTests
{
	[Fact]
	public void TryResolve_accepts_doc_and_pdf_only()
	{
		Assert.True(ModeleFichierNames.TryResolve("pdf", out bool pdf));
		Assert.True(pdf);
		Assert.True(ModeleFichierNames.TryResolve("DOC", out bool doc));
		Assert.False(doc);
		Assert.False(ModeleFichierNames.TryResolve("docx", out _));
		Assert.False(ModeleFichierNames.TryResolve("", out _));
	}

	[Fact]
	public void DownloadName_strips_accents_and_keeps_the_extension()
	{
		Assert.Equal("RIB signe.pdf", ModeleFichierNames.DownloadName("RIB signé", true));
		Assert.Equal("MRA-E EN.doc", ModeleFichierNames.DownloadName("MRA-E EN", false));
		Assert.Equal("modele.doc", ModeleFichierNames.DownloadName("   ", false));
		Assert.Equal("application/pdf", ModeleFichierNames.ContentType(true));
		Assert.Equal("application/msword", ModeleFichierNames.ContentType(false));
	}
}
