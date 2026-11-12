using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Application.Search;

public sealed class SearchUseCase : ISearchUseCase
{
	private readonly ISearchService _searchService;
	private readonly ILogger<SearchUseCase> _logger;

	public SearchUseCase(ISearchService searchService, ILogger<SearchUseCase> logger)
	{
		_searchService = searchService;
		_logger = logger;
	}

	public IEnumerable<SelectResult> Execute(string filter, int max = 15, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(filter))
		{
			return new SelectResult[0];
		}
		try
		{
			string sqlFilter = filter.Replace("%", "\\%");
			return _searchService.Search(new SearchQuery
			{
				Filter = filter,
				SqlFilter = sqlFilter,
				HtmlFilter = HttpUtility.HtmlEncode(filter),
				Max = max,
				Cancellation = cancellationToken
			});
		}
		catch (Exception) when (cancellationToken.IsCancellationRequested)
		{
			return new SelectResult[0];
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Recherche globale en échec pour le filtre « {Filter} ».", filter);
			return new SelectResult[1]
			{
				new SelectResult
				{
					Category = "Erreur",
					Description = UserFacingSearchError(ex)
				}
			};
		}
	}

	internal static string UserFacingSearchError(Exception ex)
	{
		for (Exception cur = ex; cur != null; cur = cur.InnerException)
		{
			if (cur.Message.Contains("Too many connections", StringComparison.OrdinalIgnoreCase))
			{
				return "Le serveur de base de données est momentanément saturé. Réessayez dans quelques secondes.";
			}
		}
		return "La recherche est temporairement indisponible. Réessayez ou contactez l'administrateur si le problème persiste.";
	}
}
