using System;
using ApiPerleRare.Application.Exchange;
using Microsoft.Exchange.WebServices.Data;
using Xunit;

namespace ApiPerleRare.Tests;

public class ExchangeMailHtmlTests
{
	private const string Prospect = "<p style=\"font-family:Lucida Sans Unicode, Lucida Sans, Lucida Grande, sans-serif; font-size: 10pt; color: #403152\">"
		+ "<a style=\"color: #403152\" href=\"https://perle-rare.info/#/contact/64326\">BEAUSOLEIL Frédéric</a></p>";

	[Fact]
	public void Prospect_fragment_is_wrapped_as_utf8_html()
	{
		string html = ExchangeMailHtml.Prepare(Prospect);

		Assert.Contains("charset=utf-8", html, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("BEAUSOLEIL Frédéric", html);
		Assert.Contains("https://perle-rare.info/#/contact/64326", html);
		Assert.Contains("font-family:Verdana,sans-serif", html);
		Assert.Equal(1, CountOf(html, "charset="));
	}

	[Fact]
	public void Document_without_charset_receives_one_meta_inside_the_head()
	{
		const string source = "<html><head><style>p{color:red}</style></head><body>Bonjour</body></html>";

		string html = ExchangeMailHtml.Prepare(source);

		Assert.Contains("<head>" + ExchangeMailHtml.CharsetMeta + "<style>", html);
		Assert.Contains("Bonjour", html);
		Assert.Equal(1, CountOf(html, "charset="));
		Assert.Equal(html, ExchangeMailHtml.Prepare(html));
	}

	[Fact]
	public void Document_that_already_declares_a_charset_is_left_unchanged()
	{
		const string source = "<html><head><meta charset=\"utf-8\"></head><body>Déjà encodé</body></html>";

		Assert.Equal(source, ExchangeMailHtml.Prepare(source));
	}

	[Fact]
	public void Document_without_a_head_gets_a_utf8_head()
	{
		const string source = "<!DOCTYPE html><html><body>Isabelle</body></html>";

		string html = ExchangeMailHtml.Prepare(source);

		Assert.Contains("<html><head>" + ExchangeMailHtml.CharsetMeta + "</head><body>Isabelle</body></html>", html);
	}

	[Fact]
	public void Document_with_html_attributes_or_uppercase_is_not_double_wrapped()
	{
		const string withLang = "<!DOCTYPE html><html lang=\"fr\"><head><style>p{color:red}</style></head><body>Offre acceptée : 360.000 €</body></html>";
		const string uppercase = "<HTML><BODY>Négociateur : Clémence</BODY></HTML>";

		string preparedLang = ExchangeMailHtml.Prepare(withLang);
		string preparedUpper = ExchangeMailHtml.Prepare(uppercase);

		Assert.Equal(1, CountOf(preparedLang, "<html"));
		Assert.Contains("<head>" + ExchangeMailHtml.CharsetMeta + "<style>", preparedLang);
		Assert.Contains("Offre acceptée : 360.000 €", preparedLang);
		Assert.Equal(1, CountOf(preparedUpper, "<html"));
		Assert.Contains("<HTML><head>" + ExchangeMailHtml.CharsetMeta + "</head><BODY>Négociateur : Clémence</BODY></HTML>", preparedUpper);
	}

	[Fact]
	public void Apply_sets_utf8_html_body_without_overriding_internet_code_page()
	{
		ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
		service.Url = new Uri("https://exchange.invalid/EWS/Exchange.asmx");
		EmailMessage message = new EmailMessage(service);
		const string offerHtml = "<div>Offre acceptée au prix du mandat soit 360.000 € FAI <br />\nNégociateur : Clémence KAMIENNY <br />\nConseiller : Solveig DE FONTGALLAND</div>";

		ExchangeMailHtml.Apply(message, offerHtml);

		Assert.Equal(BodyType.HTML, message.Body.BodyType);
		Assert.Contains("charset=utf-8", message.Body.Text, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("Offre acceptée au prix du mandat soit 360.000 € FAI", message.Body.Text);
		Assert.Contains("Négociateur : Clémence KAMIENNY", message.Body.Text);
		Assert.DoesNotContain(
			message.ExtendedProperties,
			prop => prop.PropertyDefinition is ExtendedPropertyDefinition def
				&& def.Tag == ExchangeMailHtml.InternetCodePageTag);
	}

	[Fact]
	public void Localhost_mail_uses_utf8_encodings_splits_semicolon_recipients_and_declares_charset()
	{
		using var message = ApiPerleRare.Infrastructure.Mail.LocalhostMailService.BuildMessage(
			"Support opérationnel",
			"support@example.com",
			"ct@perle-rare.com; gld@perle-rare.com",
			"OFFRE ACCEPTEE — 360.000 €",
			"<div>Offre acceptée : 360.000 € — Négociateur : Clémence</div>",
			"copie@perle-rare.com; autre@perle-rare.com");

		Assert.Equal(System.Text.Encoding.UTF8, message.HeadersEncoding);
		Assert.Equal(System.Text.Encoding.UTF8, message.SubjectEncoding);
		Assert.Equal(System.Text.Encoding.UTF8, message.BodyEncoding);
		Assert.True(message.IsBodyHtml);
		Assert.Contains("charset=utf-8", message.Body, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("Offre acceptée : 360.000 € — Négociateur : Clémence", message.Body);
		Assert.Equal("Support opérationnel", message.From.DisplayName);
		Assert.Equal(2, message.To.Count);
		Assert.Equal(2, message.CC.Count);
	}

	private static int CountOf(string text, string token)
	{
		int count = 0;
		int from = 0;
		while ((from = text.IndexOf(token, from, StringComparison.OrdinalIgnoreCase)) >= 0)
		{
			count++;
			from += token.Length;
		}
		return count;
	}
}
