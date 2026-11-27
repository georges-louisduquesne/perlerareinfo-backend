using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Reseaux;

internal sealed class ReseauxConseillerRow
{
	public uint Ref { get; init; }

	public string Prenom { get; init; }

	public string Nom { get; init; }

	public string Tel { get; init; }

	public int Quota { get; init; }

	public int Solde { get; init; }

	public string Photo { get; init; }
}

/// <summary>
/// Lit CP_Quota_Reseaux et CP_Solde_Quota en SQL brut.
/// Ces colonnes ne sont pas mappées sur ConseillersPersonnels : l'API existante
/// continue de fonctionner tant que le script SQL n'est pas passé.
/// </summary>
internal static class ReseauxQuotaStore
{
	internal static async Task<bool> SchemaReady(ApplicationDbContext db, CancellationToken cancellationToken)
	{
		var connection = db.Database.GetDbConnection();
		if (connection.State != System.Data.ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
		await using var command = connection.CreateCommand();
		command.CommandText = @"
SELECT COUNT(*) FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'modeles_reseaux'
  AND COLUMN_NAME = 'MR_Id'";
		object value = await command.ExecuteScalarAsync(cancellationToken);
		return Convert.ToInt32(value) > 0;
	}

	internal static async Task<List<ReseauxConseillerRow>> ListActifs(ApplicationDbContext db, CancellationToken cancellationToken)
	{
		var rows = new List<ReseauxConseillerRow>();
		var connection = db.Database.GetDbConnection();
		if (connection.State != System.Data.ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
		await using var command = connection.CreateCommand();
		command.CommandText = @"
SELECT CP_RefConseiller, CP_Prenom, CP_NomFamille, CP_TelPersonnel, CP_Quota_Reseaux, CP_Solde_Quota, CP_PhotoSignature
FROM conseillers_personnels
WHERE " + ReseauxSociauxLogic.ConseillerActifPredicate + @"
ORDER BY CP_Solde_Quota DESC, CP_Prenom, CP_NomFamille";
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			rows.Add(new ReseauxConseillerRow
			{
				Ref = Convert.ToUInt32(reader.GetValue(0)),
				Prenom = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
				Nom = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
				Tel = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
				Quota = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4)),
				Solde = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5)),
				Photo = reader.IsDBNull(6) ? string.Empty : Convert.ToString(reader.GetValue(6)) ?? string.Empty
			});
		}
		return rows;
	}

	internal static async Task<ReseauxConseillerRow> ReadActif(ApplicationDbContext db, uint id, CancellationToken cancellationToken)
	{
		var connection = db.Database.GetDbConnection();
		if (connection.State != System.Data.ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
		await using var command = connection.CreateCommand();
		command.CommandText = @"
SELECT CP_RefConseiller, CP_Prenom, CP_NomFamille, CP_TelPersonnel, CP_Quota_Reseaux, CP_Solde_Quota
FROM conseillers_personnels
WHERE CP_RefConseiller = @id AND " + ReseauxSociauxLogic.ConseillerActifPredicate;
		var parameter = command.CreateParameter();
		parameter.ParameterName = "@id";
		parameter.Value = id;
		command.Parameters.Add(parameter);
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		if (!await reader.ReadAsync(cancellationToken))
		{
			return null;
		}
		return new ReseauxConseillerRow
		{
			Ref = Convert.ToUInt32(reader.GetValue(0)),
			Prenom = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
			Nom = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
			Tel = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
			Quota = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4)),
			Solde = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5))
		};
	}

	internal static async Task<bool> Save(ApplicationDbContext db, uint id, int quota, int solde, CancellationToken cancellationToken)
	{
		int updated = await db.Database.ExecuteSqlRawAsync(
			"UPDATE conseillers_personnels SET CP_Quota_Reseaux = {0}, CP_Solde_Quota = {1} WHERE CP_RefConseiller = {2} AND " + ReseauxSociauxLogic.ConseillerActifPredicate,
			new object[] { quota, solde, id },
			cancellationToken);
		return updated == 1;
	}
}
