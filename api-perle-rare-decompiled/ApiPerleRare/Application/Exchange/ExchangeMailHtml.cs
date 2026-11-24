using System;
using System.Text.RegularExpressions;
using Microsoft.Exchange.WebServices.Data;

namespace ApiPerleRare.Application.Exchange;

/// <summary>
/// Exchange enregistre un HTML sans charset en UTF-16. Le Mail de l’iPhone
/// affiche alors chaque lettre comme un idéogramme. Le meta UTF-8 et la page
/// de code Internet 65001 font stocker le corps en UTF-8.
/// </summary>
internal static class ExchangeMailHtml
{
	internal const int Utf8CodePage = 65001;

	internal const int InternetCodePageTag = 0x3FDE;

	internal const string CharsetMeta = "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">";

	private const string DocumentStyle = "<style>html,body{font-family:Verdana,sans-serif;font-size:15px;line-height:1.5}</style>";

	private static readonly ExtendedPropertyDefinition InternetCodePage = new ExtendedPropertyDefinition(InternetCodePageTag, MapiPropertyType.Integer);

	private static readonly Regex CharsetMetaPattern = new Regex("<meta\\b[^>]*charset\\s*=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	internal static void Apply(EmailMessage message, string contents)
	{
		message.SetExtendedProperty(InternetCodePage, Utf8CodePage);
		message.Body = new MessageBody(BodyType.HTML, Prepare(contents));
	}

	internal static string Prepare(string contents)
	{
		string html = contents ?? string.Empty;
		if (html.IndexOf("<html>", StringComparison.Ordinal) < 0)
		{
			return Wrap(html);
		}
		if (HasCharset(html))
		{
			return html;
		}
		return InsertCharset(html);
	}

	private static string Wrap(string html)
	{
		return "<html><head>" + CharsetMeta + DocumentStyle + "</head><body>" + html + "</body></html>";
	}

	private static bool HasCharset(string html)
	{
		return CharsetMetaPattern.IsMatch(html);
	}

	private static string InsertCharset(string html)
	{
		int headEnd = TagEnd(html, "head");
		if (headEnd >= 0)
		{
			return html.Insert(headEnd + 1, CharsetMeta);
		}
		int htmlEnd = TagEnd(html, "html");
		if (htmlEnd >= 0)
		{
			return html.Insert(htmlEnd + 1, "<head>" + CharsetMeta + "</head>");
		}
		return html;
	}

	private static int TagEnd(string html, string tagName)
	{
		int search = 0;
		while (search < html.Length)
		{
			int start = html.IndexOf("<" + tagName, search, StringComparison.OrdinalIgnoreCase);
			if (start < 0)
			{
				return -1;
			}
			int nameEnd = start + 1 + tagName.Length;
			if (nameEnd < html.Length)
			{
				char boundary = html[nameEnd];
				if (boundary != '>' && boundary != '/' && !char.IsWhiteSpace(boundary))
				{
					search = nameEnd;
					continue;
				}
			}
			return html.IndexOf('>', start);
		}
		return -1;
	}
}
