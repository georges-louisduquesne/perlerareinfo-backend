using ApiPerleRare.Application.Annonces;
using ApiPerleRare.Controllers;
using ApiPerleRare.Models;
using ApiPerleRare.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace ApiPerleRare.Tasks;

public class CheckNicolasQuery : AbstractTask
{
	private readonly ApplicationDbContext _dbContext;

	private readonly IExchangeService _exchangeService;

	public override string Description => "Test Nicolas Query";

	public CheckNicolasQuery(ApplicationDbContext dbContext, IExchangeService exchangeService = null)
	{
		_dbContext = dbContext;
		_exchangeService = exchangeService;
	}

	public override void Run()
	{
		IRecupInfosAnnoncesUseCase recup = new RecupInfosAnnoncesUseCase(_dbContext, _exchangeService);
		AnnoncesGlobalesController.FilterDef filterDef = JsonConvert.DeserializeObject<AnnoncesGlobalesController.FilterDef>(Resources.NicolasQuery);
		AnnoncesGlobalesController.InfoAnnonces res = recup.Execute(filterDef).Result;
	}
}
