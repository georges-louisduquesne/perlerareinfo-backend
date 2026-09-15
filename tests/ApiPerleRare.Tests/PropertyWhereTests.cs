using System;
using System.Linq;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using ApiPerleRare.Predicates;
using Xunit;

namespace ApiPerleRare.Tests;

public class PropertyWhereTests
{
	private const string RechercheWhere =
		"PState eq 1 and (PCp eq '94120' or PCp eq '94130' or PCp eq '94160' or PCp eq '94300') and PTypeTransaction eq 'A' and (PType eq 'Maison') and (PNbPieces eq 4 or PNbPieces eq 5 or PNbPieces ge 6) and (PNbChambres eq 3 or PNbChambres eq 4 or PNbChambres eq 5 or PNbChambres ge 6) and PSurface ge 90 and PPrix le 1300000 and PDateDebut ge 2026-08-16T19:25:05Z";

	[Fact]
	public void Parse_accepts_iso_datetime_with_utc_z()
	{
		IValue parsed = Parser.Parse("PDateDebut ge 2026-08-16T19:25:05Z");
		Assert.NotNull(parsed);
	}

	[Fact]
	public void Parse_accepts_odata_datetime_literal()
	{
		IValue parsed = Parser.Parse("PDateDebut ge datetime'2026-08-16T19:25:05Z'");
		Assert.NotNull(parsed);
	}

	[Fact]
	public void Parse_accepts_iso_datetime_with_milliseconds_and_z()
	{
		IValue parsed = Parser.Parse("PDateDebut ge 2026-08-16T19:25:05.000Z");
		Assert.NotNull(parsed);
	}

	[Fact]
	public void Apply_coerces_byte_and_double_property_filters()
	{
		Property[] rows =
		[
			new Property { PPropertyId = "ok", PNbPieces = 6, PSurface = 100, PPrix = 200000 },
			new Property { PPropertyId = "no", PNbPieces = 2, PSurface = 40, PPrix = 2000000 }
		];
		Property[] selected = EFHelper<Property>.Apply(
			rows.AsQueryable(),
			"PNbPieces ge 6 and PSurface ge 90 and PPrix le 1300000",
			null,
			0,
			0,
			null).ToArray();
		Assert.Single(selected);
		Assert.Equal("ok", selected[0].PPropertyId);
	}

	[Fact]
	public void Apply_filters_recherche_where_with_pieces_surface_price_and_date()
	{
		Property[] rows =
		[
			new Property
			{
				PPropertyId = "match",
				PState = 1,
				PCp = "94120",
				PTypeTransaction = "A",
				PType = "Maison",
				PNbPieces = 5,
				PNbChambres = 4,
				PSurface = 120,
				PPrix = 900000,
				PDateDebut = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc)
			},
			new Property
			{
				PPropertyId = "too-old",
				PState = 1,
				PCp = "94120",
				PTypeTransaction = "A",
				PType = "Maison",
				PNbPieces = 5,
				PNbChambres = 4,
				PSurface = 120,
				PPrix = 900000,
				PDateDebut = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc)
			}
		];

		Property[] selected = EFHelper<Property>.Apply(rows.AsQueryable(), RechercheWhere, null, 1, 0, null).ToArray();
		Assert.Single(selected);
		Assert.Equal("match", selected[0].PPropertyId);
	}
}
