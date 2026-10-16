using System;

namespace ApiPerleRare.Application.Exchange;

internal sealed class ExchangeSendException : Exception
{
	public ExchangeSendException(string message)
		: base(message)
	{
	}
}
