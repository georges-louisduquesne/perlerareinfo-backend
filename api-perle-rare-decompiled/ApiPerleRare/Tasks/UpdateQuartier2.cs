using System.Linq;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Tasks;

public class UpdateQuartier2 : AbstractTask
{
	private readonly ApplicationDbContext _dbContext;

	private readonly ILogger<UpdateQuartier2> _logger;

	public override string Description => "Mise à jour du champ AG_Quartier2";

	public UpdateQuartier2(ApplicationDbContext dbContext, ILogger<UpdateQuartier2> logger)
	{
		_dbContext = dbContext;
		_logger = logger;
	}

	public override void Run()
	{
		_logger.LogInformation("Chargement des quartiers...");
		Quartiers[] quartiers = _dbContext.Quartiers.Where((Quartiers q) => q.QActif == (bool?)true).AsNoTracking().ToArray();
		_logger.LogInformation($"Chargement des quartiers OK ({quartiers.Length})");
		quartiers = quartiers.Where((Quartiers q) => q.QNomQuartier != q.QTag && !string.IsNullOrEmpty(q.QNomQuartier) && !string.IsNullOrWhiteSpace(q.QTag)).ToArray();
		string[] nqs = quartiers.Select((Quartiers q) => q.QNomQuartier).Distinct().ToArray();
		_logger.LogInformation("Chargement des annonces avec un mauvais quartier...");
		AnnoncesGlobales[] annonces = _dbContext.AnnoncesGlobales.Where((AnnoncesGlobales a) => nqs.Contains(a.AgQuartier)).ToArray();
		annonces = annonces.Where((AnnoncesGlobales a) => nqs.Contains(a.AgQuartier)).ToArray();
		_logger.LogInformation($"Chargement des annonces avec un mauvais quartier OK ({annonces.Length})");
		int nb = 0;
		AnnoncesGlobales[] array = annonces;
		foreach (AnnoncesGlobales ag in array)
		{
			Quartiers[] qs = quartiers.Where((Quartiers q) => q.QNomQuartier == ag.AgQuartier).ToArray();
			if (qs.Length > 1)
			{
				qs = qs.Where((Quartiers q) => q.QCp.ToString() == ag.AgCp).ToArray();
			}
			if (qs.Length == 0)
			{
				continue;
			}
			if (qs.Length != 1)
			{
				_logger.LogInformation($"#{ag.AgRef} : pas le bon nombre de quartiers {ag.AgQuartier}/{ag.AgCp} trouvés ({qs.Length})");
			}
			else
			{
				ag.AgQuartier = qs.Single().QTag;
				nb++;
				if (nb % 100 == 0)
				{
					_dbContext.SaveChanges();
					_logger.LogInformation($"{nb}...");
				}
			}
		}
		_dbContext.SaveChanges();
		_logger.LogInformation($"{nb} annonces mises à jour");
	}

	public void RunMissing()
	{
		_logger.LogInformation("Chargement des quartiers...");
		Quartiers[] quartiers = _dbContext.Quartiers.Where((Quartiers quartiers2) => quartiers2.QActif == (bool?)true).AsNoTracking().ToArray();
		_logger.LogInformation($"Chargement des quartiers OK ({quartiers.Length})");
		_logger.LogInformation("Chargement des annonces sans quartier2...");
		AnnoncesGlobales[] withoutQuartiers2 = _dbContext.AnnoncesGlobales.Where((AnnoncesGlobales a) => a.AgQuartier != "" && (a.AgQuartier2 == null || a.AgQuartier2 == "")).ToArray();
		_logger.LogInformation($"Chargement des annonces sans quartier2 OK ({withoutQuartiers2.Length})");
		int nb = 0;
		AnnoncesGlobales[] array = withoutQuartiers2;
		foreach (AnnoncesGlobales ag in array)
		{
			Quartiers[] qs = quartiers.Where((Quartiers quartiers2) => quartiers2.QNomQuartier == ag.AgQuartier).ToArray();
			if (qs.Length == 0)
			{
				qs = quartiers.Where((Quartiers quartiers2) => quartiers2.QTag == ag.AgQuartier).ToArray();
			}
			if (qs.Length == 0)
			{
				continue;
			}
			if (qs.Length > 1)
			{
				qs = qs.Where((Quartiers quartiers2) => quartiers2.QCp.ToString() == ag.AgCp).ToArray();
			}
			if (qs.Length != 1)
			{
				_logger.LogInformation($"#{ag.AgRef} : pas le bon nombre de quartiers {ag.AgQuartier}/{ag.AgCp} trouvés ({qs.Length})");
				continue;
			}
			Quartiers q = qs.Single();
			ag.AgQuartier2 = $"[{q.QRef}]";
			if (ag.AgQuartier == q.QNomQuartier)
			{
				ag.AgQuartier = q.QTag;
			}
			nb++;
			if (nb % 100 == 0)
			{
				_dbContext.SaveChanges();
				_logger.LogInformation($"{nb}...");
			}
		}
		_dbContext.SaveChanges();
		_logger.LogInformation($"{nb} annonces mises à jour");
	}
}
