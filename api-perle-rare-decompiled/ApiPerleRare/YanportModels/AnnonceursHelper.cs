using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using ApiPerleRare.Models;
using MySqlConnector;

namespace ApiPerleRare.YanportModels;

public class AnnonceursHelper
{
	private static readonly Regex rgx_phoneNumber = new Regex("\"PhoneNumber\":\"(?<phone>[^\"]+)\"", RegexOptions.Compiled);

	public static void Check(MySqlConnection connection, Property property)
	{
		Check(connection, property.PPropertyId, property.PAnnonceurs);
	}

	public static void Check(MySqlConnection connection, string propId, string annonceurs_json)
	{
		Dealer[] annonceurs = ((!string.IsNullOrEmpty(annonceurs_json)) ? JsonSerializer.Deserialize<Dealer[]>(annonceurs_json) : new Dealer[0]);
		Check(connection, propId, annonceurs);
	}

	public static void Check(MySqlConnection connection, string propId, Dealer[] annonceurs)
	{
		if (annonceurs == null)
		{
			annonceurs = new Dealer[0];
		}
		CheckIntermediairesDirects(connection, propId, annonceurs);
		CheckContactIntermediaires(connection, propId, annonceurs);
	}

	private static void CheckIntermediairesDirects(MySqlConnection connection, string propId, Dealer[] annonceurs)
	{
		List<uint> expected = new List<uint>();
		foreach (Dealer a in annonceurs)
		{
			uint annId = 0u;
			if (a.Id.HasValue && a.Id != 0)
			{
				using MySqlCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT idy.I_RefIntermediaire FROM intermediaires_directs_yanport idy WHERE idy.I_YanportId=@y";
				cmd.Parameters.AddWithValue("@y", a.Id);
				if (cmd.ExecuteScalar() is uint v)
				{
					annId = v;
				}
			}
			if (annId == 0 && !string.IsNullOrEmpty(a.Email))
			{
				using (MySqlCommand cmd2 = connection.CreateCommand())
				{
					cmd2.CommandText = "SELECT r.I_RefIntermediaire FROM intermediaires_directs r WHERE r.I_Mel=@mail";
					cmd2.Parameters.AddWithValue("@mail", a.Email.ToLower());
					if (cmd2.ExecuteScalar() is uint v2)
					{
						annId = v2;
					}
				}
				if (annId != 0 && a.Id.HasValue && a.Id != 0)
				{
					using MySqlCommand cmd3 = connection.CreateCommand();
					cmd3.CommandText = "INSERT INTO intermediaires_directs_yanport (I_RefIntermediaire, I_YanportId, I_Source) VALUES (@riid, @yid, @source)";
					cmd3.Parameters.AddWithValue("@riid", annId);
					cmd3.Parameters.AddWithValue("@yid", a.Id);
					cmd3.Parameters.AddWithValue("@source", "email");
					cmd3.ExecuteNonQuery();
				}
			}
			if (annId == 0 && !string.IsNullOrEmpty(a.PhoneNumber))
			{
				using (MySqlCommand cmd4 = connection.CreateCommand())
				{
					cmd4.CommandText = "SELECT r.I_RefIntermediaire FROM intermediaires_directs r WHERE r.I_Telephone=@tel";
					cmd4.Parameters.AddWithValue("@tel", NormalizePhoneNumber(a.PhoneNumber));
					if (cmd4.ExecuteScalar() is uint v3)
					{
						annId = v3;
					}
				}
				if (annId != 0 && a.Id.HasValue && a.Id != 0)
				{
					using MySqlCommand cmd5 = connection.CreateCommand();
					cmd5.CommandText = "INSERT INTO intermediaires_directs_yanport (I_RefIntermediaire, I_YanportId, I_Source) VALUES (@riid, @yid, @source)";
					cmd5.Parameters.AddWithValue("@riid", annId);
					cmd5.Parameters.AddWithValue("@yid", a.Id);
					cmd5.Parameters.AddWithValue("@source", "tel1");
					cmd5.ExecuteNonQuery();
				}
			}
			if (annId == 0 && !string.IsNullOrEmpty(a.PhoneNumber))
			{
				using (MySqlCommand cmd6 = connection.CreateCommand())
				{
					cmd6.CommandText = "SELECT r.I_RefIntermediaire FROM intermediaires_directs r WHERE r.I_Telephone2=@tel";
					cmd6.Parameters.AddWithValue("@tel", NormalizePhoneNumber(a.PhoneNumber));
					if (cmd6.ExecuteScalar() is uint v4)
					{
						annId = v4;
					}
				}
				if (annId != 0 && a.Id.HasValue && a.Id != 0)
				{
					using MySqlCommand cmd7 = connection.CreateCommand();
					cmd7.CommandText = "INSERT INTO intermediaires_directs_yanport (I_RefIntermediaire, I_YanportId, I_Source) VALUES (@riid, @yid, @source)";
					cmd7.Parameters.AddWithValue("@riid", annId);
					cmd7.Parameters.AddWithValue("@yid", a.Id);
					cmd7.Parameters.AddWithValue("@source", "tel2");
					cmd7.ExecuteNonQuery();
				}
			}
			if (annId != 0 && !expected.Contains(annId))
			{
				expected.Add(annId);
			}
		}
		List<uint> actual = new List<uint>();
		using (MySqlCommand cmd8 = connection.CreateCommand())
		{
			cmd8.CommandText = "SELECT r.I_RefIntermediaire FROM rel_property_inter_direct r WHERE r.P_PropertyId=@pid";
			cmd8.Parameters.AddWithValue("@pid", propId);
			using MySqlDataReader reader = cmd8.ExecuteReader();
			while (reader.Read())
			{
				actual.Add(reader.GetUInt32(0));
			}
		}
		foreach (uint newRef in expected.Except(actual))
		{
			using MySqlCommand cmd9 = connection.CreateCommand();
			cmd9.CommandText = "INSERT INTO rel_property_inter_direct (P_PropertyId, I_RefIntermediaire) VALUES (@pid, @riid)";
			cmd9.Parameters.AddWithValue("@pid", propId);
			cmd9.Parameters.AddWithValue("riid", newRef);
			cmd9.ExecuteNonQuery();
		}
		foreach (uint oldRef in actual.Except(expected))
		{
			using MySqlCommand cmd10 = connection.CreateCommand();
			cmd10.CommandText = "DELETE FROM rel_property_inter_direct WHERE P_PropertyId=@pid and I_RefIntermediaire=@riid";
			cmd10.Parameters.AddWithValue("@pid", propId);
			cmd10.Parameters.AddWithValue("riid", oldRef);
			cmd10.ExecuteNonQuery();
		}
	}

	private static void CheckContactIntermediaires(MySqlConnection connection, string propId, Dealer[] annonceurs)
	{
		List<uint> expected = new List<uint>();
		foreach (Dealer a in annonceurs)
		{
			uint annId = 0u;
			if (annId == 0 && !string.IsNullOrEmpty(a.PhoneNumber))
			{
				using MySqlCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT r.C_RefCtcInter FROM contact_intermediaire r WHERE r.C_Tel=@tel";
				cmd.Parameters.AddWithValue("@tel", NormalizePhoneNumber(a.PhoneNumber));
				if (cmd.ExecuteScalar() is uint v)
				{
					annId = v;
				}
			}
			if (annId == 0 && !string.IsNullOrEmpty(a.Email))
			{
				using MySqlCommand cmd2 = connection.CreateCommand();
				cmd2.CommandText = "SELECT r.C_RefCtcInter FROM contact_intermediaire r WHERE r.C_Mel=@email";
				cmd2.Parameters.AddWithValue("@email", a.Email);
				if (cmd2.ExecuteScalar() is uint v2)
				{
					annId = v2;
				}
			}
			if (annId != 0 && !expected.Contains(annId))
			{
				expected.Add(annId);
			}
		}
		List<uint> actual = new List<uint>();
		using (MySqlCommand cmd3 = connection.CreateCommand())
		{
			cmd3.CommandText = "SELECT r.C_RefCtcInter FROM rel_property_contact_inter r WHERE r.P_PropertyId=@pid";
			cmd3.Parameters.AddWithValue("@pid", propId);
			using MySqlDataReader reader = cmd3.ExecuteReader();
			while (reader.Read())
			{
				actual.Add(reader.GetUInt32(0));
			}
		}
		foreach (uint newRef in expected.Except(actual))
		{
			using MySqlCommand cmd4 = connection.CreateCommand();
			cmd4.CommandText = "INSERT INTO rel_property_contact_inter (P_PropertyId, C_RefCtcInter) VALUES (@pid, @riid)";
			cmd4.Parameters.AddWithValue("@pid", propId);
			cmd4.Parameters.AddWithValue("riid", newRef);
			cmd4.ExecuteNonQuery();
		}
		foreach (uint oldRef in actual.Except(expected))
		{
			using MySqlCommand cmd5 = connection.CreateCommand();
			cmd5.CommandText = "DELETE FROM rel_property_contact_inter WHERE P_PropertyId=@pid and C_RefCtcInter=@riid";
			cmd5.Parameters.AddWithValue("@pid", propId);
			cmd5.Parameters.AddWithValue("riid", oldRef);
			cmd5.ExecuteNonQuery();
		}
	}

	public static string NormalizePhoneNumbers(string dealers)
	{
		if (string.IsNullOrEmpty(dealers))
		{
			return dealers;
		}
		return rgx_phoneNumber.Replace(dealers, delegate(Match m)
		{
			string value = m.Groups["phone"].Value;
			return "\"PhoneNumber\":\"" + NormalizePhoneNumber(value) + "\"";
		});
	}

	public static string NormalizePhoneNumber(string phoneNumber)
	{
		string normalized = Regex.Replace(phoneNumber, "[^\\d+]+", "");
		if (normalized.Length == 9 && normalized.All((char c) => char.IsDigit(c)) && normalized[0] != '0')
		{
			return "0" + normalized;
		}
		if (normalized.Length == 12 && normalized.StartsWith("+33"))
		{
			return "0" + normalized.Substring(3);
		}
		if (normalized.Length == 14 && normalized.StartsWith("00233"))
		{
			return "0" + normalized.Substring(5);
		}
		return phoneNumber;
	}
}
