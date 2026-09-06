namespace ApiPerleRare.Orderbys;

internal class Scanner
{
	private string _textReader;

	public const char EOS = '\uffff';

	private string _buffer;

	private int _bufferPosition = 0;

	private int _bufferSize = 0;

	private char ch;

	private int _currentCharPosX;

	private int _nextCharPosX;

	private int _currentCharPosY;

	private int _nextCharPosY;

	public int TokenCharPosX;

	public int TokenCharPosY;

	public int TokenType;

	public string TokenText;

	private int _foundTokenStart;

	private int _foundTokenEnd;

	public const int IDENT = 10;

	public const int INTERNAL1 = 6;

	public const int INTERNAL2 = 11;

	public const int ASC = 8;

	public const int DESC = 9;

	public Scanner(string text)
	{
		_textReader = text;
		_buffer = text;
		_bufferSize = text.Length;
		MoveToNextToken();
	}

	private void NextChar()
	{
		if (_bufferPosition >= _bufferSize)
		{
			ch = '\uffff';
			return;
		}
		ch = _buffer[_bufferPosition];
		_bufferPosition++;
		if (ch == '\n')
		{
			_currentCharPosY++;
			_currentCharPosX = 0;
		}
		else if (ch != '\r')
		{
			_currentCharPosX++;
		}
	}

	private void MoveToNextToken()
	{
		do
		{
			_nextCharPosX = _currentCharPosX;
			_nextCharPosY = _currentCharPosY;
			NextChar();
		}
		while (ch == '\t' || ch == '\n' || ch == '\r' || ch == ' ');
	}

	private void CommitFoundToken()
	{
		if (_foundTokenStart >= _foundTokenEnd)
		{
			TokenText = "";
			return;
		}
		TokenText = _buffer.Substring(_foundTokenStart, _foundTokenEnd - _foundTokenStart);
		_bufferPosition = _foundTokenEnd;
		_currentCharPosX = _nextCharPosX;
		_currentCharPosY = _nextCharPosY;
		if (_bufferPosition < _bufferSize)
		{
			ch = _buffer[_bufferPosition];
		}
		MoveToNextToken();
	}

	private void FoundAny()
	{
		TokenType = 1;
		_foundTokenEnd = _bufferPosition;
		_nextCharPosX = _currentCharPosX;
		_nextCharPosY = _currentCharPosY;
	}

	public static string GetTokenTypeDescription(int tokenType)
	{
		return tokenType switch
		{
			10 => "Ident", 
			6 => "\",\"", 
			11 => "\".\"", 
			8 => "\"asc\"", 
			9 => "\"desc\"", 
			_ => "???", 
		};
	}

	public static string GetLiteral(int tokenType)
	{
		return tokenType switch
		{
			10 => "Ident", 
			6 => ",", 
			11 => ".", 
			8 => "asc", 
			9 => "desc", 
			_ => null, 
		};
	}

	public void NextToken1()
	{
		TokenCharPosX = _nextCharPosX;
		TokenCharPosY = _nextCharPosY;
		if (ch == '\uffff')
		{
			TokenType = 2;
			return;
		}
		TokenType = 0;
		_foundTokenStart = (_foundTokenEnd = _bufferPosition - 1);
		int num = 0;
		int num2 = 0;
		if (ch != ',')
		{
			if ((ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
			{
				do
				{
					FoundToken(10);
					NextChar();
				}
				while ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'));
			}
		}
		else
		{
			FoundToken(6);
		}
		CommitFoundToken();
	}

	public void NextToken2()
	{
		TokenCharPosX = _nextCharPosX;
		TokenCharPosY = _nextCharPosY;
		if (ch == '\uffff')
		{
			TokenType = 2;
			return;
		}
		TokenType = 0;
		_foundTokenStart = (_foundTokenEnd = _bufferPosition - 1);
		int num = 0;
		int num2 = 0;
		if (ch == ',')
		{
			FoundToken(6);
		}
		CommitFoundToken();
	}

	public void NextToken3()
	{
		TokenCharPosX = _nextCharPosX;
		TokenCharPosY = _nextCharPosY;
		if (ch == '\uffff')
		{
			TokenType = 2;
			return;
		}
		TokenType = 0;
		_foundTokenStart = (_foundTokenEnd = _bufferPosition - 1);
		int num = 0;
		int num2 = 0;
		if (ch != 'A' && ch != 'a')
		{
			if (ch != 'D' && ch != 'd')
			{
				if (ch != '.')
				{
					if (ch == ',')
					{
						FoundToken(6);
					}
				}
				else
				{
					FoundToken(11);
				}
			}
			else
			{
				NextChar();
				if (ch == 'E' || ch == 'e')
				{
					NextChar();
					if (ch == 'S' || ch == 's')
					{
						NextChar();
						if (ch == 'C' || ch == 'c')
						{
							FoundToken(9);
						}
					}
				}
			}
		}
		else
		{
			NextChar();
			if (ch == 'S' || ch == 's')
			{
				NextChar();
				if (ch == 'C' || ch == 'c')
				{
					FoundToken(8);
				}
			}
		}
		CommitFoundToken();
	}

	public void NextToken4()
	{
		TokenCharPosX = _nextCharPosX;
		TokenCharPosY = _nextCharPosY;
		if (ch == '\uffff')
		{
			TokenType = 2;
			return;
		}
		TokenType = 0;
		_foundTokenStart = (_foundTokenEnd = _bufferPosition - 1);
		int num = 0;
		int num2 = 0;
		if ((ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
		{
			do
			{
				FoundToken(10);
				NextChar();
			}
			while ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'));
		}
		CommitFoundToken();
	}

	private void FoundToken(int tokenType)
	{
		TokenType = tokenType;
		_foundTokenEnd = _bufferPosition;
		_nextCharPosX = _currentCharPosX;
		_nextCharPosY = _currentCharPosY;
	}
}
