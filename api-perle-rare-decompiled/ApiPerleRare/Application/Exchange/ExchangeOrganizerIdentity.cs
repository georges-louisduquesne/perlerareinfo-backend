using System;

namespace ApiPerleRare.Application.Exchange;

/// <summary>
/// Exchange met parfois le legacy DN (/o=…/ou=Exchange Administrative Group…)
/// à la place de l’adresse SMTP dans l’invitation reçue par un externe.
/// </summary>
public static class ExchangeOrganizerIdentity
{
	public static bool IsLegacyDn(string value)
	{
		string text = (value ?? string.Empty).Trim();
		if (text.Length == 0)
		{
			return false;
		}
		return text.StartsWith("/o=", StringComparison.OrdinalIgnoreCase)
			|| text.IndexOf("Exchange Administrative Group", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static string SmtpOrEmpty(string value)
	{
		string text = (value ?? string.Empty).Trim();
		if (text.Length == 0 || IsLegacyDn(text) || text.IndexOf('@') < 0)
		{
			return string.Empty;
		}
		return text;
	}
}
