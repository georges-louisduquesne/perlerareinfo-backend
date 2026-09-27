using System.Data;
using System.Threading;
using ApiPerleRare.Models;
using ApiPerleRare.RecupInfos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Annonces;

public sealed class RecupInfosAnnonces2UseCase : IRecupInfosAnnonces2UseCase
{
	private readonly IApplicationDbContext _context;

	private readonly IExchangeService _exchangeService;

	private readonly IMemoryCache _memoryCache;

	public RecupInfosAnnonces2UseCase(IApplicationDbContext context, IExchangeService exchangeService, IMemoryCache memoryCache)
	{
		_context = context;
		_exchangeService = exchangeService;
		_memoryCache = memoryCache;
	}

	public RecupInfoResponse Execute(Filter filter, CancellationToken cancellationToken = default)
	{
		MySqlConnection c = (MySqlConnection)_context.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			c.Open();
		}
		return (RecupInfoResponse)RecupInfoSearcher.Search(_context, c, filter, _exchangeService, _memoryCache, cancellationToken);
	}
}
