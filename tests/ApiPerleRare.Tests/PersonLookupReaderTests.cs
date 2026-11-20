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
	public void La_recherche_Google_a_le_temps_d_aboutir()
	{
		Assert.Equal(120, PersonLookupController.HttpTimeoutSeconds);
		Assert.Equal(90, PersonLookupController.GoogleSearchTimeoutSeconds);
		Assert.True(PersonLookupController.HttpTimeoutSeconds > PersonLookupController.GoogleSearchTimeoutSeconds);
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

	[Fact]
	public void Nettoie_et_structure_le_resume_ia_Google()
	{
		const string googleJson = """
		[
		  {
		    "aiOverview": {
		      "type": "live",
		      "content": "Anastasia Plasmans est une professionnelle française active en finance et agronomie.\n- **Formation** : Diplômée de l'ISA Lille et de l'IAE Lille.\n- Expérience professionnelle : Contrôle de gestion chez Andros & Cie.\nProfils et liens publics\n- Anastasia Plasmans - LinkedIn : son profil.\nSouhaitez-vous en savoir plus sur un aspect particulier de son parcours ?"
		    }
		  }
		]
		""";

		var parsed = PersonLookupReader.ParseAiSummary(PersonLookupReader.ReadAiSummary(googleJson));

		Assert.NotNull(parsed);
		Assert.Contains("professionnelle française", parsed.Intro);
		Assert.DoesNotContain("Souhaitez-vous", parsed.Text);
		Assert.DoesNotContain("Profils et liens publics", parsed.Text);
		Assert.Equal(2, parsed.Points.Count);
		Assert.Equal("Formation", parsed.Points[0].Label);
		Assert.Contains("ISA Lille", parsed.Points[0].Text);
		Assert.Equal("Expérience professionnelle", parsed.Points[1].Label);
		Assert.Contains("Andros", parsed.Points[1].Text);
	}

	[Fact]
	public void Supprime_la_relance_Precisez_du_resume_ia()
	{
		const string raw = "Anastasia Plasmans est active en région parisienne.\n- Activité : Contrôle de gestion.\nPrécisez ce que vous recherchez concernant Anastasia Plasmans.";
		var parsed = PersonLookupReader.ParseAiSummary(raw);

		Assert.NotNull(parsed);
		Assert.DoesNotContain("Précisez ce que vous recherchez", parsed.Text);
		Assert.Single(parsed.Points);
	}

	[Fact]
	public void Extrait_LinkedIn_depuis_Google_et_ecarte_le_bruit()
	{
		const string googleJson = """
		[
		  {
		    "organicResults": [
		      {
		        "title": "Anastasia Plasmans - Contrôle de gestion chez Andros & cie",
		        "websiteTitle": "LinkedIn · Anastasia Plasmans",
		        "url": "https://fr.linkedin.com/in/anastasia-plasmans-38107416a",
		        "description": "Issue du milieu agricole, contrôle de gestion chez Andros."
		      },
		      {
		        "title": "Aurore Keunebroek - Freelance en illustration",
		        "websiteTitle": "LinkedIn · Aurore Keunebroek",
		        "url": "https://fr.linkedin.com/in/aurore-keunebroek-57015b17a",
		        "description": "Freelance."
		      },
		      {
		        "title": "Post LinkedIn | Anastasia Plasmans",
		        "websiteTitle": "LinkedIn",
		        "url": "https://fr.linkedin.com/posts/anastasia-plasmans-38107416a_post",
		        "description": "Un post."
		      }
		    ]
		  },
		  {
		    "organicResults": [
		      {
		        "title": "Anastasia Plasmans - Contrôle de gestion chez Andros & cie | LinkedIn",
		        "url": "https://www.linkedin.com/in/anastasia-plasmans-38107416a/",
		        "description": "ANDROS IAE Lille. Paris et périphérie.",
		        "personalInfo": {
		          "location": "Paris et périphérie",
		          "jobTitle": "Contrôle de gestion",
		          "companyName": "Andros & cie"
		        }
		      }
		    ]
		  }
		]
		""";

		var profiles = PersonLookupReader.ReadLinkedInFromGoogle(googleJson, "Anastasia", "PLASMANS");

		Assert.Single(profiles);
		Assert.Equal("Anastasia Plasmans", profiles[0].Name);
		Assert.Equal("Contrôle de gestion chez Andros & cie", profiles[0].Headline);
		Assert.Equal("Andros & cie", profiles[0].Company);
		Assert.Equal("Paris et périphérie", profiles[0].Location);
	}

	[Fact]
	public void Filtre_les_resultats_web_hors_personne_et_dedoublonne()
	{
		const string googleJson = """
		[
		  {
		    "organicResults": [
		      {
		        "title": "Melanie Plasmans : DU NID D'HIRONDELLES",
		        "websiteTitle": "Pappers",
		        "url": "https://www.pappers.fr/dirigeant/melanie_plasmans_1978-05",
		        "description": "Date de naissance : 05/1978."
		      },
		      {
		        "title": "Anastasia PLASMANS : ESCAPE GAME APERO PARIS",
		        "websiteTitle": "Pappers",
		        "url": "https://www.pappers.fr/dirigeant/anastasia_plasmans_1998-10",
		        "description": "Anastasia PLASMANS. Date de naissance : 10/1998."
		      },
		      {
		        "title": "Anastasia Plasmans - Contrôle de gestion chez Andros & cie",
		        "websiteTitle": "LinkedIn",
		        "url": "https://fr.linkedin.com/in/anastasia-plasmans-38107416a",
		        "description": "Profil LinkedIn."
		      }
		    ]
		  },
		  {
		    "organicResults": [
		      {
		        "title": "Anastasia PLASMANS : ESCAPE GAME APERO PARIS",
		        "websiteTitle": "Pappers",
		        "url": "https://www.pappers.fr/dirigeant/anastasia_plasmans_1998-10/",
		        "description": "Doublon avec slash."
		      },
		      {
		        "title": "Bureau – Triathlon - Le Stade Français",
		        "websiteTitle": "stadefrancais.com",
		        "url": "https://www.stadefrancais.com/sections/triathlon/bureau/",
		        "description": "ANASTASIA PLASMANS. Digital et web."
		      }
		    ]
		  }
		]
		""";

		var hits = PersonLookupReader.ReadGoogleForPerson(googleJson, "Anastasia", "PLASMANS");

		Assert.Equal(2, hits.Count);
		Assert.Equal("https://www.pappers.fr/dirigeant/anastasia_plasmans_1998-10", hits[0].Url);
		Assert.Equal("Pappers", hits[0].Site);
		Assert.Equal("https://www.stadefrancais.com/sections/triathlon/bureau/", hits[1].Url);
	}

	[Fact]
	public void Filtre_et_classe_les_photos_Google_Images_et_complete_le_portrait_LinkedIn()
	{
		const string imagesJson = """
		[
		  {
		    "title": "Bureau – Triathlon",
		    "imageUrl": "https://www.stadefrancais.com/photo.jpg",
		    "thumbnailUrl": "https://encrypted-tbn0.gstatic.com/images?q=tbn:triathlon",
		    "imageWidth": 479,
		    "imageHeight": 479,
		    "source": "Le Stade Français",
		    "domain": "www.stadefrancais.com",
		    "link": "https://www.stadefrancais.com/sections/triathlon/bureau/"
		  },
		  {
		    "title": "Anastasia Plasmans - Contrôle de gestion chez Andros & cie | LinkedIn",
		    "imageUrl": "https://media.licdn.com/dms/image/v2/profile-displayphoto-shrink_200_200/x",
		    "thumbnailUrl": "https://encrypted-tbn0.gstatic.com/images?q=tbn:linkedin",
		    "imageWidth": 200,
		    "imageHeight": 200,
		    "source": "LinkedIn",
		    "domain": "fr.linkedin.com",
		    "link": "https://fr.linkedin.com/in/anastasia-plasmans-38107416a"
		  },
		  {
		    "title": "La Coupe du monde de biathlon reprend aujourd'hui",
		    "imageUrl": "https://lookaside.fbsbx.com/biathlon",
		    "thumbnailUrl": "https://encrypted-tbn0.gstatic.com/images?q=tbn:biathlon",
		    "imageWidth": 1000,
		    "imageHeight": 1000,
		    "source": "Facebook",
		    "domain": "www.facebook.com",
		    "link": "https://www.facebook.com/lachainelequipe/posts/biathlon"
		  }
		]
		""";

		var knownUrls = new[] { "https://www.stadefrancais.com/sections/triathlon/bureau/" };
		var photos = PersonLookupReader.ReadGoogleImages(imagesJson, "Anastasia", "PLASMANS", knownUrls);

		Assert.Equal(2, photos.Count);
		Assert.True(photos[0].IsLinkedInPortrait);
		Assert.Equal("https://encrypted-tbn0.gstatic.com/images?q=tbn:linkedin", photos[0].ThumbnailUrl);
		Assert.Equal("Le Stade Français", photos[1].Source);

		var profiles = new[]
		{
			new PublicProfile { Name = "Anastasia Plasmans", Url = "https://fr.linkedin.com/in/anastasia-plasmans-38107416a" },
		};
		PersonLookupReader.AttachLinkedInPhotos(profiles, photos);
		Assert.Equal("https://encrypted-tbn0.gstatic.com/images?q=tbn:linkedin", profiles[0].Photo);
	}

	[Fact]
	public void Construit_les_trois_requetes_Google_pour_une_personne()
	{
		var queries = PersonLookupController.BuildGoogleQueries("Anastasia", "PLASMANS");
		var lines = queries.Split('\n');
		Assert.Equal(3, lines.Length);
		Assert.Equal("Anastasia PLASMANS", lines[0]);
		Assert.Equal("\"Anastasia PLASMANS\"", lines[1]);
		Assert.Equal("\"Anastasia PLASMANS\" site:linkedin.com/in", lines[2]);
	}
}
