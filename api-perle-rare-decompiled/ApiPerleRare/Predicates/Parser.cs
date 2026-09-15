using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ApiPerleRare.Predicates;

internal class Parser
{
	private Scanner _scanner;

	private List<ParserValue> _values = new List<ParserValue>();

	private int _pos = 0;

	private int _reduction = 0;

	public Parser(Scanner scanner)
	{
		_scanner = scanner;
	}

	public IValue Parse()
	{
		Eval0();
		if (_reduction != 0 || _pos != 1 || _values.Count != 2)
		{
			ParserException.ThrowsMessage("Problème de conception!");
		}
		return (IValue)_values[0].Value;
	}

	private static readonly Regex ODataDateTimeLiteral = new Regex(
		@"\bdatetime(?:offset)?'([^']*)'",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	public static IValue Parse(string text)
	{
		Scanner scanner = new Scanner(UnwrapODataDateTimeLiterals(text));
		Parser parser = new Parser(scanner);
		return parser.Parse();
	}

	internal static string UnwrapODataDateTimeLiterals(string text)
	{
		if (string.IsNullOrEmpty(text) || text.IndexOf("datetime", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return text;
		}
		return ODataDateTimeLiteral.Replace(text, "$1");
	}

	private void Eval0()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 4:
				Eval1();
				break;
			case 5:
				Eval2();
				break;
			case 8:
				Eval3();
				break;
			case 12:
				Eval4();
				break;
			case 13:
				Eval5();
				break;
			case 14:
				Eval6();
				break;
			case 15:
				Eval7();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			case 3:
				return;
			default:
				Fail(0);
				break;
			}
		}
		_reduction--;
	}

	private void Eval1()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken2();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2)
			{
				_pos--;
				_values[_pos].SymbolId = 3;
				return;
			}
			_pos++;
			Fail(1);
		}
		_reduction--;
	}

	private void Eval2()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken3();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 36 || symbol == 7 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 4;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 6:
				Eval27();
				break;
			case 9:
				Eval28();
				break;
			default:
				Fail(2);
				break;
			}
		}
		_reduction--;
	}

	private void Eval3()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken4();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 5;
				return;
			}
			_pos++;
			if (symbol == 11)
			{
				Eval29();
			}
			else
			{
				Fail(3);
			}
		}
		_reduction--;
	}

	private void Eval4()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken5();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 8;
				return;
			}
			_pos++;
			if (symbol == 33)
			{
				Eval30();
			}
			else
			{
				Fail(4);
			}
		}
		_reduction--;
	}

	private void Eval5()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken6();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 12;
				return;
			}
			_pos++;
			if (symbol == 32)
			{
				Eval31();
			}
			else
			{
				Fail(5);
			}
		}
		_reduction--;
	}

	private void Eval6()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken7();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 13;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 25:
				Eval32();
				break;
			case 26:
				Eval33();
				break;
			case 27:
				Eval34();
				break;
			default:
				Fail(6);
				break;
			}
		}
		_reduction--;
	}

	private void Eval7()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken8();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 14;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 28:
				Eval35();
				break;
			case 29:
				Eval36();
				break;
			case 30:
				Eval37();
				break;
			case 31:
				Eval38();
				break;
			default:
				Fail(7);
				break;
			}
		}
		_reduction--;
	}

	private void Eval8()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken9();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 15;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 20:
				Eval39();
				break;
			case 21:
				Eval40();
				break;
			default:
				Fail(8);
				break;
			}
		}
		_reduction--;
	}

	private void Eval9()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 16;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 22:
				Eval41();
				break;
			case 23:
				Eval42();
				break;
			case 24:
				Eval43();
				break;
			default:
				Fail(9);
				break;
			}
		}
		_reduction--;
	}

	private void Eval10()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 17;
				return;
			}
			_pos++;
			Fail(10);
		}
		_reduction--;
	}

	private void Eval11()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 18;
				return;
			}
			_pos++;
			Fail(11);
		}
		_reduction--;
	}

	private void Eval12()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken11();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 39 || symbol == 40 || symbol == 46 || symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 38;
				return;
			}
			_pos++;
			if (symbol == 35)
			{
				Eval44();
			}
			else
			{
				Fail(12);
			}
		}
		_reduction--;
	}

	private void Eval13()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken12();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce35((string)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(43, v);
				_reduction = 0;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 39:
				Eval45();
				break;
			case 40:
				Eval46();
				break;
			case 46:
				Eval47();
				break;
			default:
				Fail(13);
				break;
			}
		}
		_reduction--;
	}

	private void Eval14()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken13();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 43:
				Eval48();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 38:
				Eval49();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			case 34:
				Eval50();
				break;
			default:
				Fail(14);
				break;
			}
		}
		_reduction--;
	}

	private void Eval15()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken13();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 43:
				Eval51();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 38:
				Eval49();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			case 34:
				Eval50();
				break;
			default:
				Fail(15);
				break;
			}
		}
		_reduction--;
	}

	private void Eval16()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 19;
				return;
			}
			_pos++;
			Fail(16);
		}
		_reduction--;
	}

	private void Eval17()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce25((string)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(43, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(17);
		}
		_reduction--;
	}

	private void Eval18()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce26((string)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(43, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(18);
		}
		_reduction--;
	}

	private void Eval19()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce27((StringValue)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(43, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(19);
		}
		_reduction--;
	}

	private void Eval20()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce28((string)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(43, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(20);
		}
		_reduction--;
	}

	private void Eval21()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce29((string)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(43, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(21);
		}
		_reduction--;
	}

	private void Eval22()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce30();
				_pos--;
				_values[_pos] = new ParserValue(43, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(22);
		}
		_reduction--;
	}

	private void Eval23()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce31();
				_pos--;
				_values[_pos] = new ParserValue(43, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(23);
		}
		_reduction--;
	}

	private void Eval24()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 4:
				Eval52();
				break;
			case 5:
				Eval2();
				break;
			case 8:
				Eval3();
				break;
			case 12:
				Eval4();
				break;
			case 13:
				Eval5();
				break;
			case 14:
				Eval6();
				break;
			case 15:
				Eval7();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(24);
				break;
			}
		}
		_reduction--;
	}

	private void Eval25()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				StringValue v = Reduce36((string)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(49, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(25);
		}
		_reduction--;
	}

	private void Eval26()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				StringValue v = Reduce37((string)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(49, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(26);
		}
		_reduction--;
	}

	private void Eval27()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 4:
				Eval53();
				break;
			case 5:
				Eval2();
				break;
			case 8:
				Eval3();
				break;
			case 12:
				Eval4();
				break;
			case 13:
				Eval5();
				break;
			case 14:
				Eval6();
				break;
			case 15:
				Eval7();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(27);
				break;
			}
		}
		_reduction--;
	}

	private void Eval28()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 5:
				Eval54();
				break;
			case 8:
				Eval3();
				break;
			case 12:
				Eval4();
				break;
			case 13:
				Eval5();
				break;
			case 14:
				Eval6();
				break;
			case 15:
				Eval7();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(28);
				break;
			}
		}
		_reduction--;
	}

	private void Eval29()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 12:
				Eval55();
				break;
			case 13:
				Eval5();
				break;
			case 14:
				Eval6();
				break;
			case 15:
				Eval7();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(29);
				break;
			}
		}
		_reduction--;
	}

	private void Eval30()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 13:
				Eval56();
				break;
			case 14:
				Eval6();
				break;
			case 15:
				Eval7();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(30);
				break;
			}
		}
		_reduction--;
	}

	private void Eval31()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 14:
				Eval57();
				break;
			case 15:
				Eval7();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(31);
				break;
			}
		}
		_reduction--;
	}

	private void Eval32()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 15:
				Eval58();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(32);
				break;
			}
		}
		_reduction--;
	}

	private void Eval33()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 15:
				Eval59();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(33);
				break;
			}
		}
		_reduction--;
	}

	private void Eval34()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 15:
				Eval60();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(34);
				break;
			}
		}
		_reduction--;
	}

	private void Eval35()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 16:
				Eval61();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(35);
				break;
			}
		}
		_reduction--;
	}

	private void Eval36()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 16:
				Eval62();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(36);
				break;
			}
		}
		_reduction--;
	}

	private void Eval37()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 16:
				Eval63();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(37);
				break;
			}
		}
		_reduction--;
	}

	private void Eval38()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 16:
				Eval64();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(38);
				break;
			}
		}
		_reduction--;
	}

	private void Eval39()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 17:
				Eval65();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(39);
				break;
			}
		}
		_reduction--;
	}

	private void Eval40()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 17:
				Eval66();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(40);
				break;
			}
		}
		_reduction--;
	}

	private void Eval41()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 18:
				Eval67();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(41);
				break;
			}
		}
		_reduction--;
	}

	private void Eval42()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 18:
				Eval68();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(42);
				break;
			}
		}
		_reduction--;
	}

	private void Eval43()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 18:
				Eval69();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(43);
				break;
			}
		}
		_reduction--;
	}

	private void Eval44()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken14();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 36:
				Eval70();
				break;
			case 37:
				Eval71();
				break;
			case 4:
				Eval72();
				break;
			case 5:
				Eval2();
				break;
			case 8:
				Eval3();
				break;
			case 12:
				Eval4();
				break;
			case 13:
				Eval5();
				break;
			case 14:
				Eval6();
				break;
			case 15:
				Eval7();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(44);
				break;
			}
		}
		_reduction--;
	}

	private void Eval45()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken15();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			if (symbol == 35)
			{
				Eval73();
			}
			else
			{
				Fail(45);
			}
		}
		_reduction--;
	}

	private void Eval46()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken16();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 41:
				Eval74();
				break;
			case 42:
				Eval75();
				break;
			default:
				Fail(46);
				break;
			}
		}
		_reduction--;
	}

	private void Eval47()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken17();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			if (symbol == 34)
			{
				Eval76();
			}
			else
			{
				Fail(47);
			}
		}
		_reduction--;
	}

	private void Eval48()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce32((IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 1, 1);
				_pos -= 2;
				_values[_pos] = new ParserValue(19, v);
				_reduction = 1;
				return;
			}
			_pos++;
			Fail(48);
		}
		_reduction--;
	}

	private void Eval49()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken18();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce35((string)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(43, v);
				_reduction = 0;
				return;
			}
			_pos++;
			if (symbol == 46)
			{
				Eval47();
			}
			else
			{
				Fail(49);
			}
		}
		_reduction--;
	}

	private void Eval50()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken18();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 46 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				_pos--;
				_values[_pos].SymbolId = 38;
				return;
			}
			_pos++;
			Fail(50);
		}
		_reduction--;
	}

	private void Eval51()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce33((IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 1, 1);
				_pos -= 2;
				_values[_pos] = new ParserValue(19, v);
				_reduction = 1;
				return;
			}
			_pos++;
			Fail(51);
		}
		_reduction--;
	}

	private void Eval52()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken19();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			if (symbol == 36)
			{
				Eval77();
			}
			else
			{
				Fail(52);
			}
		}
		_reduction--;
	}

	private void Eval53()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken20();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			if (symbol == 7)
			{
				Eval78();
			}
			else
			{
				Fail(53);
			}
		}
		_reduction--;
	}

	private void Eval54()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken21();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 10:
				Eval79();
				break;
			case 9:
				Eval28();
				break;
			default:
				Fail(54);
				break;
			}
		}
		_reduction--;
	}

	private void Eval55()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken5();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce21((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(8, v);
				_reduction = 2;
				return;
			}
			_pos++;
			if (symbol == 33)
			{
				Eval30();
			}
			else
			{
				Fail(55);
			}
		}
		_reduction--;
	}

	private void Eval56()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken6();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce18((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(12, v);
				_reduction = 2;
				return;
			}
			_pos++;
			if (symbol == 32)
			{
				Eval31();
			}
			else
			{
				Fail(56);
			}
		}
		_reduction--;
	}

	private void Eval57()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken7();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce17((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(13, v);
				_reduction = 2;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 25:
				Eval32();
				break;
			case 26:
				Eval33();
				break;
			case 27:
				Eval34();
				break;
			default:
				Fail(57);
				break;
			}
		}
		_reduction--;
	}

	private void Eval58()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken8();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce10((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(14, v);
				_reduction = 2;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 28:
				Eval35();
				break;
			case 29:
				Eval36();
				break;
			case 30:
				Eval37();
				break;
			case 31:
				Eval38();
				break;
			default:
				Fail(58);
				break;
			}
		}
		_reduction--;
	}

	private void Eval59()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken8();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce11((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(14, v);
				_reduction = 2;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 28:
				Eval35();
				break;
			case 29:
				Eval36();
				break;
			case 30:
				Eval37();
				break;
			case 31:
				Eval38();
				break;
			default:
				Fail(59);
				break;
			}
		}
		_reduction--;
	}

	private void Eval60()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken8();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce12((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(14, v);
				_reduction = 2;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 28:
				Eval35();
				break;
			case 29:
				Eval36();
				break;
			case 30:
				Eval37();
				break;
			case 31:
				Eval38();
				break;
			default:
				Fail(60);
				break;
			}
		}
		_reduction--;
	}

	private void Eval61()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken9();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce13((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(15, v);
				_reduction = 2;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 20:
				Eval39();
				break;
			case 21:
				Eval40();
				break;
			default:
				Fail(61);
				break;
			}
		}
		_reduction--;
	}

	private void Eval62()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken9();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce14((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(15, v);
				_reduction = 2;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 20:
				Eval39();
				break;
			case 21:
				Eval40();
				break;
			default:
				Fail(62);
				break;
			}
		}
		_reduction--;
	}

	private void Eval63()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken9();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce15((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(15, v);
				_reduction = 2;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 20:
				Eval39();
				break;
			case 21:
				Eval40();
				break;
			default:
				Fail(63);
				break;
			}
		}
		_reduction--;
	}

	private void Eval64()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken9();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce16((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(15, v);
				_reduction = 2;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 20:
				Eval39();
				break;
			case 21:
				Eval40();
				break;
			default:
				Fail(64);
				break;
			}
		}
		_reduction--;
	}

	private void Eval65()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce5((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(16, v);
				_reduction = 2;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 22:
				Eval41();
				break;
			case 23:
				Eval42();
				break;
			case 24:
				Eval43();
				break;
			default:
				Fail(65);
				break;
			}
		}
		_reduction--;
	}

	private void Eval66()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce6((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(16, v);
				_reduction = 2;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 22:
				Eval41();
				break;
			case 23:
				Eval42();
				break;
			case 24:
				Eval43();
				break;
			default:
				Fail(66);
				break;
			}
		}
		_reduction--;
	}

	private void Eval67()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce7((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(17, v);
				_reduction = 2;
				return;
			}
			_pos++;
			Fail(67);
		}
		_reduction--;
	}

	private void Eval68()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce8((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(17, v);
				_reduction = 2;
				return;
			}
			_pos++;
			Fail(68);
		}
		_reduction--;
	}

	private void Eval69()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce9((IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(17, v);
				_reduction = 2;
				return;
			}
			_pos++;
			Fail(69);
		}
		_reduction--;
	}

	private void Eval70()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce19((string)_values[_pos - 3].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(18, v);
				_reduction = 2;
				return;
			}
			_pos++;
			Fail(70);
		}
		_reduction--;
	}

	private void Eval71()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken22();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 36:
				Eval80();
				break;
			case 45:
				Eval81();
				break;
			default:
				Fail(71);
				break;
			}
		}
		_reduction--;
	}

	private void Eval72()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken22();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 36 || symbol == 45)
			{
				List<IValue> v = Reduce38((IValue)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(37, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(72);
		}
		_reduction--;
	}

	private void Eval73()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 37:
				Eval82();
				break;
			case 4:
				Eval72();
				break;
			case 5:
				Eval2();
				break;
			case 8:
				Eval3();
				break;
			case 12:
				Eval4();
				break;
			case 13:
				Eval5();
				break;
			case 14:
				Eval6();
				break;
			case 15:
				Eval7();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(73);
				break;
			}
		}
		_reduction--;
	}

	private void Eval74()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce23((string)_values[_pos - 3].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(18, v);
				_reduction = 2;
				return;
			}
			_pos++;
			Fail(74);
		}
		_reduction--;
	}

	private void Eval75()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken23();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			if (symbol == 41)
			{
				Eval83();
			}
			else
			{
				Fail(75);
			}
		}
		_reduction--;
	}

	private void Eval76()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken12();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 39 || symbol == 40 || symbol == 46 || symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				string v = Reduce41((string)_values[_pos - 3].Value, (string)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(38, v);
				_reduction = 2;
				return;
			}
			_pos++;
			Fail(76);
		}
		_reduction--;
	}

	private void Eval77()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce34((IValue)_values[_pos - 2].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(43, v);
				_reduction = 2;
				return;
			}
			_pos++;
			Fail(77);
		}
		_reduction--;
	}

	private void Eval78()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 4:
				Eval84();
				break;
			case 5:
				Eval2();
				break;
			case 8:
				Eval3();
				break;
			case 12:
				Eval4();
				break;
			case 13:
				Eval5();
				break;
			case 14:
				Eval6();
				break;
			case 15:
				Eval7();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(78);
				break;
			}
		}
		_reduction--;
	}

	private void Eval79()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken24();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 45 || symbol == 10)
			{
				IValue v = Reduce3((IValue)_values[_pos - 4].Value, (IValue)_values[_pos - 2].Value);
				_values.RemoveRange(_pos - 3, 3);
				_pos -= 4;
				_values[_pos] = new ParserValue(5, v);
				_reduction = 3;
				return;
			}
			_pos++;
			Fail(79);
		}
		_reduction--;
	}

	private void Eval80()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce20((string)_values[_pos - 4].Value, (List<IValue>)_values[_pos - 2].Value);
				_values.RemoveRange(_pos - 3, 3);
				_pos -= 4;
				_values[_pos] = new ParserValue(18, v);
				_reduction = 3;
				return;
			}
			_pos++;
			Fail(80);
		}
		_reduction--;
	}

	private void Eval81()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken1();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 4:
				Eval85();
				break;
			case 5:
				Eval2();
				break;
			case 8:
				Eval3();
				break;
			case 12:
				Eval4();
				break;
			case 13:
				Eval5();
				break;
			case 14:
				Eval6();
				break;
			case 15:
				Eval7();
				break;
			case 16:
				Eval8();
				break;
			case 17:
				Eval9();
				break;
			case 18:
				Eval10();
				break;
			case 19:
				Eval11();
				break;
			case 34:
				Eval12();
				break;
			case 38:
				Eval13();
				break;
			case 21:
				Eval14();
				break;
			case 44:
				Eval15();
				break;
			case 43:
				Eval16();
				break;
			case 47:
				Eval17();
				break;
			case 48:
				Eval18();
				break;
			case 49:
				Eval19();
				break;
			case 50:
				Eval20();
				break;
			case 51:
				Eval21();
				break;
			case 52:
				Eval22();
				break;
			case 53:
				Eval23();
				break;
			case 35:
				Eval24();
				break;
			case 54:
				Eval25();
				break;
			case 55:
				Eval26();
				break;
			default:
				Fail(81);
				break;
			}
		}
		_reduction--;
	}

	private void Eval82()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken22();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 36:
				Eval86();
				break;
			case 45:
				Eval81();
				break;
			default:
				Fail(82);
				break;
			}
		}
		_reduction--;
	}

	private void Eval83()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce24((string)_values[_pos - 4].Value);
				_values.RemoveRange(_pos - 3, 3);
				_pos -= 4;
				_values[_pos] = new ParserValue(18, v);
				_reduction = 3;
				return;
			}
			_pos++;
			Fail(83);
		}
		_reduction--;
	}

	private void Eval84()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken25();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 36 || symbol == 7 || symbol == 45)
			{
				IValue v = Reduce1((IValue)_values[_pos - 5].Value, (IValue)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 4, 4);
				_pos -= 5;
				_values[_pos] = new ParserValue(4, v);
				_reduction = 4;
				return;
			}
			_pos++;
			Fail(84);
		}
		_reduction--;
	}

	private void Eval85()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken22();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 36 || symbol == 45)
			{
				List<IValue> v = Reduce39((List<IValue>)_values[_pos - 3].Value, (IValue)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(37, v);
				_reduction = 2;
				return;
			}
			_pos++;
			Fail(85);
		}
		_reduction--;
	}

	private void Eval86()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken10();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6 || symbol == 11 || symbol == 33 || symbol == 32 || symbol == 25 || symbol == 26 || symbol == 27 || symbol == 28 || symbol == 29 || symbol == 30 || symbol == 31 || symbol == 20 || symbol == 21 || symbol == 22 || symbol == 23 || symbol == 24 || symbol == 9 || symbol == 36 || symbol == 7 || symbol == 10 || symbol == 45)
			{
				IValue v = Reduce22((string)_values[_pos - 5].Value, (List<IValue>)_values[_pos - 2].Value);
				_values.RemoveRange(_pos - 4, 4);
				_pos -= 5;
				_values[_pos] = new ParserValue(18, v);
				_reduction = 4;
				return;
			}
			_pos++;
			Fail(86);
		}
		_reduction--;
	}

	private IValue Reduce1(IValue p0, IValue p1, IValue p2)
	{
		return new ConditionalValue(p0, p1, p2);
	}

	private IValue Reduce3(IValue p0, IValue p1)
	{
		return new MethodValue("GetPropertyValue", p0, p1);
	}

	private IValue Reduce5(IValue p0, IValue p1)
	{
		return new BinaryOperationValue(p0, p1, BinaryOperator.Addition);
	}

	private IValue Reduce6(IValue p0, IValue p1)
	{
		return new BinaryOperationValue(p0, p1, BinaryOperator.Substraction);
	}

	private IValue Reduce7(IValue p0, IValue p1)
	{
		return new BinaryOperationValue(p0, p1, BinaryOperator.Multiplication);
	}

	private IValue Reduce8(IValue p0, IValue p1)
	{
		return new BinaryOperationValue(p0, p1, BinaryOperator.Division);
	}

	private IValue Reduce9(IValue p0, IValue p1)
	{
		return new BinaryOperationValue(p0, p1, BinaryOperator.Modulo);
	}

	private IValue Reduce10(IValue p0, IValue p1)
	{
		return new ComparisonValue(p0, p1, ComparisonOperator.Equal);
	}

	private IValue Reduce11(IValue p0, IValue p1)
	{
		return new ComparisonValue(p0, p1, ComparisonOperator.Like);
	}

	private IValue Reduce12(IValue p0, IValue p1)
	{
		return new ComparisonValue(p0, p1, ComparisonOperator.NotEqual);
	}

	private IValue Reduce13(IValue p0, IValue p1)
	{
		return new ComparisonValue(p0, p1, ComparisonOperator.Less);
	}

	private IValue Reduce14(IValue p0, IValue p1)
	{
		return new ComparisonValue(p0, p1, ComparisonOperator.LessOrEqual);
	}

	private IValue Reduce15(IValue p0, IValue p1)
	{
		return new ComparisonValue(p0, p1, ComparisonOperator.Greater);
	}

	private IValue Reduce16(IValue p0, IValue p1)
	{
		return new ComparisonValue(p0, p1, ComparisonOperator.GreaterOrEqual);
	}

	private IValue Reduce17(IValue p0, IValue p1)
	{
		return LogicalValue.Apply(LogicalOperator.And, p0, p1);
	}

	private IValue Reduce18(IValue p0, IValue p1)
	{
		return LogicalValue.Apply(LogicalOperator.Or, p0, p1);
	}

	private IValue Reduce19(string p0)
	{
		return new MethodValue(p0);
	}

	private IValue Reduce20(string p0, List<IValue> p1)
	{
		return new MethodValue(p0, p1.ToArray());
	}

	private IValue Reduce21(IValue p0, IValue p1)
	{
		return new BinaryOperationValue(p0, p1, BinaryOperator.NullCoalescing);
	}

	private IValue Reduce22(string p0, List<IValue> p1)
	{
		return new InValue(p0, p1);
	}

	private IValue Reduce23(string p0)
	{
		return new UnaryOperationValue(new IdentifierValue(p0), UnaryOperator.Null);
	}

	private IValue Reduce24(string p0)
	{
		return new UnaryOperationValue(new IdentifierValue(p0), UnaryOperator.NotNull);
	}

	private IValue Reduce25(string p0)
	{
		return new IntegerValue(int.Parse(p0));
	}

	private IValue Reduce26(string p0)
	{
		return new DoubleValue(double.Parse(p0, CultureInfo.InvariantCulture));
	}

	private IValue Reduce27(StringValue p0)
	{
		return p0;
	}

	private IValue Reduce28(string p0)
	{
		return new DateTimeValue(p0 + "T00:00:00");
	}

	private IValue Reduce29(string p0)
	{
		return new DateTimeValue(p0);
	}

	private IValue Reduce30()
	{
		return new BooleanValue(value: true);
	}

	private IValue Reduce31()
	{
		return new BooleanValue(value: false);
	}

	private IValue Reduce32(IValue p0)
	{
		return new UnaryOperationValue(p0, UnaryOperator.Neg);
	}

	private IValue Reduce33(IValue p0)
	{
		return new UnaryOperationValue(p0, UnaryOperator.Not);
	}

	private IValue Reduce34(IValue p0)
	{
		return p0;
	}

	private IValue Reduce35(string p0)
	{
		return new PropertyValue(p0);
	}

	private StringValue Reduce36(string p0)
	{
		return new StringValue(p0.Substring(1, p0.Length - 2).Replace("''", "'"));
	}

	private StringValue Reduce37(string p0)
	{
		return new StringValue(p0.Substring(1, p0.Length - 2).Replace("\"\"", "\""));
	}

	private List<IValue> Reduce38(IValue p0)
	{
		List<IValue> returnedValue = new List<IValue>();
		returnedValue.Add(p0);
		return returnedValue;
	}

	private List<IValue> Reduce39(List<IValue> p0, IValue p1)
	{
		p0.Add(p1);
		return p0;
	}

	private string Reduce41(string p0, string p1)
	{
		return p0 + "." + p1;
	}

	private void Fail(int currentNodeId)
	{
		ParserException.Throws(_scanner.TokenCharPosX, _scanner.TokenCharPosY, GetExpectedTokens(currentNodeId));
	}

	public static int[] GetExpectedTokens(int nodeId)
	{
		switch (nodeId)
		{
		case 0:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 1:
			return new int[1] { 2 };
		case 2:
			return new int[6] { 6, 9, 2, 36, 7, 45 };
		case 3:
			return new int[8] { 11, 2, 6, 9, 36, 7, 10, 45 };
		case 4:
			return new int[9] { 33, 2, 6, 11, 9, 36, 7, 10, 45 };
		case 5:
			return new int[10] { 32, 2, 6, 11, 33, 9, 36, 7, 10, 45 };
		case 6:
			return new int[13]
			{
				25, 26, 27, 2, 6, 11, 33, 32, 9, 36,
				7, 10, 45
			};
		case 7:
			return new int[17]
			{
				28, 29, 30, 31, 2, 6, 11, 33, 32, 25,
				26, 27, 9, 36, 7, 10, 45
			};
		case 8:
			return new int[19]
			{
				20, 21, 2, 6, 11, 33, 32, 25, 26, 27,
				28, 29, 30, 31, 9, 36, 7, 10, 45
			};
		case 9:
			return new int[22]
			{
				22, 23, 24, 2, 6, 11, 33, 32, 25, 26,
				27, 28, 29, 30, 31, 20, 21, 9, 36, 7,
				10, 45
			};
		case 10:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 11:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 12:
			return new int[26]
			{
				35, 39, 40, 46, 2, 6, 11, 33, 32, 25,
				26, 27, 28, 29, 30, 31, 20, 21, 22, 23,
				24, 9, 36, 7, 10, 45
			};
		case 13:
			return new int[25]
			{
				39, 40, 46, 2, 6, 11, 33, 32, 25, 26,
				27, 28, 29, 30, 31, 20, 21, 22, 23, 24,
				9, 36, 7, 10, 45
			};
		case 14:
			return new int[10] { 47, 48, 50, 51, 52, 53, 35, 54, 55, 34 };
		case 15:
			return new int[10] { 47, 48, 50, 51, 52, 53, 35, 54, 55, 34 };
		case 16:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 17:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 18:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 19:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 20:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 21:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 22:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 23:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 24:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 25:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 26:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 27:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 28:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 29:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 30:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 31:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 32:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 33:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 34:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 35:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 36:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 37:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 38:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 39:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 40:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 41:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 42:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 43:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 44:
			return new int[13]
			{
				36, 34, 21, 44, 47, 48, 50, 51, 52, 53,
				35, 54, 55
			};
		case 45:
			return new int[1] { 35 };
		case 46:
			return new int[2] { 41, 42 };
		case 47:
			return new int[1] { 34 };
		case 48:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 49:
			return new int[23]
			{
				46, 2, 6, 11, 33, 32, 25, 26, 27, 28,
				29, 30, 31, 20, 21, 22, 23, 24, 9, 36,
				7, 10, 45
			};
		case 50:
			return new int[23]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 46, 36,
				7, 10, 45
			};
		case 51:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 52:
			return new int[1] { 36 };
		case 53:
			return new int[1] { 7 };
		case 54:
			return new int[2] { 10, 9 };
		case 55:
			return new int[9] { 33, 2, 6, 11, 9, 36, 7, 10, 45 };
		case 56:
			return new int[10] { 32, 2, 6, 11, 33, 9, 36, 7, 10, 45 };
		case 57:
			return new int[13]
			{
				25, 26, 27, 2, 6, 11, 33, 32, 9, 36,
				7, 10, 45
			};
		case 58:
			return new int[17]
			{
				28, 29, 30, 31, 2, 6, 11, 33, 32, 25,
				26, 27, 9, 36, 7, 10, 45
			};
		case 59:
			return new int[17]
			{
				28, 29, 30, 31, 2, 6, 11, 33, 32, 25,
				26, 27, 9, 36, 7, 10, 45
			};
		case 60:
			return new int[17]
			{
				28, 29, 30, 31, 2, 6, 11, 33, 32, 25,
				26, 27, 9, 36, 7, 10, 45
			};
		case 61:
			return new int[19]
			{
				20, 21, 2, 6, 11, 33, 32, 25, 26, 27,
				28, 29, 30, 31, 9, 36, 7, 10, 45
			};
		case 62:
			return new int[19]
			{
				20, 21, 2, 6, 11, 33, 32, 25, 26, 27,
				28, 29, 30, 31, 9, 36, 7, 10, 45
			};
		case 63:
			return new int[19]
			{
				20, 21, 2, 6, 11, 33, 32, 25, 26, 27,
				28, 29, 30, 31, 9, 36, 7, 10, 45
			};
		case 64:
			return new int[19]
			{
				20, 21, 2, 6, 11, 33, 32, 25, 26, 27,
				28, 29, 30, 31, 9, 36, 7, 10, 45
			};
		case 65:
			return new int[22]
			{
				22, 23, 24, 2, 6, 11, 33, 32, 25, 26,
				27, 28, 29, 30, 31, 20, 21, 9, 36, 7,
				10, 45
			};
		case 66:
			return new int[22]
			{
				22, 23, 24, 2, 6, 11, 33, 32, 25, 26,
				27, 28, 29, 30, 31, 20, 21, 9, 36, 7,
				10, 45
			};
		case 67:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 68:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 69:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 70:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 71:
			return new int[2] { 36, 45 };
		case 72:
			return new int[2] { 36, 45 };
		case 73:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 74:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 75:
			return new int[1] { 41 };
		case 76:
			return new int[25]
			{
				39, 40, 46, 2, 6, 11, 33, 32, 25, 26,
				27, 28, 29, 30, 31, 20, 21, 22, 23, 24,
				9, 36, 7, 10, 45
			};
		case 77:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 78:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 79:
			return new int[7] { 2, 6, 9, 36, 7, 45, 10 };
		case 80:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 81:
			return new int[12]
			{
				34, 21, 44, 47, 48, 50, 51, 52, 53, 35,
				54, 55
			};
		case 82:
			return new int[2] { 36, 45 };
		case 83:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		case 84:
			return new int[4] { 2, 36, 7, 45 };
		case 85:
			return new int[2] { 36, 45 };
		case 86:
			return new int[22]
			{
				2, 6, 11, 33, 32, 25, 26, 27, 28, 29,
				30, 31, 20, 21, 22, 23, 24, 9, 36, 7,
				10, 45
			};
		default:
			ParserException.ThrowsMessage("Unexpected nodeId");
			return null;
		}
	}
}
