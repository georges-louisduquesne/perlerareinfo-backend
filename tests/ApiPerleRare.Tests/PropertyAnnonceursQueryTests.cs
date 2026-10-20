using System;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApiPerleRare.Tests;

public class PropertyAnnonceursQueryTests
{
	private const string Where = "pAnnonceurs like \"%VISIMO%\"";

	[Fact]
	public void Select_without_ordered_key_fails_translation()
	{
		using ApplicationDbContext ctx = Create();
		Exception error = Record.Exception(() =>
			EFHelper<Property>.Apply(
				ctx.Property,
				Where,
				"pPropertyId",
				2000,
				0,
				"pAnnonceurs,pDateFin").ToQueryString());
		Assert.NotNull(error);
		Assert.Contains("could not be translated", error.ToString(), StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Select_with_ordered_key_translates()
	{
		using ApplicationDbContext ctx = Create();
		string sql = EFHelper<Property>.Apply(
			ctx.Property,
			Where,
			"pPropertyId",
			2000,
			0,
			"pPropertyId,pAnnonceurs,pDateFin").ToQueryString();
		Assert.Contains("P_Annonceurs", sql);
		Assert.Contains("P_PropertyId", sql);
		Assert.Contains("P_DateFin", sql);
		Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
	}

	private static ApplicationDbContext Create()
	{
		DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseMySql(
				"Server=127.0.0.1;Database=perle;User=x;Password=x",
				ServerVersion.Parse("10.5.23-mariadb"))
			.Options;
		return new ApplicationDbContext(options);
	}
}
