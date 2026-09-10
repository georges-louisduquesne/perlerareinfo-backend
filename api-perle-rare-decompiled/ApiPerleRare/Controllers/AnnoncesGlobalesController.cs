using System.Threading.Tasks;
using ApiPerleRare.Application.Annonces;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using ApiPerleRare.RecupInfos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AnnoncesGlobalesController : ControllerBase
{
	public class InfoAnnonces
	{
		public InfoAnnoncesRes[] Res { get; set; }

		public string Error { get; set; }
	}

	public class InfoAnnoncesRes
	{
		public string Field { get; set; }

		public string Value { get; set; }

		public long Count { get; set; }
	}

	public class FilterDef
	{
		public string Where { get; set; }

		public uint ContactRef { get; set; }

		public bool Apply { get; set; }

		public int Limit { get; set; } = 5000;

		public FilterDefFieldInfo[] Fields { get; set; }
	}

	public class FilterDefFieldInfo
	{
		public string Field { get; set; }

		public string[] CheckedValues { get; set; }

		public string[] UncheckedValues { get; set; }
	}

	private readonly IListAnnoncesGlobalesUseCase _list;

	private readonly IGetAnnoncesGlobalesByIdUseCase _getById;

	private readonly IRecupInfosAnnoncesUseCase _recupInfos;

	private readonly IRecupInfosAnnonces2UseCase _recupInfos2;

	private readonly IViderInfosAnnoncesUseCase _viderInfos;

	public AnnoncesGlobalesController(
		IListAnnoncesGlobalesUseCase list,
		IGetAnnoncesGlobalesByIdUseCase getById,
		IRecupInfosAnnoncesUseCase recupInfos,
		IRecupInfosAnnonces2UseCase recupInfos2,
		IViderInfosAnnoncesUseCase viderInfos)
	{
		_list = list;
		_getById = getById;
		_recupInfos = recupInfos;
		_recupInfos2 = recupInfos2;
		_viderInfos = viderInfos;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<AnnoncesGlobales>>> GetAnnoncesGlobales([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] int photos = 0)
	{
		return await _list.Execute(select, where, orderby, skip, take, photos);
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<AnnoncesGlobales>> GetAnnoncesGlobales(uint id, [FromQuery] int photos = 0)
	{
		AnnoncesGlobales annoncesGlobales = await _getById.Execute(id, photos);
		if (annoncesGlobales == null)
		{
			return NotFound();
		}
		return annoncesGlobales;
	}

	[HttpPost("RecupInfosAnnonces")]
	public async Task<ActionResult<InfoAnnonces>> RecupInfosAnnonces(FilterDef filterDef)
	{
		return await _recupInfos.Execute(filterDef);
	}

	[HttpPost("RecupInfosAnnonces2")]
	public ActionResult<RecupInfoResponse> RecupInfosAnnonces2(Filter filter)
	{
		return _recupInfos2.Execute(filter);
	}

	[HttpGet("ViderInfosAnnonces/{contactRef}")]
	public ActionResult ViderInfosAnnonces(uint contactRef)
	{
		_viderInfos.Execute(contactRef);
		return Ok();
	}

	/// <summary>Kept for callers that used the former controller static helper.</summary>
	public static string ConvertFilterDefToAnnoncesGlobalesSQL(FilterDef filterDef)
	{
		return RecupInfosAnnoncesUseCase.ConvertFilterDefToAnnoncesGlobalesSQL(filterDef);
	}
}
