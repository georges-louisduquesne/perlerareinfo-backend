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
	public void Apply_stamps_the_internet_code_page_as_utf8()
	{
		ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
		service.Url = new Uri("https://exchange.invalid/EWS/Exchange.asmx");
		EmailMessage message = new EmailMessage(service);

		ExchangeMailHtml.Apply(message, Prospect);

		Assert.Contains("charset=utf-8", message.Body.Text, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("Frédéric", message.Body.Text);
		ExtendedProperty codePage = Assert.Single(message.ExtendedProperties);
		ExtendedPropertyDefinition definition = Assert.IsType<ExtendedPropertyDefinition>(codePage.PropertyDefinition);
		Assert.Equal(ExchangeMailHtml.InternetCodePageTag, definition.Tag);
		Assert.Equal(ExchangeMailHtml.Utf8CodePage, codePage.Value);
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
