using ApiPerleRare.Application.Formation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class FormationController : ControllerBase
{
	private readonly IFormationStore _store;

	public FormationController(IFormationStore store)
	{
		_store = store;
	}

	[HttpPost("series")]
	public IActionResult CreateSeries([FromBody] CreateSerieRequest request)
	{
		return Run(() =>
		{
			int[] questions = FormationRules.RequireHistoireSeries(request?.Section, request?.Questions);
			long serie = _store.CreateSeries(this.GetUserId(), FormationRules.HistoireSection, questions);
			return Ok(new { serie });
		});
	}

	[HttpPost("reponses")]
	public IActionResult Record([FromBody] AnswerRequest request)
	{
		return Run(() =>
		{
			if (request == null)
			{
				throw new FormationRejectedException("Réponse absente.");
			}
			_store.RecordAnswer(this.GetUserId(), request.Serie, request.Ordre, request.Question, request.Resultat);
			return Ok(new { resultat = FormationRules.RequireResultat(request.Resultat) });
		});
	}

	[HttpPost("scores")]
	public IActionResult Close([FromBody] ScoreRequest request)
	{
		return Run(() =>
		{
			if (request == null || request.Serie <= 0)
			{
				throw new FormationRejectedException("Série absente.");
			}
			int score = _store.CloseScore(this.GetUserId(), request.Serie);
			return Ok(new { score, total = FormationRules.SeriesSize, tone = FormationRules.Tone(score) });
		});
	}

	private IActionResult Run(System.Func<IActionResult> action)
	{
		try
		{
			return action();
		}
		catch (FormationSchemaMissingException ex)
		{
			return StatusCode(503, new { message = ex.Message });
		}
		catch (FormationForbiddenException)
		{
			return StatusCode(403, new { message = "Cette série appartient à un autre utilisateur." });
		}
		catch (FormationRejectedException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	public class CreateSerieRequest
	{
		public string Section { get; set; }

		public int[] Questions { get; set; }
	}

	public class AnswerRequest
	{
		public long Serie { get; set; }

		public int Ordre { get; set; }

		public int Question { get; set; }

		public string Resultat { get; set; }
	}

	public class ScoreRequest
	{
		public long Serie { get; set; }
	}
}
