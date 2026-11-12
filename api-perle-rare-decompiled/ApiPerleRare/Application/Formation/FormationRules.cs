using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiPerleRare.Application.Formation;

public static class FormationRules
{
	public const string HistoireSection = "histoire";

	public const int SeriesSize = 30;

	public const int QuestionCount = 120;

	public static int[] RequireHistoireSeries(string section, int[] questions)
	{
		if (section != HistoireSection)
		{
			throw new FormationRejectedException("La section attendue est histoire.");
		}
		if (questions == null || questions.Length != SeriesSize)
		{
			throw new FormationRejectedException("Une série Histoire compte 30 questions.");
		}
		HashSet<int> seen = new HashSet<int>();
		foreach (int id in questions)
		{
			if (id < 1 || id > QuestionCount || !seen.Add(id))
			{
				throw new FormationRejectedException("La série Histoire doit tirer 30 questions distinctes du corpus.");
			}
		}
		return questions.ToArray();
	}

	public static string RequireResultat(string resultat)
	{
		if (resultat == "A" || resultat == "0")
		{
			return resultat;
		}
		throw new FormationRejectedException("Le résultat est A ou 0.");
	}

	public static void RequireOrdre(int ordre, int question, string storedQuestions)
	{
		string[] parts = (storedQuestions ?? "").Split(',');
		if (ordre < 1 || ordre > parts.Length || !int.TryParse(parts[ordre - 1], out int expected) || expected != question)
		{
			throw new FormationRejectedException("La question ne correspond pas à cet ordre dans la série.");
		}
	}

	public static int Score(IEnumerable<string> marks)
	{
		return marks.Count((string mark) => mark == "A");
	}

	public static string Tone(int score)
	{
		if (score >= 25)
		{
			return "vert";
		}
		if (score >= 20)
		{
			return "orange";
		}
		return "rouge";
	}
}

public class FormationRejectedException : Exception
{
	public FormationRejectedException(string message)
		: base(message)
	{
	}
}

public class FormationSchemaMissingException : Exception
{
	public FormationSchemaMissingException()
		: base("Les tables Formation ne sont pas en place.")
	{
	}
}

public class FormationForbiddenException : Exception
{
	public FormationForbiddenException()
		: base("Cette série appartient à un autre utilisateur.")
	{
	}
}
