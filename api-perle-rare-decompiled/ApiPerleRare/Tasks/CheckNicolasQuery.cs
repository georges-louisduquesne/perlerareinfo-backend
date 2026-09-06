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

	public override string Description => "Test Nicolas Query";

	public CheckNicolasQuery(ApplicationDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public override void Run()
	{
		AnnoncesGlobalesController agc = new AnnoncesGlobalesController(_dbContext, null, new MemoryCache(new MemoryCacheOptions()));
		AnnoncesGlobalesController.FilterDef filterDef = JsonConvert.DeserializeObject<AnnoncesGlobalesController.FilterDef>(Resources.NicolasQuery);
		ActionResult<AnnoncesGlobalesController.InfoAnnonces> res = agc.RecupInfosAnnonces(filterDef).Result;
	}
}
