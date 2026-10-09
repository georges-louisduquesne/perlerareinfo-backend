using System.Linq;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using Xunit;

namespace ApiPerleRare.Tests;

public class PersonLookupReaderTests
{
	[Theory]
	[InlineData("Sophia", "ESCHEU", "Sophia Escheu", true)]
	[InlineData("Sophia", "ESCHEU", "Sofya Dymchenko", false)]
	[InlineData("Jean", "Dupont", "Jean Dupont", true)]
	[InlineData("Jean", "Dupont", "Jean-Baptiste Dupont", false)]
	[InlineData("Jean", "Dupont", "Jean Marc Dupont", false)]
	public void Seul_le_prenom_et_le_nom_exacts_comptent(string prenom, string nom, string candidate, bool expected)
	{
		Assert.Equal(expected, PersonLookupReader.IsExactPersonName(prenom, nom, candidate));
	}

	[Fact]
	public void Lit_le_jeton_Apify_dans_le_fichier_ou_un_profil()
	{
		Assert.Equal("abc", PersonLookupController.TokenFromAuthJson("{\"token\":\" abc \",\"username\":\"x\"}"));
		Assert.Equal("nested", PersonLookupController.TokenFromAuthJson("{\"secretsBackend\":\"keyring\",\"profiles\":{\"default\":{\"token\":\"nested\"}}}"));
		Assert.Null(PersonLookupController.TokenFromAuthJson("{\"secretsBackend\":\"keyring\",\"username\":\"x\"}"));
	}

	[Fact]
	public void Lit_le_profil_Sophia_et_garde_un_nom_different()
	{
		const string json = """
		[
		  {"name":"Sophia Escheu","headline":"Enabling enterprises","about":"AI in Physics","linkedinUrl":"https://www.linkedin.com/in/sophia-escheu-40069812b","photo":"https://media.licdn.com/photo.jpg","location":"Paris","company":"Mistral","school":"CDTM"},
		  {"name":"Sofya Dymchenko","headline":"Other","linkedinUrl":"https://www.linkedin.com/in/sofya","company":"Emmi"}
		]
		""";

		var people = PersonLookupReader.ReadLinkedIn(json);
		var exact = people.Where(person => PersonLookupReader.IsExactPersonName("Sophia", "ESCHEU", person.Name)).ToList();

		Assert.Equal(2, people.Count);
		Assert.Single(exact);
		Assert.Equal("https://media.licdn.com/photo.jpg", exact[0].Photo);
		Assert.Equal("Mistral", exact[0].Company);
		Assert.Equal("CDTM", exact[0].School);
	}

	[Fact]
	public void Le_profil_detaille_apporte_la_photo_sans_ecarter_les_autres_noms()
	{
		var light = PersonLookupReader.ReadLinkedIn("""
		[{"name":"Sophia Escheu","linkedinUrl":"https://www.linkedin.com/in/sophia-escheu-40069812b","company":"Mistral"},{"name":"Sofya Dymchenko","linkedinUrl":"https://www.linkedin.com/in/sofya"}]
		""");
		var full = PersonLookupReader.ReadLinkedIn("""
		[{"name":"Sophia Escheu","linkedinUrl":"https://www.linkedin.com/in/sophia-escheu-40069812b","photo":"https://media.licdn.com/photo.jpg","about":"AI in Physics","company":"Mistral"}]
		""");

		var merged = PersonLookupReader.Merge(light, full);

		Assert.Equal(2, merged.Count);
		Assert.Equal("https://media.licdn.com/photo.jpg", merged[0].Photo);
		Assert.Equal("Sofya Dymchenko", merged[1].Name);
	}
}
