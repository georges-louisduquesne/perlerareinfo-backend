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

	[Fact]
	public void Profile_name_updates_only_strong_text_and_keeps_role_line()
	{
		Assert.Equal("Isabelle Chetochine", ConseillerPublicParcours.BuildFullName(" Isabelle ", " Chetochine "));
		Assert.True(ConseillerPublicParcours.TryReplaceProfileName(Page, "Georges-Louis Duquesne-Test", out string updated));
		Assert.Contains("<p class=\"nom-chasseur-immobilier-noir\"><strong>Georges-Louis Duquesne-Test</strong><br />\nDirigeant co-fondateur</p>", updated);
		Assert.Contains("expérience de 20 années", updated);
		Assert.False(ConseillerPublicParcours.TryReplaceProfileName(Page, "   ", out _));
		Assert.False(ConseillerPublicParcours.TryReplaceProfileName("<p>Bonjour</p>", "Jean Dupont", out _));
	}

	[Fact]
	public void French_job_title_replaces_only_the_line_under_the_name()
	{
		Assert.True(ConseillerPublicParcours.TryReplaceJobTitle(Page, " Directeur ", out string updated));
		Assert.Contains("<strong>Georges-Louis Duquesne</strong><br />\nDirecteur</p>", updated);
		Assert.DoesNotContain("Dirigeant co-fondateur", updated);
		Assert.Contains("mailto:gld@perle-rare.com", updated);
		Assert.False(ConseillerPublicParcours.TryReplaceJobTitle(Page, "   ", out _));
		Assert.False(ConseillerPublicParcours.TryReplaceJobTitle(Page, "Titre <b>interdit</b>", out _));
		Assert.True(ConseillerPublicParcours.TryReplaceJobTitle("<p class=\"nom-chasseur-immobilier-noir\"><strong>Jean</strong></p>", "Directeur", out string bare));
		Assert.Contains("<strong>Jean</strong> Directeur</p>", bare);
		Assert.True(ConseillerPublicParcours.TryReplaceJobTitle("<p class=\"nom-chasseur-immobilier-noir\"><strong>Catherine BLANCHARD</strong>\nPersonal consultant</p>", "Property hunter", out string english));
		Assert.Contains("<strong>Catherine BLANCHARD</strong>\nProperty hunter</p>", english);
		Assert.DoesNotContain("Personal consultant", english);
	}

	[Fact]
	public void Publish_button_rewrites_a_page_even_when_the_column_is_unchanged()
	{
		Assert.True(ConseillerPublicParcours.PublishEvenIfUnchanged(force: true, changed: false));
		Assert.False(ConseillerPublicParcours.PublishEvenIfUnchanged(force: false, changed: false));
		Assert.True(ConseillerPublicParcours.PublishEvenIfUnchanged(force: false, changed: true));
	}

	[Fact]
	public void English_raw_biography_replaces_a_single_word_and_keeps_the_mail_link()
	{
		string page = Dedent("""
			[vc_row][vc_column width="1/2"][vc_column_text]
			<p class="nom-chasseur-immobilier-noir"><strong>Catherine BLANCHARD</strong>
			Personal consultant</p>
			Our overqualified women !

			Catherine is a former specialist in change management.

			<a class="lien-noir" href="mailto:cb@perle-rare.com">cb@perle-rare.com</a>[/vc_column_text][/vc_column][/vc_row]
			""");
		string parcours = "Our overqualified woman !\n\nCatherine is a former specialist in change management.";
		Assert.True(ConseillerPublicParcours.TryReplaceBiography(page, parcours, out string updated));
		Assert.Contains("Our overqualified woman !", updated);
		Assert.DoesNotContain("women", updated);
		Assert.Contains("href=\"mailto:cb@perle-rare.com\"", updated);
		Assert.Contains("Personal consultant", updated);
	}

	[Fact]
	public void Public_profile_url_is_canonical_https()
	{
		Assert.True(PublicPageRepublish.TryProfileUrl("https://perle-rare.com/fr/anne-davoli-conseiller-personnel", out string url));
		Assert.Equal("https://perle-rare.com/fr/anne-davoli-conseiller-personnel/", url);
		Assert.False(PublicPageRepublish.TryProfileUrl("https://example.com/fr/anne/", out _));
		Assert.False(PublicPageRepublish.TryProfileUrl("javascript:alert(1)", out _));
	}

	[Fact]
	public void Profile_email_updates_icon_link_text_link_and_broken_hrefs()
	{
		Assert.True(ConseillerPublicParcours.TryReplaceEmail(Page, "georges-louis.duquesne@perle-rare.com", out string iconUpdated));
		Assert.Contains("href=\"mailto:georges-louis.duquesne@perle-rare.com\"", iconUpdated);
		Assert.Contains("envelope-solid.png", iconUpdated);
		Assert.Contains("https://www.linkedin.com/in/glduquesne/", iconUpdated);

		string textPage = Page.Replace(
			"<a href=\"mailto:gld@perle-rare.com\"> <img class=\"alignnone size-full wp-image-4359\" src=\"https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid.png\" alt=\"\" width=\"30\" height=\"30\" /></a>",
			"<a class=\"lien-noir\" href=\"mailto:georges-louis@perle-rare.com\">gld@perle-rare.com</a>");
		Assert.True(ConseillerPublicParcours.TryReplaceEmail(textPage, "georges-louis.duquesne@perle-rare.com", out string textUpdated));
		Assert.Contains("<a class=\"lien-noir\" href=\"mailto:georges-louis.duquesne@perle-rare.com\">georges-louis.duquesne@perle-rare.com</a>", textUpdated);

		Assert.True(ConseillerPublicParcours.TryReplaceEmail(RawMargotPage, "margot.saidy@perle-rare.com", out string fixedMissingScheme));
		Assert.Contains("href=\"mailto:margot.saidy@perle-rare.com\"", fixedMissingScheme);
		Assert.DoesNotContain("href=\"ms@perle-rare.com\"", fixedMissingScheme);
		Assert.Contains("envelope-solid-2.png", fixedMissingScheme);

		Assert.True(ConseillerPublicParcours.TryReplaceEmail(Page, "", out string cleared));
		Assert.DoesNotContain("mailto:", cleared);
		Assert.Contains("https://www.linkedin.com/in/glduquesne/", cleared);

		Assert.True(ConseillerPublicParcours.TryReplaceEmail(cleared, "gld@perle-rare.com", out string restored));
		Assert.Contains("href=\"mailto:gld@perle-rare.com\"", restored);
		Assert.Contains("envelope-solid.png", restored);

		Assert.False(ConseillerPublicParcours.TryReplaceEmail(Page, "pas-un-email", out _));
		Assert.False(ConseillerPublicParcours.TryReplaceEmail(Page, "a@b.com\" onclick=\"x", out _));
	}

	[Fact]
	public void Rsac_is_added_updated_and_removed_for_agent_commercial_only()
	{
		Assert.Equal("", ConseillerPublicParcours.EffectiveRsac("Associé", "2016AC00325"));
		Assert.Equal("2016AC00325", ConseillerPublicParcours.EffectiveRsac("Agent commercial", " 2016AC00325 "));
		Assert.Equal("", ConseillerPublicParcours.EffectiveRsac("Agent commercial", "  "));
		Assert.Null(ConseillerPublicParcours.EffectiveRsac("Agent commercial", "2016<script>"));

		Assert.True(ConseillerPublicParcours.TryReplaceRsac(Page, "fr", "Agent commercial", "2025AC00297", out string frAdded));
		Assert.Contains("<p class=\"rsac-conseiller\">Agent commercial inscrit au RSAC sous le numéro 2025AC00297</p>", frAdded);
		Assert.Contains("expérience de 20 années", frAdded);
		Assert.Contains("mailto:gld@perle-rare.com", frAdded);

		// Une mise à jour du parcours conserve la ligne RSAC existante.
		Assert.True(ConseillerPublicParcours.TryReplaceBiography(frAdded, "Nouveau parcours.", out string bioChanged));
		Assert.Contains("<p>Nouveau parcours.</p>", bioChanged);
		Assert.Contains("<p class=\"rsac-conseiller\">Agent commercial inscrit au RSAC sous le numéro 2025AC00297</p>", bioChanged);

		Assert.True(ConseillerPublicParcours.TryReplaceRsac(frAdded, "en", "Agent commercial", "2025AC00297", out string enUpdated));
		Assert.Contains("<p class=\"rsac-conseiller\">Commercial agent registered with the RSAC under number 2025AC00297</p>", enUpdated);
		Assert.DoesNotContain("Agent commercial inscrit", enUpdated);

		// Remplace aussi les anciennes lignes RSAC brutes (wpautop) ou en <span>.
		Assert.True(ConseillerPublicParcours.TryReplaceRsac(RawMargotPage, "fr", "Agent commercial", "2023AA00160", out string margotFixed));
		Assert.Contains("2023AA00160", margotFixed);
		Assert.DoesNotContain("2023AC00160", margotFixed);

		string spanRsac = Page.Replace(
			"<p>Il est notamment",
			"<span style=\"color: #0b0113;\">Agent commercial inscrit au RSAC de Paris sous le numéro 2019AC00183</span>\n<p>Il est notamment");
		Assert.True(ConseillerPublicParcours.TryReplaceRsac(spanRsac, "fr", "Associé", "2019AC00183", out string removed));
		Assert.DoesNotContain("RSAC", removed);
		Assert.Contains("expérience de 20 années", removed);
	}

	[Fact]
	public void Google_link_inserts_updates_encodes_ampersands_and_clears()
	{
		const string mapsUrl = "https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Laurence+Berry";
		Assert.True(ConseillerPublicParcours.TryReplaceGoogle(Page, mapsUrl, out string added));
		Assert.Contains("href=\"https://www.google.com/maps/search/?api=1&amp;query=Perle+Rare+-+Laurence+Berry\"", added);
		Assert.Contains("google-brands.png", added);
		Assert.Contains("https://www.linkedin.com/in/glduquesne/", added);
		Assert.True(added.IndexOf("linkedin.com") < added.IndexOf("google.com"));

		// Idempotent au second passage malgré le & encodé en &amp;.
		Assert.True(ConseillerPublicParcours.TryReplaceGoogle(added, mapsUrl, out string same));
		Assert.Equal(added, same);

		const string gPageUrl = "https://g.page/r/perle-rare-paris/review";
		Assert.True(ConseillerPublicParcours.TryReplaceGoogle(added, gPageUrl, out string updated));
		Assert.Contains("href=\"" + gPageUrl + "\"", updated);
		Assert.DoesNotContain("Laurence+Berry", updated);

		Assert.True(ConseillerPublicParcours.TryReplaceGoogle(updated, "", out string cleared));
		Assert.DoesNotContain("google-brands.png", cleared);
		Assert.DoesNotContain("g.page", cleared);
		Assert.Contains("https://www.linkedin.com/in/glduquesne/", cleared);

		Assert.False(ConseillerPublicParcours.TryReplaceGoogle(Page, "https://example.com/maps", out _));
		Assert.False(ConseillerPublicParcours.TryReplaceGoogle(Page, "https://google.com.evil.test/maps", out _));
		Assert.False(ConseillerPublicParcours.TryReplaceGoogle(Page, "javascript:alert(1)", out _));
	}

	[Fact]
	public void Raw_wpautop_profile_supports_all_fields_and_cleans_orphan_linkedin_icon()
	{
		Assert.True(ConseillerPublicParcours.TryReplaceLinkedIn(RawMargotPage, "https://www.linkedin.com/in/margot-saidy-08b968295/", out string withLi));
		Assert.Contains("href=\"https://www.linkedin.com/in/margot-saidy-08b968295/\"", withLi);
		Assert.DoesNotContain("linkedin-brands-2.png", withLi);
		Assert.Contains("linkedin-brands.png", withLi);

		Assert.True(ConseillerPublicParcours.TryReplaceGoogle(withLi, "https://maps.app.goo.gl/abc123", out string withGoogle));
		Assert.Contains("href=\"https://maps.app.goo.gl/abc123\"", withGoogle);
		Assert.Contains("google-brands.png", withGoogle);
		Assert.Contains("[/vc_column_text]", withGoogle);
	}

	[Fact]
	public void Vc_column_text_card_updates_name_email_linkedin_and_google_without_touching_others()
	{
		ConseillerCardChanges changes = new ConseillerCardChanges
		{
			UpdateName = true,
			FullName = "Constance Perrin-Test",
			UpdateEmail = true,
			Email = "constance.perrin@perle-rare.com",
			UpdateLinkedIn = true,
			LinkedIn = "https://www.linkedin.com/in/constance-perrin-471b0540/",
			UpdateGoogle = true,
			Google = "https://www.google.com/maps/search/?api=1&query=Perle+Rare+Constance"
		};

		LinkedInCardUpdate result = ConseillerPublicParcours.ReplaceCardFieldsNearProfile(
			VcCards,
			"https://perle-rare.com/fr/constance-perrin-conseiller-personnel/",
			changes,
			out string updated);

		Assert.Equal(LinkedInCardUpdate.Updated, result);
		Assert.Contains("<p class=\"nom-chasseur-immobilier\"><b>Constance Perrin-Test,</b>", updated);
		Assert.Contains("href=\"mailto:constance.perrin@perle-rare.com\"", updated);
		Assert.DoesNotContain("mailo:cp@perle-rare.com", updated);
		Assert.Contains("href=\"https://www.linkedin.com/in/constance-perrin-471b0540/\"", updated);
		Assert.Contains("href=\"https://www.google.com/maps/search/?api=1&amp;query=Perle+Rare+Constance\"", updated);
		Assert.Contains("google-brands-2.png", updated);
		Assert.Contains("Constance est une femme ultra dynamique", updated);
		// La carte de Georges-Louis reste intacte.
		Assert.Contains("<strong>Georges-L. Duquesne,</strong>", updated);
		Assert.Contains("mailo:gld@perle-rare.com", updated);
		Assert.DoesNotContain("google-brands-2.png\" alt=\"Google\" width=\"25\" height=\"26\" /></a>[/vc_column_text][vc_column_text]<img src=\"constance", updated);
	}

	[Fact]
	public void Card_with_two_advisors_in_same_block_is_refused_as_unsafe()
	{
		string merged = VcCards.Replace("[/vc_column_text][vc_column_text]<img src=\"constance.jpg\" />[/vc_column_text][vc_column_text margin_bottom=\"40\"]", "\n");
		LinkedInCardUpdate result = ConseillerPublicParcours.ReplaceCardFieldsNearProfile(
			merged,
			"https://perle-rare.com/fr/constance-perrin-conseiller-personnel/",
			new ConseillerCardChanges { UpdateEmail = true, Email = "constance.perrin@perle-rare.com" },
			out string updated);
		Assert.Equal(LinkedInCardUpdate.Unsafe, result);
		Assert.Null(updated);
	}

	[Fact]
	public void Raw_english_text_mail_page_accepts_email_rsac_and_google_without_linkedin()
	{
		string enPage = Dedent("""
			[vc_row][vc_column width="1/2"][vc_column_text]
			<p class="nom-chasseur-immobilier-noir"><strong>Fabiola Hemmet</strong>
			Personal consultant</p>
			Before joining Perle Rare, Fabiola worked in communications.

			<a class="lien-noir" href="mailto:fh@perle-rare.com">fh@perle-rare.com</a>[/vc_column_text][/vc_column][/vc_row]
			""");
		Assert.True(ConseillerPublicParcours.TryReplaceEmail(enPage, "fabiola.hemmet@perle-rare.com", out string s1));
		Assert.Contains("<a class=\"lien-noir\" href=\"mailto:fabiola.hemmet@perle-rare.com\">fabiola.hemmet@perle-rare.com</a>", s1);
		Assert.True(ConseillerPublicParcours.TryReplaceRsac(s1, "en", "Agent commercial", "2025AC00297", out string s2));
		Assert.Contains("<p class=\"rsac-conseiller\">Commercial agent registered with the RSAC under number 2025AC00297</p>", s2);
		Assert.True(ConseillerPublicParcours.TryReplaceGoogle(s2, "https://www.google.com/maps/search/?api=1&query=Fabiola", out string s3));
		Assert.Contains("href=\"https://www.google.com/maps/search/?api=1&amp;query=Fabiola\"", s3);
		Assert.Contains("google-brands.png", s3);
		Assert.Contains("[/vc_column_text]", s3);
	}

	private static readonly string RawMargotPage = Dedent("""
		[vc_row][vc_column align="left" width="1/2"][vc_single_image image="6108" link_type="no-link" alt="Acheter un bien immobilier à Paris" margin_top="50" margin_bottom="40"][/vc_column][vc_column width="1/2"][vc_column_text margin_top="50" margin_bottom="50"]
		<p class="nom-chasseur-immobilier-noir"><strong>Margot Saidy</strong>
		Conseiller personnel</p>
		Notre femme de l’orient !

		Diplômée en psychologie.

		Agent commercial inscrit au RSAC de Paris sous le numéro 2023AC00160

		<a href="ms@perle-rare.com"><img class="alignnone size-full wp-image-4375" src="https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid-2.png" alt="" width="30" height="22" /></a> <img class="alignnone size-full wp-image-4374" src="https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands-2.png" alt="" width="25" height="26" />[/vc_column_text][/vc_column][/vc_row]
		""");

	private static readonly string VcCards = Dedent("""
		[vc_column_text margin_bottom="40"]
		<p class="nom-chasseur-immobilier"><strong>Georges-L. Duquesne,</strong>
		Dirigeant co-fondateur</p>
		Georges-Louis possède une expérience de 20 années [...]

		<a class="lien-noir-bis" href="https://perle-rare.com/fr/georges-louis-duquesne-dirigeant-co-fondateur-perle-rare-chasseur-immobilier/">En savoir plus</a>

		<a href="mailo:gld@perle-rare.com"> <img class="alignnone size-full wp-image-4375" src="https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid-2.png" alt="" width="30" height="22" /></a> <a href="https://www.linkedin.com/in/glduquesne/"><img class="alignnone size-full wp-image-4374" src="https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands-2.png" alt="" width="25" height="26" /></a>[/vc_column_text][vc_column_text]<img src="constance.jpg" />[/vc_column_text][vc_column_text margin_bottom="40"]
		<p class="nom-chasseur-immobilier"><b>Constance Perrin,</b>
		Conseiller personnel</p>
		Constance est une femme ultra dynamique [...]

		<a class="lien-noir-bis" href="https://perle-rare.com/fr/constance-perrin-conseiller-personnel/">En savoir plus</a>

		<a href="mailo:cp@perle-rare.com"> <img class="alignnone size-full wp-image-4375" src="https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid-2.png" alt="" width="30" height="22" /></a> <a href="https://www.linkedin.com/in/constance-perrin-old/"><img class="alignnone size-full wp-image-4374" src="https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands-2.png" alt="" width="25" height="26" /></a>[/vc_column_text]
		""");

	private static string Dedent(string value)
	{
		return string.Join("\n", value.Replace("\r\n", "\n").Split('\n').Select((string line) => line.TrimStart('\t', ' ')));
	}

	[Fact]
	public void Portrait_url_must_be_the_site_jpg_and_the_shortcode_is_replaced()
	{
		Assert.True(ConseillerPublicParcours.TryParseSitePortraitUrl(
			"https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-georges-louis-duquesne-1.jpg",
			out string relative));
		Assert.Equal("2023/01/chasseur-immobilier-perle-rare-georges-louis-duquesne-1.jpg", relative);
		Assert.False(ConseillerPublicParcours.TryParseSitePortraitUrl(
			"http://perle-rare.com/wp-content/uploads/2023/01/portrait.jpg", out _));
		Assert.False(ConseillerPublicParcours.TryParseSitePortraitUrl(
			"https://perle-rare.com/wp-content/uploads/2023/01/portrait.jpg?x=1", out _));
		Assert.False(ConseillerPublicParcours.TryParseSitePortraitUrl(
			"https://example.com/wp-content/uploads/2023/01/portrait.jpg", out _));
		Assert.False(ConseillerPublicParcours.TryParseSitePortraitUrl(
			"https://perle-rare.com/wp-content/uploads/2023/01/../portrait.jpg", out _));
		Assert.False(ConseillerPublicParcours.TryParseSitePortraitUrl("", out _));

		Assert.True(ConseillerPublicParcours.AttachmentMetadataIsSquare555("a:2:{s:5:\"width\";i:555;s:6:\"height\";i:555;}"));
		Assert.False(ConseillerPublicParcours.AttachmentMetadataIsSquare555("a:2:{s:5:\"width\";i:555;s:6:\"height\";i:400;}"));
		Assert.False(ConseillerPublicParcours.AttachmentMetadataIsSquare555("a:3:{s:5:\"width\";i:669;s:6:\"height\";i:669;s:5:\"sizes\";a:1:{s:4:\"crop\";a:2:{s:5:\"width\";i:555;s:6:\"height\";i:555;}}}"));
		Assert.False(ConseillerPublicParcours.AttachmentMetadataIsSquare555(""));

		Assert.True(ConseillerPublicParcours.TryReplacePortrait(Page, 7244, out string updated));
		Assert.Contains("[vc_single_image image=\"7244\"", updated);
		Assert.DoesNotContain("image=\"5791\"", updated);
		Assert.Contains("wp-image-4359", updated);
		Assert.Contains("wp-image-4358", updated);
		Assert.True(ConseillerPublicParcours.TryReplacePortrait(updated, 7244, out string same));
		Assert.Equal(updated, same);
		Assert.False(ConseillerPublicParcours.TryReplacePortrait(Page + Page, 7244, out _));
		Assert.False(ConseillerPublicParcours.TryReplacePortrait("<p>sans portrait</p>", 7244, out _));
	}
}
