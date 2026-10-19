using ApiPerleRare.Application.Annuaire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AnnuairePartenairesController : ControllerBase
{
	private readonly IAnnuairePartenairesStore _store;

	public AnnuairePartenairesController(IAnnuairePartenairesStore store)
	{
		_store = store;
	}

	[HttpGet]
	public ActionResult<AnnuairePartenairesDocument> Get()
	{
		return _store.Read();
	}

	[HttpPut]
	[Authorize(Roles = "Admin")]
	public ActionResult<AnnuairePartenairesDocument> Put([FromBody] AnnuairePartenairesWrite body)
	{
		var cleaned = AnnuairePartenairesLogic.Clean(body?.Items);
		if (cleaned.Count == 0)
		{
			return BadRequest(new { message = "Ajoutez au moins un partenaire." });
		}
		return _store.Save(cleaned);
	}
}
