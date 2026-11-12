using System;
using System.IO;
using System.Linq;
using ApiPerleRare.Application.Formation;
using Xunit;

namespace ApiPerleRare.Tests;

public class FormationHistoireTests
{
	[Fact]
	public void Series_accepts_thirty_distinct_ids_and_rejects_the_rest()
	{
		int[] ids = Enumerable.Range(1, 30).ToArray();
		Assert.Equal(ids, FormationRules.RequireHistoireSeries("histoire", ids));
		Assert.Throws<FormationRejectedException>(() => FormationRules.RequireHistoireSeries("metier", ids));
		Assert.Throws<FormationRejectedException>(() => FormationRules.RequireHistoireSeries("histoire", Enumerable.Range(1, 29).ToArray()));
		int[] duplicated = Enumerable.Range(1, 29).Append(29).ToArray();
		Assert.Throws<FormationRejectedException>(() => FormationRules.RequireHistoireSeries("histoire", duplicated));
		int[] outOfCorpus = Enumerable.Range(1, 29).Append(121).ToArray();
		Assert.Throws<FormationRejectedException>(() => FormationRules.RequireHistoireSeries("histoire", outOfCorpus));
	}

	[Fact]
	public void Result_is_only_A_or_zero_and_the_order_must_match_the_draw()
	{
		Assert.Equal("A", FormationRules.RequireResultat("A"));
		Assert.Equal("0", FormationRules.RequireResultat("0"));
		Assert.Throws<FormationRejectedException>(() => FormationRules.RequireResultat("a"));
		Assert.Throws<FormationRejectedException>(() => FormationRules.RequireResultat("1"));
		FormationRules.RequireOrdre(2, 40, "12,40,7");
		Assert.Throws<FormationRejectedException>(() => FormationRules.RequireOrdre(2, 41, "12,40,7"));
		Assert.Throws<FormationRejectedException>(() => FormationRules.RequireOrdre(0, 12, "12,40,7"));
	}

	[Fact]
	public void Tone_is_green_from_25_orange_from_20_red_below()
	{
		Assert.Equal(2, FormationRules.Score(new[] { "A", "0", "A" }));
		Assert.Equal("vert", FormationRules.Tone(25));
		Assert.Equal("vert", FormationRules.Tone(30));
		Assert.Equal("orange", FormationRules.Tone(24));
		Assert.Equal("orange", FormationRules.Tone(20));
		Assert.Equal("rouge", FormationRules.Tone(19));
	}

	[Fact]
	public void Schema_file_creates_the_four_tables_and_loads_120_questions_without_being_applied_here()
	{
		string sql = File.ReadAllText(RepoFile("sql/formation-histoire.sql"));
		Assert.Contains("CREATE TABLE IF NOT EXISTS formation_question", sql);
		Assert.Contains("CREATE TABLE IF NOT EXISTS formation_serie", sql);
		Assert.Contains("CREATE TABLE IF NOT EXISTS formation_reponse", sql);
		Assert.Contains("CREATE TABLE IF NOT EXISTS formation_score", sql);
		Assert.Contains("fr_resultat CHAR(1)", sql);
		Assert.Contains("fs_user", sql);
		Assert.Contains("fsc_section", sql);
		Assert.Equal(120, sql.Split("INSERT INTO formation_question").Length - 1);
		Assert.DoesNotContain("DROP DATABASE", sql);
		string store = File.ReadAllText(RepoFile("api-perle-rare-decompiled/ApiPerleRare/Infrastructure/Formation/MariaDbFormationStore.cs"));
		Assert.Contains("table_name = @name", store);
		Assert.DoesNotContain("DoesTableExist", store);
		string controller = File.ReadAllText(RepoFile("api-perle-rare-decompiled/ApiPerleRare/Controllers/FormationController.cs"));
		Assert.Contains("[Authorize(Roles = \"Admin\")]", controller);
		Assert.Contains("StatusCode(503", controller);
		string startup = File.ReadAllText(RepoFile("api-perle-rare-decompiled/ApiPerleRare/Startup.cs"));
		Assert.Contains("IFormationStore, MariaDbFormationStore", startup);
	}

	private static string RepoFile(string relative)
	{
		DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null)
		{
			string candidate = Path.Combine(dir.FullName, relative);
			if (File.Exists(candidate))
			{
				return candidate;
			}
			dir = dir.Parent;
		}
		throw new FileNotFoundException(relative);
	}
}
