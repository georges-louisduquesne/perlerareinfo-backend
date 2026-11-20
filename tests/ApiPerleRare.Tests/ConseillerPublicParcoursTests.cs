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
	public void Only_a_perle_rare_advisor_page_is_publishable()
	{
		Assert.True(ConseillerPublicParcours.TryPerleRareSlug(
			"https://perle-rare.com/fr/georges-louis-duquesne-dirigeant-co-fondateur-perle-rare-chasseur-immobilier/",
			out string slug));
		Assert.Equal("georges-louis-duquesne-dirigeant-co-fondateur-perle-rare-chasseur-immobilier", slug);
		Assert.False(ConseillerPublicParcours.TryPerleRareSlug("https://example.com/fr/georges/", out _));
		Assert.False(ConseillerPublicParcours.TryPerleRareSlug("https://perle-rare.com/fr/autre-conseiller/", out _));
		Assert.False(ConseillerPublicParcours.TryPerleRareSlug("javascript:alert(1)", out _));
		Assert.True(ConseillerPublicParcours.IsGeorgesLouis(0));
		Assert.False(ConseillerPublicParcours.IsGeorgesLouis(12));
		Assert.False(ConseillerPublicParcours.ShouldPublish("https://perle-rare.com/fr/a/", "meme", "https://perle-rare.com/fr/a/", "meme"));
		Assert.True(ConseillerPublicParcours.ShouldPublish("https://perle-rare.com/fr/a/", "ancien", "https://perle-rare.com/fr/a/", "nouveau"));
	}

	private static string Dedent(string value)
	{
		return string.Join("\n", value.Replace("\r\n", "\n").Split('\n').Select((string line) => line.TrimStart('\t', ' ')));
	}
}
