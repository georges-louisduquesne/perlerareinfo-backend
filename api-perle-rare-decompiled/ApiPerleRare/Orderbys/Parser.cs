using System.Collections.Generic;

namespace ApiPerleRare.Orderbys;

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

	public List<OrderByDef> Parse()
	{
		Eval0();
		if (_reduction != 0 || _pos != 1 || _values.Count != 2)
		{
			ParserException.ThrowsMessage("Problème de conception!");
		}
		return (List<OrderByDef>)_values[0].Value;
	}

	public static List<OrderByDef> Parse(string text)
	{
		Scanner scanner = new Scanner(text);
		Parser parser = new Parser(scanner);
		return parser.Parse();
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
			if (symbol == 2 || symbol == 6)
			{
				List<OrderByDef> v = Reduce0();
				_values.Insert(_pos, new ParserValue(4, v));
				continue;
			}
			_pos++;
			switch (symbol)
			{
			case 4:
				Eval1();
				break;
			case 5:
				Eval2();
				break;
			case 7:
				Eval3();
				break;
			case 10:
				Eval4();
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
			if (symbol == 6)
			{
				Eval5();
			}
			else
			{
				Fail(1);
			}
		}
		_reduction--;
	}

	private void Eval2()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken2();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6)
			{
				List<OrderByDef> v = Reduce1((OrderByDef)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(4, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(2);
		}
		_reduction--;
	}

	private void Eval3()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken3();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6)
			{
				OrderByDef v = Reduce5((List<string>)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(5, v);
				_reduction = 0;
				return;
			}
			_pos++;
			switch (symbol)
			{
			case 8:
				Eval6();
				break;
			case 9:
				Eval7();
				break;
			case 11:
				Eval8();
				break;
			default:
				Fail(3);
				break;
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
				_scanner.NextToken3();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 8 || symbol == 9 || symbol == 11 || symbol == 6)
			{
				List<string> v = Reduce3((string)_values[_pos - 1].Value);
				_pos--;
				_values[_pos] = new ParserValue(7, v);
				_reduction = 0;
				return;
			}
			_pos++;
			Fail(4);
		}
		_reduction--;
	}

	private void Eval5()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken4();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			switch (symbol)
			{
			case 5:
				Eval9();
				break;
			case 7:
				Eval3();
				break;
			case 10:
				Eval4();
				break;
			default:
				Fail(5);
				break;
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
				_scanner.NextToken2();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6)
			{
				OrderByDef v = Reduce6((List<string>)_values[_pos - 2].Value);
				_values.RemoveRange(_pos - 1, 1);
				_pos -= 2;
				_values[_pos] = new ParserValue(5, v);
				_reduction = 1;
				return;
			}
			_pos++;
			Fail(6);
		}
		_reduction--;
	}

	private void Eval7()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken2();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6)
			{
				OrderByDef v = Reduce7((List<string>)_values[_pos - 2].Value);
				_values.RemoveRange(_pos - 1, 1);
				_pos -= 2;
				_values[_pos] = new ParserValue(5, v);
				_reduction = 1;
				return;
			}
			_pos++;
			Fail(7);
		}
		_reduction--;
	}

	private void Eval8()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken4();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			_pos++;
			if (symbol == 10)
			{
				Eval10();
			}
			else
			{
				Fail(8);
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
				_scanner.NextToken2();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 6)
			{
				List<OrderByDef> v = Reduce2((List<OrderByDef>)_values[_pos - 3].Value, (OrderByDef)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(4, v);
				_reduction = 2;
				return;
			}
			_pos++;
			Fail(9);
		}
		_reduction--;
	}

	private void Eval10()
	{
		while (_reduction == 0)
		{
			if (_pos == _values.Count)
			{
				_scanner.NextToken3();
				_values.Add(new ParserValue(_scanner.TokenType, _scanner.TokenText));
			}
			int symbol = _values[_pos].SymbolId;
			if (symbol == 2 || symbol == 8 || symbol == 9 || symbol == 11 || symbol == 6)
			{
				List<string> v = Reduce4((List<string>)_values[_pos - 3].Value, (string)_values[_pos - 1].Value);
				_values.RemoveRange(_pos - 2, 2);
				_pos -= 3;
				_values[_pos] = new ParserValue(7, v);
				_reduction = 2;
				return;
			}
			_pos++;
			Fail(10);
		}
		_reduction--;
	}

	private List<OrderByDef> Reduce0()
	{
		return new List<OrderByDef>();
	}

	private List<OrderByDef> Reduce1(OrderByDef p0)
	{
		List<OrderByDef> returnedValue = new List<OrderByDef>();
		returnedValue.Add(p0);
		return returnedValue;
	}

	private List<OrderByDef> Reduce2(List<OrderByDef> p0, OrderByDef p1)
	{
		p0.Add(p1);
		return p0;
	}

	private List<string> Reduce3(string p0)
	{
		List<string> list = new List<string>();
		list.Add(p0);
		return list;
	}

	private List<string> Reduce4(List<string> p0, string p1)
	{
		p0.Add(p1);
		return p0;
	}

	private OrderByDef Reduce5(List<string> p0)
	{
		return new OrderByDef(p0, asc: true);
	}

	private OrderByDef Reduce6(List<string> p0)
	{
		return new OrderByDef(p0, asc: true);
	}

	private OrderByDef Reduce7(List<string> p0)
	{
		return new OrderByDef(p0, asc: false);
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
			return new int[3] { 6, 10, 2 };
		case 1:
			return new int[2] { 6, 2 };
		case 2:
			return new int[2] { 2, 6 };
		case 3:
			return new int[5] { 8, 9, 11, 2, 6 };
		case 4:
			return new int[5] { 2, 8, 9, 11, 6 };
		case 5:
			return new int[1] { 10 };
		case 6:
			return new int[2] { 2, 6 };
		case 7:
			return new int[2] { 2, 6 };
		case 8:
			return new int[1] { 10 };
		case 9:
			return new int[2] { 2, 6 };
		case 10:
			return new int[5] { 2, 8, 9, 11, 6 };
		default:
			ParserException.ThrowsMessage("Unexpected nodeId");
			return null;
		}
	}
}
