using System;
using System.Text.Json;
using ApiPerleRare.Models;
using ApiPerleRare.ScheduledTasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class TypeformPartialLeadMapperTests
{
	[Fact]
	public void NormalizePhone_converts_french_international()
	{
		Assert.Equal("0675900558", TypeformPartialLeadMapper.NormalizePhone("+33675900558"));
		Assert.Equal("0627109658", TypeformPartialLeadMapper.NormalizePhone("+33 6 27 10 96 58"));
	}

	[Fact]
	public void NormalizePhone_keeps_non_french_international()
	{
		Assert.Equal("+56933870121", TypeformPartialLeadMapper.NormalizePhone("+56 9 3387 0121"));
	}

	[Fact]
	public void ToParisWallClock_uses_summer_offset()
	{
		DateTime paris = TypeformPartialLeadMapper.ToParisWallClock(new DateTime(2026, 9, 27, 20, 27, 2, DateTimeKind.Utc));
		Assert.Equal(new DateTime(2026, 9, 27, 22, 27, 2), paris);
		Assert.Equal(DateTimeKind.Unspecified, paris.Kind);
	}

	[Fact]
	public void ToFormulaire_maps_partial_with_phone()
	{
		using JsonDocument doc = JsonDocument.Parse("""
		{
		  "staged_at": "2026-09-27T19:26:56Z",
		  "hidden": {
		    "utm_source": "google",
		    "utm_medium": "cpc",
		    "utm_campaign": "Achat-Vente",
		    "utm_term": "acheter appartement paris",
		    "utm_content": "Acheter Immobilier Paris"
		  },
		  "answers": [
		    { "field": { "ref": "1b79097e-2487-4141-bca5-6fe90e6674ad" }, "choice": { "label": "de 300.000 à 800.000 €" } },
		    { "field": { "ref": "49801432-858b-4e38-9d47-e442c2f0a9c8" }, "text": "2 pièces" },
		    { "field": { "ref": "cecdd17b-bdc5-4281-9271-bc8537f51bb2" }, "text": "Mathilde Nahon" },
		    { "field": { "ref": "201b290b-0b38-4898-8372-2252cd3db179" }, "phone_number": "+33695534218" }
		  ]
		}
		""");
		Formulaires form = TypeformPartialLeadMapper.ToFormulaire(doc.RootElement, "02");
		Assert.NotNull(form);
		Assert.Equal("Mathilde Nahon", form.FNom);
		Assert.Equal("0695534218", form.FTel);
		Assert.Equal("de 300.000 à 800.000 € 2 pièces", form.FTexte);
		Assert.Equal("a", form.FSouhaits);
		Assert.Equal((byte)1, form.FType);
		Assert.Equal((short)1, form.FStatut);
		Assert.Equal("02", form.FVariant);
		Assert.Equal("google", form.FUtmSource);
		Assert.Equal(new DateTime(2026, 9, 27, 21, 26, 56), form.FDate);
	}

	[Fact]
	public void ToFormulaire_skips_missing_phone_and_tests()
	{
		using JsonDocument noPhone = JsonDocument.Parse("""
		{ "staged_at": "2026-09-27T19:26:56Z", "answers": [ { "field": { "ref": "cecdd17b-bdc5-4281-9271-bc8537f51bb2" }, "text": "Anne" } ] }
		""");
		Assert.Null(TypeformPartialLeadMapper.ToFormulaire(noPhone.RootElement, "01"));
		using JsonDocument test = JsonDocument.Parse("""
		{
		  "staged_at": "2026-09-28T15:48:11Z",
		  "answers": [
		    { "field": { "ref": "cecdd17b-bdc5-4281-9271-bc8537f51bb2" }, "text": "test partiel 2" },
		    { "field": { "ref": "201b290b-0b38-4898-8372-2252cd3db179" }, "phone_number": "+33614585458" }
		  ]
		}
		""");
		Assert.Null(TypeformPartialLeadMapper.ToFormulaire(test.RootElement, "02"));
	}
}
