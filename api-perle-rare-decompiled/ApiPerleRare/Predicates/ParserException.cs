using System;

namespace ApiPerleRare.Predicates;

internal class ParserException : Exception
{
	private int _x;

	private int _y;

	private int _index;

	private int[] _expectedTokens;

	public string Expectation => GetExpectationText(_expectedTokens);

	public override string Message
	{
		get
		{
			if (_index == -1)
			{
				return "Erreur ligne " + (_y + 1) + ", colonne " + (_x + 1) + ": " + Expectation;
			}
			return "Erreur au caractère " + (_index + 1) + ": " + Expectation;
		}
	}

	public static void ThrowsMessage(string message)
	{
		throw new Exception(message);
	}

	public static void Throws(int x, int y, int[] expectedTokens)
	{
		throw new ParserException(x, y, -1, expectedTokens);
	}

	public static void Throws(int index, int[] expectedTokens)
	{
		throw new ParserException(-1, -1, index, expectedTokens);
	}

	public ParserException(int x, int y, int index, int[] expectedTokens)
	{
		_x = x;
		_y = y;
		_index = index;
		_expectedTokens = expectedTokens;
	}

	public static string GetExpectationText(int[] expected)
	{
		if (expected.Length == 1)
		{
			return Scanner.GetTokenTypeDescription(expected[0]) + " attendu";
		}
		string msg = "";
		foreach (int t in expected)
		{
			if (msg.Length > 0)
			{
				msg += " ou ";
			}
			msg += Scanner.GetTokenTypeDescription(t);
		}
		return msg + " attendus";
	}
}
