using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ApiPerleRare.Application.Formation;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace ApiPerleRare.Infrastructure.Formation;

public class MariaDbFormationStore : IFormationStore
{
	private static readonly string[] Tables = { "formation_question", "formation_serie", "formation_reponse", "formation_score" };

	private readonly string _connectionString;

	public MariaDbFormationStore(IConfiguration configuration)
	{
		_connectionString = configuration.GetConnectionString("PerleRareDB");
	}

	public long CreateSeries(int userId, string section, IReadOnlyList<int> questions)
	{
		string packed = string.Join(",", questions.Select((int id) => id.ToString(CultureInfo.InvariantCulture)));
		using MySqlConnection connection = Open();
		using MySqlCommand cmd = connection.CreateCommand();
		cmd.CommandText = "INSERT INTO formation_serie (fs_user, fs_section, fs_questions, fs_created) VALUES (@user, @section, @questions, UTC_TIMESTAMP())";
		cmd.Parameters.Add("@user", MySqlDbType.Int32).Value = userId;
		cmd.Parameters.Add("@section", MySqlDbType.VarChar).Value = section;
		cmd.Parameters.Add("@questions", MySqlDbType.VarChar).Value = packed;
		cmd.ExecuteNonQuery();
		using MySqlCommand id = connection.CreateCommand();
		id.CommandText = "SELECT LAST_INSERT_ID()";
		return Convert.ToInt64(id.ExecuteScalar(), CultureInfo.InvariantCulture);
	}

	public void RecordAnswer(int userId, long serie, int ordre, int question, string resultat)
	{
		string mark = FormationRules.RequireResultat(resultat);
		using MySqlConnection connection = Open();
		SerieRow row = ReadSerie(connection, serie);
		if (row.UserId != userId)
		{
			throw new FormationForbiddenException();
		}
		FormationRules.RequireOrdre(ordre, question, row.Questions);
		using MySqlCommand cmd = connection.CreateCommand();
		cmd.CommandText = "INSERT INTO formation_reponse (fr_serie, fr_user, fr_ordre, fr_question, fr_resultat, fr_created) VALUES (@serie, @user, @ordre, @question, @resultat, UTC_TIMESTAMP()) ON DUPLICATE KEY UPDATE fr_resultat = @resultat, fr_question = @question, fr_created = UTC_TIMESTAMP()";
		cmd.Parameters.Add("@serie", MySqlDbType.Int64).Value = serie;
		cmd.Parameters.Add("@user", MySqlDbType.Int32).Value = userId;
		cmd.Parameters.Add("@ordre", MySqlDbType.Int32).Value = ordre;
		cmd.Parameters.Add("@question", MySqlDbType.Int32).Value = question;
		cmd.Parameters.Add("@resultat", MySqlDbType.VarChar).Value = mark;
		cmd.ExecuteNonQuery();
	}

	public int CloseScore(int userId, long serie)
	{
		using MySqlConnection connection = Open();
		SerieRow row = ReadSerie(connection, serie);
		if (row.UserId != userId)
		{
			throw new FormationForbiddenException();
		}
		List<string> marks = new List<string>();
		using (MySqlCommand cmd = connection.CreateCommand())
		{
			cmd.CommandText = "SELECT fr_ordre, fr_resultat FROM formation_reponse WHERE fr_serie = @serie AND fr_user = @user ORDER BY fr_ordre";
			cmd.Parameters.Add("@serie", MySqlDbType.Int64).Value = serie;
			cmd.Parameters.Add("@user", MySqlDbType.Int32).Value = userId;
			using MySqlDataReader reader = cmd.ExecuteReader();
			int expected = 1;
			while (reader.Read())
			{
				int ordre = Convert.ToInt32(reader["fr_ordre"], CultureInfo.InvariantCulture);
				string mark = Convert.ToString(reader["fr_resultat"], CultureInfo.InvariantCulture);
				if (ordre != expected || (mark != "A" && mark != "0"))
				{
					throw new FormationRejectedException("La série n'est pas terminée.");
				}
				marks.Add(mark);
				expected++;
			}
		}
		if (marks.Count != FormationRules.SeriesSize)
		{
			throw new FormationRejectedException("La série n'est pas terminée.");
		}
		int score = FormationRules.Score(marks);
		using MySqlCommand insert = connection.CreateCommand();
		insert.CommandText = "INSERT INTO formation_score (fsc_user, fsc_section, fsc_serie, fsc_score, fsc_total, fsc_created) VALUES (@user, @section, @serie, @score, @total, UTC_TIMESTAMP()) ON DUPLICATE KEY UPDATE fsc_score = @score, fsc_total = @total, fsc_created = UTC_TIMESTAMP()";
		insert.Parameters.Add("@user", MySqlDbType.Int32).Value = userId;
		insert.Parameters.Add("@section", MySqlDbType.VarChar).Value = row.Section;
		insert.Parameters.Add("@serie", MySqlDbType.Int64).Value = serie;
		insert.Parameters.Add("@score", MySqlDbType.Int32).Value = score;
		insert.Parameters.Add("@total", MySqlDbType.Int32).Value = FormationRules.SeriesSize;
		insert.ExecuteNonQuery();
		return score;
	}

	private MySqlConnection Open()
	{
		if (string.IsNullOrWhiteSpace(_connectionString))
		{
			throw new FormationSchemaMissingException();
		}
		MySqlConnection connection = new MySqlConnection(_connectionString);
		try
		{
			connection.Open();
			EnsureSchema(connection);
			return connection;
		}
		catch (MySqlException)
		{
			connection.Dispose();
			throw new FormationSchemaMissingException();
		}
		catch
		{
			connection.Dispose();
			throw;
		}
	}

	private static void EnsureSchema(MySqlConnection connection)
	{
		foreach (string name in Tables)
		{
			using MySqlCommand cmd = connection.CreateCommand();
			cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @name";
			cmd.Parameters.Add("@name", MySqlDbType.VarChar).Value = name;
			long count = Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
			if (count == 0)
			{
				throw new FormationSchemaMissingException();
			}
		}
	}

	private static SerieRow ReadSerie(MySqlConnection connection, long serie)
	{
		using MySqlCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT fs_user, fs_section, fs_questions FROM formation_serie WHERE fs_id = @serie";
		cmd.Parameters.Add("@serie", MySqlDbType.Int64).Value = serie;
		using MySqlDataReader reader = cmd.ExecuteReader();
		if (!reader.Read())
		{
			throw new FormationRejectedException("Série introuvable.");
		}
		return new SerieRow(
			Convert.ToInt32(reader["fs_user"], CultureInfo.InvariantCulture),
			Convert.ToString(reader["fs_section"], CultureInfo.InvariantCulture),
			Convert.ToString(reader["fs_questions"], CultureInfo.InvariantCulture));
	}

	private sealed class SerieRow
	{
		public SerieRow(int userId, string section, string questions)
		{
			UserId = userId;
			Section = section;
			Questions = questions;
		}

		public int UserId { get; }

		public string Section { get; }

		public string Questions { get; }
	}
}
