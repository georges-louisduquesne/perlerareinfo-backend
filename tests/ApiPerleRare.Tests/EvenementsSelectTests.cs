using System.Linq;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Predicates;
using Xunit;

namespace ApiPerleRare.Tests;

public class EvenementsSelectTests
{
	[Fact]
	public void Select_skips_unknown_event_fields_instead_of_400()
	{
		EvenementsEx[] rows =
		[
			new EvenementsEx
			{
				ERefEvenement = 1,
				EPropertyId = "73665ae0-a958-11f1-8233-8ba5573284a8",
				EStatut = 1,
				ECr = "",
				ENomContact = "",
				ETexte = ""
			}
		];
		EvenementsEx[] selected = EFHelper<EvenementsEx>.Apply(
			rows.AsQueryable(),
			null,
			null,
			0,
			0,
			"eRefEvenement,eAppointmentId,ePropertyId,ePcId,eRefAnnAgc").ToArray();
		Assert.Single(selected);
		Assert.Equal(1, selected[0].ERefEvenement);
		Assert.Equal("73665ae0-a958-11f1-8233-8ba5573284a8", selected[0].EPropertyId);
	}

	[Fact]
	public void Where_accepts_quoted_property_guids()
	{
		IValue parsed = Parser.Parse(
			"eStatut eq 1 and (ePropertyId eq '73665ae0-a958-11f1-8233-8ba5573284a8' or ePropertyId eq 'cdb1de40-a906-11f1-8233-8ba5573284a8')");
		Assert.NotNull(parsed);
	}

	[Fact]
	public void Where_accepts_sql_column_names_for_statut_and_property_id()
	{
		EvenementsEx[] rows =
		[
			new EvenementsEx
			{
				ERefEvenement = 1,
				EPropertyId = "f5093ca0-3c32-11f0-a032-a1ee9ddfcd69",
				EStatut = 1,
				ECr = "",
				ENomContact = "",
				ETexte = ""
			},
			new EvenementsEx
			{
				ERefEvenement = 2,
				EPropertyId = "f5093ca0-3c32-11f0-a032-a1ee9ddfcd69",
				EStatut = 0,
				ECr = "",
				ENomContact = "",
				ETexte = ""
			}
		];
		EvenementsEx[] selected = EFHelper<EvenementsEx>.Apply(
			rows.AsQueryable(),
			"E_Statut eq 1 and (E_PropertyId eq 'f5093ca0-3c32-11f0-a032-a1ee9ddfcd69' or E_PropertyId eq '9486d4c0-0f2b-11f0-9ae8-1957f5561889')",
			null,
			2000,
			0,
			null).ToArray();
		Assert.Single(selected);
		Assert.Equal(1, selected[0].ERefEvenement);
	}
}
