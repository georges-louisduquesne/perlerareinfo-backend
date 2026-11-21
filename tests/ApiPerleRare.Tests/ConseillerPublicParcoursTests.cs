using System.Linq;
using ApiPerleRare.Application.Conseillers;
using Xunit;

namespace ApiPerleRare.Tests;

public class ConseillerPublicParcoursTests
{
	private static readonly string Page = Dedent("""
		<p>[vc_row][vc_column align="left" width="1/2"][vc_single_image image="5791" link_type="no-link" alt="Acheter un bien immobilier à Paris" margin_top="50" margin_bottom="40"][/vc_column][vc_column width="1/2"][vc_column_text margin_top="50" margin_bottom="50"]</p>
		<p class="nom-chasseur-immobilier-noir"><strong>Georges-Louis Duquesne</strong><br />
		Dirigeant co-fondateur</p>
		<p>Georges-Louis possède une expérience de 20 années dans la banque et le financement immobilier des particuliers.</p>
		<p>En appliquant des méthodes d’organisation éprouvées en entreprise, il permet aux Conseillers <a href="https://perle-rare.com/fr/">Perle Rare</a> d’offrir à leurs clients un service moderne et fiable. Passionné par Paris, son histoire et ses quartiers, il vous communiquera son attachement pour la pierre. Il a co-fondé Perle Rare en octobre 2005.</p>
		<p>Il est notamment en charge des finances, du suivi des missions de recherche et des fonctions support. Il est le garant du bon déroulement des missions de recherche.</p>
		<p><a href="mailto:gld@perle-rare.com"> <img class="alignnone size-full wp-image-4359" src="https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid.png" alt="" width="30" height="30" /></a> <a href="https://www.linkedin.com/in/glduquesne/"><img class="alignnone size-full wp-image-4358" src="https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands.png" alt="" width="30" height="30" /></a>[/vc_column_text][/vc_column][/vc_row]</p>
		""");

	private static readonly string Stored = Dedent("""
		Georges-Louis possède une expérience de 20 années dans la banque et le financement immobilier des particuliers.

		En appliquant des méthodes d’organisation éprouvées en entreprise, il permet aux Conseillers Perle Rare d’offrir à leurs clients un service moderne et fiable. Passionné par Paris, son histoire et ses quartiers, il vous communiquera son attachement pour la pierre. Il a co-fondé Perle Rare en octobre 2005.

		Il est notamment en charge des finances, du suivi des missions de recherche et des fonctions support. Il est le garant du bon déroulement des missions de recherche.
		""");

	[Fact]
	public void Same_text_keeps_the_existing_link_and_the_contact_icons()
	{
		Assert.True(ConseillerPublicParcours.TryReplaceBiography(Page, Stored, out string updated));
		Assert.Equal(Page, updated);
	}

	[Fact]
	public void A_changed_paragraph_is_replaced_and_html_is_escaped()
	{
		string parcours = Stored.Replace("octobre 2005", "octobre 2006 <script>");
		Assert.True(ConseillerPublicParcours.TryReplaceBiography(Page, parcours, out string updated));
		Assert.Contains("octobre 2006 &lt;script&gt;", updated);
		Assert.DoesNotContain("<script>", updated);
		Assert.Contains("nom-chasseur-immobilier-noir", updated);
		Assert.Contains("mailto:gld@perle-rare.com", updated);
		Assert.Contains("expérience de 20 années", updated);
		Assert.DoesNotContain("octobre 2005", updated);
	}

	[Fact]
	public void Clearing_the_parcours_removes_only_the_biography()
	{
		Assert.True(ConseillerPublicParcours.TryReplaceBiography(Page, "", out string updated));
		Assert.Contains("nom-chasseur-immobilier-noir", updated);
		Assert.Contains("mailto:gld@perle-rare.com", updated);
		Assert.DoesNotContain("expérience de 20 années", updated);
		Assert.DoesNotContain("fonctions support", updated);
	}

	[Fact]
	public void Unknown_markup_is_left_untouched()
	{
		Assert.False(ConseillerPublicParcours.TryReplaceBiography("<p>Bonjour</p>", "Texte", out _));
	}

	[Fact]
	public void Any_perle_rare_advisor_page_is_publishable()
	{
		Assert.True(ConseillerPublicParcours.TryPerleRareSlug(
			"https://perle-rare.com/fr/georges-louis-duquesne-dirigeant-co-fondateur-perle-rare-chasseur-immobilier/",
			out string slug));
		Assert.Equal("georges-louis-duquesne-dirigeant-co-fondateur-perle-rare-chasseur-immobilier", slug);
		Assert.True(ConseillerPublicParcours.TryPerleRarePage(
			"https://perle-rare.com/en/celine-thieriet-director/",
			out string language,
			out string englishSlug));
		Assert.Equal("en", language);
		Assert.Equal("celine-thieriet-director", englishSlug);
		Assert.True(ConseillerPublicParcours.TryPerleRareSlug("https://perle-rare.com/fr/autre-conseiller/", out _));
		Assert.False(ConseillerPublicParcours.TryPerleRareSlug("https://example.com/fr/georges/", out _));
		Assert.False(ConseillerPublicParcours.TryPerleRareSlug("https://perle-rare.com/fr/a/b/", out _));
		Assert.False(ConseillerPublicParcours.TryPerleRareSlug("http://perle-rare.com/fr/autre-conseiller/", out _));
		Assert.False(ConseillerPublicParcours.TryPerleRareSlug("javascript:alert(1)", out _));
		Assert.False(ConseillerPublicParcours.ShouldPublish("https://perle-rare.com/fr/a/", "meme", "https://perle-rare.com/fr/a/", "meme"));
		Assert.True(ConseillerPublicParcours.ShouldPublish("https://perle-rare.com/fr/a/", "ancien", "https://perle-rare.com/fr/a/", "nouveau"));
	}

	[Fact]
	public void A_new_linkedin_replaces_only_the_contact_icon_href()
	{
		const string next = "https://www.linkedin.com/in/autre-profil/";
		Assert.True(ConseillerPublicParcours.TryReplaceLinkedIn(Page, next, out string updated));
		Assert.Contains("href=\"" + next + "\"", updated);
		Assert.DoesNotContain("https://www.linkedin.com/in/glduquesne/", updated);
		Assert.Contains("linkedin-brands.png", updated);
		Assert.Contains("mailto:gld@perle-rare.com", updated);
		Assert.Contains("expérience de 20 années", updated);
		Assert.Contains("nom-chasseur-immobilier-noir", updated);
	}

	[Fact]
	public void The_same_linkedin_leaves_the_page_unchanged()
	{
		Assert.True(ConseillerPublicParcours.TryReplaceLinkedIn(Page, "https://www.linkedin.com/in/glduquesne/", out string updated));
		Assert.Equal(Page, updated);
		Assert.False(ConseillerPublicParcours.ShouldPublishLinkedIn(
			"https://www.linkedin.com/in/glduquesne/",
			"https://www.linkedin.com/in/glduquesne/"));
		Assert.True(ConseillerPublicParcours.ShouldPublishLinkedIn(
			"https://www.linkedin.com/in/glduquesne/",
			"https://www.linkedin.com/in/autre/"));
	}

	[Fact]
	public void Clearing_linkedin_removes_the_icon_and_a_later_value_publishes_it_again()
	{
		Assert.True(ConseillerPublicParcours.TryReplaceLinkedIn(Page, "", out string cleared));
		Assert.DoesNotContain("linkedin.com", cleared);
		Assert.Contains("mailto:gld@perle-rare.com", cleared);
		Assert.Contains("expérience de 20 années", cleared);

		const string next = "https://fr.linkedin.com/in/glduquesne/";
		Assert.True(ConseillerPublicParcours.TryReplaceLinkedIn(cleared, next, out string restored));
		Assert.Contains("href=\"" + next + "\"", restored);
		Assert.Contains("linkedin-brands.png", restored);
		Assert.Contains("mailto:gld@perle-rare.com", restored);
	}

	[Fact]
	public void A_sector_card_updates_only_that_advisors_linkedin()
	{
		const string next = "https://www.linkedin.com/in/fabiola-hemmet-crm/";
		LinkedInCardUpdate result = ConseillerPublicParcours.ReplaceLinkedInNearProfile(
			Cards,
			"https://perle-rare.com/fr/fabiola-hemmet-conseiller-personnel-perle-rare/",
			next,
			out string updated);
		Assert.Equal(LinkedInCardUpdate.Updated, result);
		Assert.Contains("href=\"" + next + "\"", updated);
		Assert.DoesNotContain("https://www.linkedin.com/in/fabiola-hemmet/", updated);
		Assert.Contains("https://www.linkedin.com/in/anne-coste/", updated);
		Assert.Contains("Avant de rejoindre Perle Rare", updated);
		Assert.Contains("Artiste accomplie", updated);
		Assert.False(ConseillerPublicParcours.TryReplaceBiography(Cards, "Parcours complet qui ne doit pas ecraser l extrait.", out _));
	}

	[Fact]
	public void A_sector_card_without_linkedin_receives_the_icon_after_the_mail_link()
	{
		const string next = "https://www.linkedin.com/in/anne-coste/";
		string without = Cards.Replace(" <a href=\"" + next + "\"><img src=\"linkedin-brands-2.png\" /></a>", "");
		Assert.DoesNotContain("anne-coste/", without);
		LinkedInCardUpdate result = ConseillerPublicParcours.ReplaceLinkedInNearProfile(
			without,
			"https://perle-rare.com/fr/anne-coste-conseiller-personnel-perle-rare/",
			next,
			out string updated);
		Assert.Equal(LinkedInCardUpdate.Updated, result);
		Assert.Contains("mailo:aco@perle-rare.com", updated);
		Assert.Contains("href=\"" + next + "\"", updated);
		Assert.Contains("linkedin-brands-2.png", updated);
		Assert.Contains("https://www.linkedin.com/in/fabiola-hemmet/", updated);
	}

	[Fact]
	public void Clearing_linkedin_on_a_sector_card_removes_only_that_icon()
	{
		LinkedInCardUpdate result = ConseillerPublicParcours.ReplaceLinkedInNearProfile(
			Cards,
			"https://perle-rare.com/fr/fabiola-hemmet-conseiller-personnel-perle-rare/",
			"",
			out string updated);
		Assert.Equal(LinkedInCardUpdate.Updated, result);
		Assert.DoesNotContain("https://www.linkedin.com/in/fabiola-hemmet/", updated);
		Assert.Contains("mailo:fh@perle-rare.com", updated);
		Assert.Contains("https://www.linkedin.com/in/anne-coste/", updated);
		Assert.Contains("Avant de rejoindre Perle Rare", updated);
	}

	[Fact]
	public void A_broken_card_href_is_replaced_by_the_crm_linkedin()
	{
		const string broken = "https://www.linkedin.com/in/constance-perrin-471b0540/https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands.png";
		string page = Cards.Replace("https://www.linkedin.com/in/fabiola-hemmet/", broken);
		const string next = "https://www.linkedin.com/in/constance-perrin-471b0540/";
		LinkedInCardUpdate result = ConseillerPublicParcours.ReplaceLinkedInNearProfile(
			page,
			"https://perle-rare.com/fr/fabiola-hemmet-conseiller-personnel-perle-rare/",
			next,
			out string updated);
		Assert.Equal(LinkedInCardUpdate.Updated, result);
		Assert.Contains("href=\"" + next + "\"", updated);
		Assert.DoesNotContain(broken, updated);
		Assert.Contains("linkedin-brands-2.png", updated);
	}

	[Fact]
	public void An_ambiguous_card_is_not_rewritten()
	{
		string page = Cards.Replace(
			"https://www.linkedin.com/in/fabiola-hemmet/",
			"https://www.linkedin.com/in/fabiola-hemmet/\"></a> <a href=\"https://www.linkedin.com/in/autre/");
		LinkedInCardUpdate result = ConseillerPublicParcours.ReplaceLinkedInNearProfile(
			page,
			"https://perle-rare.com/fr/fabiola-hemmet-conseiller-personnel-perle-rare/",
			"https://www.linkedin.com/in/fabiola-hemmet/",
			out string updated);
		Assert.Equal(LinkedInCardUpdate.Unsafe, result);
		Assert.Null(updated);
	}

	[Fact]
	public void A_non_linkedin_address_is_not_written_on_the_page()
	{
		Assert.False(ConseillerPublicParcours.TryReplaceLinkedIn(Page, "https://example.com/in/gld", out _));
		Assert.False(ConseillerPublicParcours.TryReplaceLinkedIn(Page, "javascript:alert(1)", out _));
		Assert.False(ConseillerPublicParcours.TryReplaceLinkedIn(Page, "https://linkedin.com.evil.test/in/gld", out _));
		Assert.False(ConseillerPublicParcours.TryReplaceLinkedIn("<p>Bonjour</p>", "https://www.linkedin.com/in/glduquesne/", out _));
	}

	private static readonly string Cards = Dedent("""
		<p class="nom-chasseur-immobilier"><strong>Fabiola Hemmet,</strong><br />
		Conseiller personnel</p>
		<p>Avant de rejoindre Perle Rare, Fabiola a travaillé dans la communication.</p>
		<p><a class="lien-noir-bis" href="https://perle-rare.com/fr/fabiola-hemmet-conseiller-personnel-perle-rare/">En savoir plus</a></p>
		<p><a href="mailo:fh@perle-rare.com"> <img src="envelope.png" /></a> <a href="https://www.linkedin.com/in/fabiola-hemmet/"><img src="linkedin-brands-2.png" /></a></p>
		<p class="nom-chasseur-immobilier"><strong>Anne Coste,</strong><br />
		Conseiller personnel</p>
		<p>Artiste accomplie et membre du duo musical.</p>
		<p><a class="lien-noir-bis" href="https://perle-rare.com/fr/anne-coste-conseiller-personnel-perle-rare/">En savoir plus</a></p>
		<p><a href="mailo:aco@perle-rare.com"> <img src="envelope.png" /></a> <a href="https://www.linkedin.com/in/anne-coste/"><img src="linkedin-brands-2.png" /></a></p>
		""");

	[Fact]
	public void A_longer_slug_is_not_treated_as_the_same_advisor()
	{
		string page = Cards.Replace(
			"fabiola-hemmet-conseiller-personnel-perle-rare",
			"fabiola-hemmet-conseiller-personnel-perle-rare-extra");
		LinkedInCardUpdate result = ConseillerPublicParcours.ReplaceLinkedInNearProfile(
			page,
			"https://perle-rare.com/fr/fabiola-hemmet-conseiller-personnel-perle-rare/",
			"https://www.linkedin.com/in/autre/",
			out _);
		Assert.Equal(LinkedInCardUpdate.Absent, result);
	}

	private static string Dedent(string value)
	{
		return string.Join("\n", value.Replace("\r\n", "\n").Split('\n').Select((string line) => line.TrimStart('\t', ' ')));
	}
}
