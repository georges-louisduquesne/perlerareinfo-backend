using System;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Reseaux;

internal static class ReseauxQuotaReset
{
	internal const string EnvVar = "PR_RESEAUX_QUOTA_RESET";

	internal static bool Enabled(string value)
	{
		return !string.Equals(value?.Trim(), "off", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Le lundi, une nouvelle semaine ISO remet le solde de chaque conseiller actif à son quota.
	/// Le premier passage enregistre la semaine en cours sans recharger les soldes.
	/// </summary>
	internal static async Task<string> ApplyAsync(ApplicationDbContext db, DateTime parisNow, CancellationToken cancellationToken)
	{
		int current = ReseauxSociauxLogic.IsoWeekKey(parisNow);
		if (current <= 0)
		{
			return "none";
		}
		await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
		int inserted = await db.Database.ExecuteSqlInterpolatedAsync(
			$"INSERT IGNORE INTO admin_versions (AV_ParamName, AV_Version) VALUES ({ReseauxSociauxLogic.WeekParam}, {current})",
			cancellationToken);
		if (inserted == 1)
		{
			await tx.CommitAsync(cancellationToken);
			return "record";
		}
		int claimed = await db.Database.ExecuteSqlInterpolatedAsync(
			$"UPDATE admin_versions SET AV_Version = {current} WHERE AV_ParamName = {ReseauxSociauxLogic.WeekParam} AND AV_Version <> {current}",
			cancellationToken);
		if (claimed == 1)
		{
			await db.Database.ExecuteSqlRawAsync(
				"UPDATE conseillers_personnels SET CP_Solde_Quota = CP_Quota_Reseaux WHERE CP_Actif = 1",
				cancellationToken);
		}
		await tx.CommitAsync(cancellationToken);
		return claimed == 1 ? "reset" : "none";
	}
}
