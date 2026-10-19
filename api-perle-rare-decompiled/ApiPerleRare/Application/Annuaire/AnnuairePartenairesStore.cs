using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Annuaire;

public sealed class AnnuairePartenairesStore : IAnnuairePartenairesStore
{
	private const string EnsureSql = @"
CREATE TABLE IF NOT EXISTS annuaire_partenaires_document (
  APD_Id TINYINT NOT NULL,
  APD_Json MEDIUMTEXT NOT NULL,
  APD_UpdatedAt DATETIME NOT NULL,
  PRIMARY KEY (APD_Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci";

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private static bool _tableReady;

	private readonly ApplicationDbContext _context;

	public AnnuairePartenairesStore(ApplicationDbContext context)
	{
		_context = context;
	}

	public AnnuairePartenairesDocument Read()
	{
		DbConnection connection = Open();
		EnsureTable(connection);
		using DbCommand command = connection.CreateCommand();
		command.CommandText = "SELECT APD_Json FROM annuaire_partenaires_document WHERE APD_Id = 1";
		object raw = command.ExecuteScalar();
		if (raw == null || raw == DBNull.Value)
		{
			return Empty();
		}
		List<AnnuairePartenaireRow> parsed;
		try
		{
			parsed = JsonSerializer.Deserialize<List<AnnuairePartenaireRow>>(Convert.ToString(raw), JsonOptions);
		}
		catch (JsonException)
		{
			return Empty();
		}
		List<AnnuairePartenaireRow> items = AnnuairePartenairesLogic.Clean(parsed);
		if (items.Count == 0)
		{
			return Empty();
		}
		return new AnnuairePartenairesDocument
		{
			Configured = true,
			Items = items
		};
	}

	public AnnuairePartenairesDocument Save(IReadOnlyList<AnnuairePartenaireRow> rows)
	{
		List<AnnuairePartenaireRow> items = AnnuairePartenairesLogic.Clean(rows);
		if (items.Count == 0)
		{
			throw new InvalidOperationException("Au moins un partenaire est requis.");
		}
		string json = JsonSerializer.Serialize(items, JsonOptions);
		DbConnection connection = Open();
		EnsureTable(connection);
		using DbCommand command = connection.CreateCommand();
		command.CommandText = @"
INSERT INTO annuaire_partenaires_document (APD_Id, APD_Json, APD_UpdatedAt)
VALUES (1, @json, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE APD_Json = @json, APD_UpdatedAt = UTC_TIMESTAMP()";
		DbParameter parameter = command.CreateParameter();
		parameter.ParameterName = "@json";
		parameter.Value = json;
		command.Parameters.Add(parameter);
		command.ExecuteNonQuery();
		return new AnnuairePartenairesDocument
		{
			Configured = true,
			Items = items
		};
	}

	private static AnnuairePartenairesDocument Empty()
	{
		return new AnnuairePartenairesDocument
		{
			Configured = false,
			Items = new List<AnnuairePartenaireRow>()
		};
	}

	private DbConnection Open()
	{
		DbConnection connection = _context.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
		{
			connection.Open();
		}
		return connection;
	}

	private static void EnsureTable(DbConnection connection)
	{
		if (_tableReady)
		{
			return;
		}
		using DbCommand command = connection.CreateCommand();
		command.CommandText = EnsureSql;
		command.ExecuteNonQuery();
		_tableReady = true;
	}
}
