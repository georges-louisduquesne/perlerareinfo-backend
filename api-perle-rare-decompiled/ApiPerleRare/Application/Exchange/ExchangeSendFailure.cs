using System;
using Microsoft.Exchange.WebServices.Data;

namespace ApiPerleRare.Application.Exchange;

internal static class ExchangeSendFailure
{
	internal static string Message(Exception ex, string fromEmail)
	{
		Exception cur = Unwrap(ex);
		string text = cur?.Message ?? string.Empty;
		string code = string.Empty;
		if (cur is ServiceResponseException response)
		{
			code = response.ErrorCode.ToString();
		}
		if (code == "ErrorSendAsDenied" || code == "ErrorSendOnBehalfDenied"
			|| text.IndexOf("send mail on behalf", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("SendAs", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return Denied(fromEmail);
		}
		if (code == "ErrorMessageSizeExceeded"
			|| (text.IndexOf("exceed", StringComparison.OrdinalIgnoreCase) >= 0
				&& text.IndexOf("size", StringComparison.OrdinalIgnoreCase) >= 0))
		{
			return "La pièce jointe est trop volumineuse pour Exchange.";
		}
		if (text.IndexOf("401", StringComparison.Ordinal) >= 0
			|| text.IndexOf("Unauthorized", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("credentials", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "L’authentification Exchange a échoué.";
		}
		return "L’envoi Exchange a échoué.";
	}

	internal static string Denied(string fromEmail)
	{
		string who = string.IsNullOrWhiteSpace(fromEmail) ? "cette adresse" : fromEmail.Trim();
		return "Le compte Exchange n’a pas le droit d’envoyer au nom de " + who + ".";
	}

	private static Exception Unwrap(Exception ex)
	{
		while (ex is AggregateException aggregate && aggregate.InnerException != null && aggregate.InnerExceptions.Count == 1)
		{
			ex = aggregate.InnerException;
		}
		return ex;
	}
}
