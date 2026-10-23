using System;
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
		Assert.Equal("Prépa compromis.doc", ModeleFichierNames.DownloadName("Prépa compromis", false));
		Assert.Equal("RIB.doc", ModeleFichierNames.DownloadName("  RIB  ", false));
		Assert.Equal("modele.doc", ModeleFichierNames.DownloadName("   ", false));
		Assert.Equal("application/pdf", ModeleFichierNames.ContentType(true));
		Assert.Equal("application/msword", ModeleFichierNames.ContentType(false));
	}

	[Fact]
	public void Catalogue_reads_file_flags_without_the_blobs()
	{
		Assert.Contains("MC_Fichier_doc IS NOT NULL", ModeleCatalogueQuery.Sql);
		Assert.Contains("MC_Fichier_pdf IS NOT NULL", ModeleCatalogueQuery.Sql);
		Assert.DoesNotContain("MC_Fichier_doc,", ModeleCatalogueQuery.Sql);
		ModeleCatalogueRow row = ModeleCatalogueQuery.FromValues(40, 4, "Divers", "Candidature", "Candidature", 1, 0);
		Assert.Equal(40, row.Ref);
		Assert.Equal((short)4, row.Categorie);
		Assert.Equal("Divers", row.CategorieNom);
		Assert.True(row.HasDoc);
		Assert.False(row.HasPdf);
		Assert.False(ModeleCatalogueQuery.Flag(DBNull.Value));
		Assert.True(ModeleCatalogueQuery.Flag(true));
	}
}
