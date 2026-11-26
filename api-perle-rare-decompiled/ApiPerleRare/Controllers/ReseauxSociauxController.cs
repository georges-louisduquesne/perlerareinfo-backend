using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Application.Reseaux;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ReseauxSociauxController : ControllerBase
{
	public class ModeleBody
	{
		public bool Actif { get; set; }

		public string Type { get; set; }

		public string Titre { get; set; }

		public string Contenu { get; set; }
	}

	public class QuotaBody
	{
		public int Quota { get; set; }

		public int Solde { get; set; }
	}

	public class SolliciterBody
	{
		public string PostId { get; set; }

		public string Reseau { get; set; }

		public uint RefConseiller { get; set; }

		public string Type { get; set; }
	}

	private static int _refreshing;

	private readonly ApplicationDbContext _context;

	private readonly IConfiguration _configuration;

	private readonly IServiceScopeFactory _scopes;

	private readonly ILogger<ReseauxSociauxController> _logger;

	public ReseauxSociauxController(ApplicationDbContext context, IConfiguration configuration, IServiceScopeFactory scopes, ILogger<ReseauxSociauxController> logger)
	{
		_context = context;
		_configuration = configuration;
		_scopes = scopes;
		_logger = logger;
	}

	[HttpGet]
	public async Task<IActionResult> Tableau()
	{
		if (!await ReseauxQuotaStore.SchemaReady(_context, HttpContext.RequestAborted))
		{
			return StatusCode(503, new { message = "Les tables réseaux ne sont pas encore créées." });
		}
		await ReseauxQuotaReset.ApplyAsync(_context, ParisNow(), HttpContext.RequestAborted);
		await EnsureModeles(HttpContext.RequestAborted);
		List<ReseauxConseillerRow> rows = await ReseauxQuotaStore.ListActifs(_context, HttpContext.RequestAborted);
		var conseillers = rows.Select((ReseauxConseillerRow cp) => new
		{
			refConseiller = cp.Ref,
			prenom = cp.Prenom,
			nom = cp.Nom,
			tel = cp.Tel,
			quota = cp.Quota,
			solde = cp.Solde
		}).ToList();
		var paires = await _context.SollicitationsReseaux.AsNoTracking()
			.Select((SollicitationsReseaux s) => new
			{
				postId = s.SrPostId,
				reseau = s.SrReseau,
				refConseiller = s.SrRefConseiller
			})
			.ToListAsync(HttpContext.RequestAborted);
		var modeles = await _context.ModelesReseaux.AsNoTracking()
			.OrderBy((ModelesReseaux m) => m.MrId)
			.Select((ModelesReseaux m) => new
			{
				id = m.MrId,
				actif = m.MrActif,
				type = m.MrType,
				titre = m.MrTitre,
				contenu = m.MrContenu
			})
			.ToListAsync(HttpContext.RequestAborted);
		return Ok(new
		{
			conseillers,
			paires,
			modeles
		});
	}

	[HttpGet("Posts")]
	public async Task<IActionResult> Posts([FromQuery] bool refresh = false)
	{
		CancellationToken cancellationToken = HttpContext.RequestAborted;
		if (!await ReseauxQuotaStore.SchemaReady(_context, cancellationToken))
		{
			return StatusCode(503, new { message = "Les tables réseaux ne sont pas encore créées." });
		}
		List<PublicationsReseaux> rows = await LoadPosts(cancellationToken);
		int stamp = await SyncStamp(cancellationToken);
		int nowUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		bool stale = ReseauxSociauxLogic.SyncIsStale(stamp, nowUnix);
		string warning = string.Empty;
		if (refresh || rows.Count == 0)
		{
			warning = await ReseauxPostsSync.RefreshAsync(_context, _configuration, cancellationToken) ?? string.Empty;
			rows = await LoadPosts(cancellationToken);
		}
		else if (stale)
		{
			RefreshInBackground();
		}
		var posts = rows
			.OrderByDescending((PublicationsReseaux row) => row.PubDatePublication)
			.Take(ReseauxSociauxLogic.PostsShown)
			.Select((PublicationsReseaux row) => new
			{
				id = row.PubPostId,
				reseau = row.PubReseau,
				url = row.PubUrl,
				vignette = row.PubVignette,
				texte = row.PubTexte,
				datePublication = row.PubDatePublication.ToString("yyyy-MM-ddTHH:mm:ss"),
				dateLabel = ReseauxSociauxLogic.FormatParis(row.PubDatePublication)
			})
			.ToList();
		return Ok(new
		{
			posts,
			warning
		});
	}

	[HttpPut("Modeles/{id:int}")]
	public async Task<IActionResult> SauverModele(int id, [FromBody] ModeleBody body)
	{
		if (id <= 0 || body == null || !ReseauxSociauxLogic.IsType((body.Type ?? string.Empty).Trim()))
		{
			return BadRequest(new { message = "Le type doit être Like ou Commentaire." });
		}
		string titre = (body.Titre ?? string.Empty).Trim();
		string contenu = body.Contenu ?? string.Empty;
		if (titre.Length == 0 || titre.Length > 120 || contenu.Trim().Length == 0 || contenu.Length > 4000)
		{
			return BadRequest(new { message = "Le titre et le contenu du modèle sont requis." });
		}
		uint modeleId = (uint)id;
		ModelesReseaux row = await _context.ModelesReseaux.FirstOrDefaultAsync((ModelesReseaux m) => m.MrId == modeleId, HttpContext.RequestAborted);
		if (row == null)
		{
			return NotFound(new { message = "Modèle introuvable." });
		}
		row.MrActif = body.Actif;
		row.MrType = body.Type.Trim();
		row.MrTitre = titre;
		row.MrContenu = contenu.Trim();
		await _context.SaveChangesAsync(HttpContext.RequestAborted);
		return Ok(new
		{
			id = row.MrId,
			actif = row.MrActif,
			type = row.MrType,
			titre = row.MrTitre,
			contenu = row.MrContenu
		});
	}

	[HttpPut("Conseillers/{id:int}")]
	public async Task<IActionResult> SauverQuota(int id, [FromBody] QuotaBody body)
	{
		if (id <= 0 || body == null)
		{
			return BadRequest(new { message = "Quota manquant." });
		}
		uint conseillerId = (uint)id;
		ReseauxConseillerRow row = await ReseauxQuotaStore.ReadActif(_context, conseillerId, HttpContext.RequestAborted);
		if (row == null)
		{
			return NotFound(new { message = "Conseiller actif introuvable." });
		}
		(int quota, int solde) = ReseauxSociauxLogic.ApplyQuotaEdit(row.Quota, row.Solde, body.Quota, body.Solde);
		bool saved = await ReseauxQuotaStore.Save(_context, conseillerId, quota, solde, HttpContext.RequestAborted);
		if (!saved)
		{
			return NotFound(new { message = "Conseiller actif introuvable." });
		}
		return Ok(new
		{
			refConseiller = conseillerId,
			quota,
			solde
		});
	}

	[HttpPost("Solliciter")]
	public async Task<IActionResult> Solliciter([FromBody] SolliciterBody body)
	{
		if (body == null || !ReseauxSociauxLogic.IsType((body.Type ?? string.Empty).Trim()) || !ReseauxSociauxLogic.IsReseau((body.Reseau ?? string.Empty).Trim()))
		{
			return BadRequest(new { message = "Publication ou type de sollicitation invalide." });
		}
		string reseau = body.Reseau.Trim();
		string postId = (body.PostId ?? string.Empty).Trim();
		string type = body.Type.Trim();
		CancellationToken cancellationToken = HttpContext.RequestAborted;
		PublicationsReseaux post = await _context.PublicationsReseaux.AsNoTracking().FirstOrDefaultAsync(
			(PublicationsReseaux p) => p.PubReseau == reseau && p.PubPostId == postId,
			cancellationToken);
		if (post == null)
		{
			return NotFound(new { message = "Publication introuvable." });
		}
		ReseauxConseillerRow conseiller = await ReseauxQuotaStore.ReadActif(_context, body.RefConseiller, cancellationToken);
		if (conseiller == null)
		{
			return NotFound(new { message = "Conseiller actif introuvable." });
		}
		bool already = await _context.SollicitationsReseaux.AsNoTracking().AnyAsync(
			(SollicitationsReseaux s) => s.SrReseau == reseau && s.SrPostId == postId && s.SrRefConseiller == body.RefConseiller,
			cancellationToken);
		if (already || conseiller.Solde <= 0)
		{
			return Conflict(new
			{
				message = already
					? "Ce conseiller a déjà été sollicité pour cette publication."
					: "Ce conseiller n'a plus de sollicitation disponible cette semaine."
			});
		}
		ModelesReseaux modele = await _context.ModelesReseaux.AsNoTracking()
			.Where((ModelesReseaux m) => m.MrActif && m.MrType == type)
			.OrderBy((ModelesReseaux m) => m.MrId)
			.FirstOrDefaultAsync(cancellationToken);
		if (modele == null)
		{
			return Conflict(new { message = "Aucun modèle actif pour ce type de message." });
		}
		string message = ReseauxSociauxLogic.ApplyTemplate(modele.MrContenu, conseiller.Prenom, conseiller.Nom, post.PubUrl);
		await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
		int decremented = await _context.Database.ExecuteSqlInterpolatedAsync(
			$"UPDATE conseillers_personnels SET CP_Solde_Quota = CP_Solde_Quota - 1 WHERE CP_RefConseiller = {body.RefConseiller} AND CP_Actif = 1 AND CP_Solde_Quota > 0",
			cancellationToken);
		if (decremented != 1)
		{
			await tx.RollbackAsync(cancellationToken);
			return Conflict(new { message = "Ce conseiller n'a plus de sollicitation disponible cette semaine." });
		}
		_context.SollicitationsReseaux.Add(new SollicitationsReseaux
		{
			SrPostId = post.PubPostId,
			SrReseau = post.PubReseau,
			SrRefConseiller = conseiller.Ref,
			SrDatePublication = post.PubDatePublication,
			SrDateEnvoi = ParisNow(),
			SrType = type
		});
		try
		{
			await _context.SaveChangesAsync(cancellationToken);
			await tx.CommitAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			await tx.RollbackAsync(cancellationToken);
			return Conflict(new { message = "Ce conseiller a déjà été sollicité pour cette publication." });
		}
		return Ok(new
		{
			message,
			telephone = conseiller.Tel,
			solde = conseiller.Solde - 1,
			refConseiller = conseiller.Ref,
			postId = post.PubPostId,
			reseau = post.PubReseau
		});
	}

	private async Task EnsureModeles(CancellationToken cancellationToken)
	{
		if (await _context.ModelesReseaux.AnyAsync(cancellationToken))
		{
			return;
		}
		_context.ModelesReseaux.Add(new ModelesReseaux
		{
			MrActif = true,
			MrType = ReseauxSociauxLogic.Like,
			MrTitre = "Like",
			MrContenu = ReseauxSociauxLogic.DefaultLikeContenu()
		});
		_context.ModelesReseaux.Add(new ModelesReseaux
		{
			MrActif = true,
			MrType = ReseauxSociauxLogic.Commentaire,
			MrTitre = "Commentaire",
			MrContenu = ReseauxSociauxLogic.DefaultCommentaireContenu()
		});
		await _context.SaveChangesAsync(cancellationToken);
	}

	private async Task<List<PublicationsReseaux>> LoadPosts(CancellationToken cancellationToken)
	{
		return await _context.PublicationsReseaux.AsNoTracking()
			.OrderByDescending((PublicationsReseaux p) => p.PubDatePublication)
			.Take(30)
			.ToListAsync(cancellationToken);
	}

	private async Task<int> SyncStamp(CancellationToken cancellationToken)
	{
		AdminVersions row = await _context.AdminVersions.AsNoTracking()
			.FirstOrDefaultAsync((AdminVersions v) => v.AvParamName == ReseauxSociauxLogic.SyncParam, cancellationToken);
		return row?.AvVersion ?? 0;
	}

	private void RefreshInBackground()
	{
		if (Interlocked.Exchange(ref _refreshing, 1) == 1)
		{
			return;
		}
		_ = Task.Run(async () =>
		{
			try
			{
				using IServiceScope scope = _scopes.CreateScope();
				var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
				var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
				await ReseauxPostsSync.RefreshAsync(db, configuration, CancellationToken.None);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Reseaux posts");
			}
			finally
			{
				Interlocked.Exchange(ref _refreshing, 0);
			}
		});
	}

	private static DateTime ParisNow()
	{
		return ReseauxSociauxLogic.ToParisUnspecified(DateTime.UtcNow, ReseauxSociauxLogic.ParisTimeZone());
	}
}
