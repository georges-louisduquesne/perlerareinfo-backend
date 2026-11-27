using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Reseaux;

internal sealed class ReseauEngagement
{
	public int Likes { get; init; }

	public int Commentaires { get; init; }

	public string Likeurs { get; init; }
}

/// <summary>
/// Likes, commentaires et comptes qui ont aimé.
/// Colonnes non mappées : la page reste lisible tant que le script SQL n'est pas passé.
/// </summary>
internal static class ReseauxEngagementStore
{
	private static readonly ReseauEngagement Empty = new ReseauEngagement
	{
		Likes = 0,
		Commentaires = 0,
		Likeurs = string.Empty
	};

	internal static string Key(string reseau, string postId)
	{
		return (reseau ?? string.Empty) + "\n" + (postId ?? string.Empty);
	}

	internal static ReseauEngagement Of(IReadOnlyDictionary<string, ReseauEngagement> map, string reseau, string postId)
	{
		if (map != null && map.TryGetValue(Key(reseau, postId), out ReseauEngagement row))
		{
			return row;
		}
		return Empty;
	}

	internal static async Task<bool> ColumnsReady(ApplicationDbContext db, CancellationToken cancellationToken)
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
  AND TABLE_NAME = 'publications_reseaux'
  AND COLUMN_NAME = 'PUB_Likes'";
		object value = await command.ExecuteScalarAsync(cancellationToken);
		return Convert.ToInt32(value) > 0;
	}

	internal static async Task<Dictionary<string, ReseauEngagement>> LoadAsync(ApplicationDbContext db, CancellationToken cancellationToken)
	{
		var map = new Dictionary<string, ReseauEngagement>();
		if (!await ColumnsReady(db, cancellationToken))
		{
			return map;
		}
		var connection = db.Database.GetDbConnection();
		if (connection.State != System.Data.ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
		await using var command = connection.CreateCommand();
		command.CommandText = @"
SELECT PUB_Reseau, PUB_PostId, PUB_Likes, PUB_Commentaires, PUB_Likeurs
FROM publications_reseaux";
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			string reseau = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
			string postId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
			map[Key(reseau, postId)] = new ReseauEngagement
			{
				Likes = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2)),
				Commentaires = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3)),
				Likeurs = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
			};
		}
		return map;
	}

	internal static async Task SaveAsync(ApplicationDbContext db, IReadOnlyList<ReseauPost> posts, CancellationToken cancellationToken)
	{
		if (posts == null || posts.Count == 0 || !await ColumnsReady(db, cancellationToken))
		{
			return;
		}
		foreach (ReseauPost post in posts)
		{
			if (post == null || string.IsNullOrWhiteSpace(post.Id))
			{
				continue;
			}
			await db.Database.ExecuteSqlRawAsync(
				"UPDATE publications_reseaux SET PUB_Likes = {0}, PUB_Commentaires = {1}, PUB_Likeurs = {2} WHERE PUB_Reseau = {3} AND PUB_PostId = {4}",
				new object[]
				{
					post.Likes,
					post.Commentaires,
					post.Likeurs ?? string.Empty,
					post.Reseau ?? string.Empty,
					post.Id
				},
				cancellationToken);
		}
	}
}
