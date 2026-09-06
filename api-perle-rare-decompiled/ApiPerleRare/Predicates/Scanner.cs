namespace ApiPerleRare.Predicates;

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

	public const int DOUBLE1 = 48;

	public const int INTEGER = 47;

	public const int INTEGER2 = -1;

	public const int STRING1 = 54;

	public const int STRING2 = 55;

	public const int IDENT = 34;

	public const int DATE = 50;

	public const int DATETIME = 51;

	public const int ANDOP = 32;

	public const int OROP = 33;

	public const int EQOP = 25;

	public const int LIKE = 26;

	public const int NEQOP = 27;

	public const int LTOP = 28;

	public const int GTOP = 30;

	public const int LEOP = 29;

	public const int GEOP = 31;

	public const int INTERNAL1 = 6;

	public const int INTERNAL2 = 7;

	public const int INTERNAL3 = 9;

	public const int INTERNAL4 = 10;

	public const int INTERNAL5 = 20;

	public const int INTERNAL6 = 21;

	public const int INTERNAL7 = 22;

	public const int INTERNAL8 = 23;

	public const int INTERNAL9 = 24;

	public const int INTERNAL10 = 35;

	public const int INTERNAL11 = 36;

	public const int INTERNAL12 = 11;

	public const int IN1 = 39;

	public const int IS1 = 40;

	public const int NULL1 = 41;

	public const int NOT = 42;

	public const int TRUE1 = 52;

	public const int FALSE1 = 53;

	public const int INTERNAL13 = 44;

	public const int INTERNAL14 = 45;

	public const int INTERNAL15 = 46;

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
			48 => "Double", 
			47 => "Integer", 
			54 => "String", 
			55 => "String2", 
			34 => "Ident", 
			50 => "Date", 
			51 => "DateTime", 
			32 => "AndOp", 
			33 => "OrOp", 
			25 => "EqOp", 
			26 => "\"Like\"", 
			27 => "NeqOp", 
			28 => "LtOp", 
			30 => "GtOp", 
			29 => "LeOp", 
			31 => "GeOp", 
			6 => "\"?\"", 
			7 => "\":\"", 
			9 => "\"[\"", 
			10 => "\"]\"", 
			20 => "\"+\"", 
			21 => "\"-\"", 
			22 => "\"*\"", 
			23 => "\"/\"", 
			24 => "\"%\"", 
			35 => "\"(\"", 
			36 => "\")\"", 
			11 => "\"??\"", 
			39 => "\"IN\"", 
			40 => "\"IS\"", 
			41 => "\"NULL\"", 
			42 => "\"NOT\"", 
			52 => "\"true\"", 
			53 => "\"false\"", 
			44 => "\"!\"", 
			45 => "\",\"", 
			46 => "\".\"", 
			_ => "???", 
		};
	}

	public static string GetLiteral(int tokenType)
	{
		return tokenType switch
		{
			48 => "Double", 
			47 => "Integer", 
			54 => "String", 
			55 => "String2", 
			34 => "Ident", 
			50 => "Date", 
			51 => "DateTime", 
			32 => "AndOp", 
			33 => "OrOp", 
			25 => "EqOp", 
			26 => "Like", 
			27 => "NeqOp", 
			28 => "LtOp", 
			30 => "GtOp", 
			29 => "LeOp", 
			31 => "GeOp", 
			6 => "?", 
			7 => ":", 
			9 => "[", 
			10 => "]", 
			20 => "+", 
			21 => "-", 
			22 => "*", 
			23 => "/", 
			24 => "%", 
			35 => "(", 
			36 => ")", 
			11 => "??", 
			39 => "IN", 
			40 => "IS", 
			41 => "NULL", 
			42 => "NOT", 
			52 => "true", 
			53 => "false", 
			44 => "!", 
			45 => ",", 
			46 => ".", 
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
		if (ch != '-')
		{
			if (ch != '!')
			{
				if (ch != 'T' && ch != 't')
				{
					if (ch != 'F' && ch != 'f')
					{
						if (ch != '(')
						{
							if (ch < '0' || ch > '9')
							{
								if (ch != '\'' && ch != '`' && ch != '‘')
								{
									if ((ch >= 'A' && ch <= 'Z') || (ch >= '_' && ch <= 'z'))
									{
										goto IL_0308;
									}
									if (ch == '"' || ch == '«' || ch == '»' || ch == '“' || ch == '”')
									{
										do
										{
											NextChar();
											if (ch == '"' || ch == '«' || ch == '»' || ch == '“' || ch == '”')
											{
												FoundToken(55);
												break;
											}
										}
										while (ch != '\uffff');
									}
								}
								else
								{
									do
									{
										NextChar();
										if (ch == '\'' || ch == '`' || ch == '‘')
										{
											FoundToken(54);
											break;
										}
									}
									while (ch != '\uffff');
								}
							}
							else
							{
								FoundToken(47);
								NextChar();
								if (ch < '0' || ch > '9')
								{
									if (ch == '.')
									{
										goto IL_05f4;
									}
								}
								else
								{
									FoundToken(47);
									NextChar();
									if (ch < '0' || ch > '9')
									{
										if (ch == '.')
										{
											goto IL_05f4;
										}
									}
									else
									{
										FoundToken(47);
										NextChar();
										if (ch < '0' || ch > '9')
										{
											if (ch == '.')
											{
												goto IL_05f4;
											}
										}
										else
										{
											FoundToken(47);
											NextChar();
											if (ch != '-')
											{
												if (ch < '0' || ch > '9')
												{
													if (ch == '.')
													{
														goto IL_05f4;
													}
												}
												else
												{
													do
													{
														FoundToken(47);
														NextChar();
													}
													while (ch >= '0' && ch <= '9');
													if (ch == '.')
													{
														goto IL_05f4;
													}
												}
											}
											else
											{
												NextChar();
												if (ch >= '0' && ch <= '9')
												{
													NextChar();
													if (ch >= '0' && ch <= '9')
													{
														NextChar();
														if (ch == '-')
														{
															NextChar();
															if (ch >= '0' && ch <= '9')
															{
																NextChar();
																if (ch >= '0' && ch <= '9')
																{
																	FoundToken(50);
																	NextChar();
																	if (ch == 'T' || ch == 't')
																	{
																		NextChar();
																		if (ch >= '0' && ch <= '9')
																		{
																			NextChar();
																			if (ch >= '0' && ch <= '9')
																			{
																				NextChar();
																				if (ch == ':')
																				{
																					NextChar();
																					if (ch >= '0' && ch <= '9')
																					{
																						NextChar();
																						if (ch >= '0' && ch <= '9')
																						{
																							NextChar();
																							if (ch == ':')
																							{
																								NextChar();
																								if (ch >= '0' && ch <= '9')
																								{
																									NextChar();
																									if (ch >= '0' && ch <= '9')
																									{
																										FoundToken(51);
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
						else
						{
							FoundToken(35);
						}
					}
					else
					{
						FoundToken(34);
						NextChar();
						if (ch != 'A' && ch != 'a')
						{
							if ((ch >= '0' && ch <= '9') || (ch >= 'B' && ch <= 'Z') || ch == '_' || (ch >= 'b' && ch <= 'z'))
							{
								goto IL_0308;
							}
						}
						else
						{
							FoundToken(34);
							NextChar();
							if (ch != 'L' && ch != 'l')
							{
								if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
								{
									goto IL_0308;
								}
							}
							else
							{
								FoundToken(34);
								NextChar();
								if (ch != 'S' && ch != 's')
								{
									if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
									{
										goto IL_0308;
									}
								}
								else
								{
									FoundToken(34);
									NextChar();
									if (ch != 'E' && ch != 'e')
									{
										if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
										{
											goto IL_0308;
										}
									}
									else
									{
										FoundToken(53);
										NextChar();
										if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
										{
											goto IL_0308;
										}
									}
								}
							}
						}
					}
				}
				else
				{
					FoundToken(34);
					NextChar();
					if (ch != 'R' && ch != 'r')
					{
						if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
						{
							goto IL_0308;
						}
					}
					else
					{
						FoundToken(34);
						NextChar();
						if (ch != 'U' && ch != 'u')
						{
							if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
							{
								goto IL_0308;
							}
						}
						else
						{
							FoundToken(34);
							NextChar();
							if (ch != 'E' && ch != 'e')
							{
								if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
								{
									goto IL_0308;
								}
							}
							else
							{
								FoundToken(52);
								NextChar();
								if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
								{
									goto IL_0308;
								}
							}
						}
					}
				}
			}
			else
			{
				FoundToken(44);
			}
		}
		else
		{
			FoundToken(21);
		}
		goto IL_0c61;
		IL_0c61:
		CommitFoundToken();
		return;
		IL_0308:
		do
		{
			FoundToken(34);
			NextChar();
		}
		while ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'));
		goto IL_0c61;
		IL_05f4:
		NextChar();
		if (ch >= '0' && ch <= '9')
		{
			do
			{
				FoundToken(48);
				NextChar();
			}
			while (ch >= '0' && ch <= '9');
		}
		goto IL_0c61;
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
		if (ch != '?')
		{
			if (ch != '[')
			{
				if (ch != ')')
				{
					if (ch != ':')
					{
						if (ch == ',')
						{
							FoundToken(45);
						}
					}
					else
					{
						FoundToken(7);
					}
				}
				else
				{
					FoundToken(36);
				}
			}
			else
			{
				FoundToken(9);
			}
		}
		else
		{
			FoundToken(6);
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
		if (ch != '?')
		{
			if (ch != '[')
			{
				if (ch != ')')
				{
					if (ch != ':')
					{
						if (ch != ']')
						{
							if (ch == ',')
							{
								FoundToken(45);
							}
						}
						else
						{
							FoundToken(10);
						}
					}
					else
					{
						FoundToken(7);
					}
				}
				else
				{
					FoundToken(36);
				}
			}
			else
			{
				FoundToken(9);
			}
		}
		else
		{
			FoundToken(6);
			NextChar();
			if (ch == '?')
			{
				FoundToken(11);
			}
		}
		CommitFoundToken();
	}

	public void NextToken5()
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
		if (ch != '?')
		{
			if (ch != '[')
			{
				if (ch != ')')
				{
					if (ch != ':')
					{
						if (ch != ']')
						{
							if (ch != ',')
							{
								if (ch != 'O' && ch != 'o')
								{
									if (ch == '|')
									{
										NextChar();
										if (ch == '|')
										{
											goto IL_01c2;
										}
									}
								}
								else
								{
									NextChar();
									if (ch == 'R' || ch == 'r')
									{
										goto IL_01c2;
									}
								}
							}
							else
							{
								FoundToken(45);
							}
						}
						else
						{
							FoundToken(10);
						}
					}
					else
					{
						FoundToken(7);
					}
				}
				else
				{
					FoundToken(36);
				}
			}
			else
			{
				FoundToken(9);
			}
		}
		else
		{
			FoundToken(6);
			NextChar();
			if (ch == '?')
			{
				FoundToken(11);
			}
		}
		goto IL_01cd;
		IL_01c2:
		FoundToken(33);
		goto IL_01cd;
		IL_01cd:
		CommitFoundToken();
	}

	public void NextToken6()
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
		if (ch != '?')
		{
			if (ch != '[')
			{
				if (ch != ')')
				{
					if (ch != ':')
					{
						if (ch != ']')
						{
							if (ch != ',')
							{
								if (ch != 'A' && ch != 'a')
								{
									if (ch != '&')
									{
										if (ch != 'O' && ch != 'o')
										{
											if (ch == '|')
											{
												NextChar();
												if (ch == '|')
												{
													goto IL_0284;
												}
											}
										}
										else
										{
											NextChar();
											if (ch == 'R' || ch == 'r')
											{
												goto IL_0284;
											}
										}
									}
									else
									{
										NextChar();
										if (ch == '&')
										{
											goto IL_0279;
										}
									}
								}
								else
								{
									NextChar();
									if (ch == 'N' || ch == 'n')
									{
										NextChar();
										if (ch == 'D' || ch == 'd')
										{
											goto IL_0279;
										}
									}
								}
							}
							else
							{
								FoundToken(45);
							}
						}
						else
						{
							FoundToken(10);
						}
					}
					else
					{
						FoundToken(7);
					}
				}
				else
				{
					FoundToken(36);
				}
			}
			else
			{
				FoundToken(9);
			}
		}
		else
		{
			FoundToken(6);
			NextChar();
			if (ch == '?')
			{
				FoundToken(11);
			}
		}
		goto IL_028f;
		IL_0284:
		FoundToken(33);
		goto IL_028f;
		IL_028f:
		CommitFoundToken();
		return;
		IL_0279:
		FoundToken(32);
		goto IL_028f;
	}

	public void NextToken7()
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
		if (ch != 'L' && ch != 'l')
		{
			if (ch != '?')
			{
				if (ch != '[')
				{
					if (ch != ')')
					{
						if (ch != ':')
						{
							if (ch != ']')
							{
								if (ch != ',')
								{
									if (ch != '=')
									{
										if (ch != 'E' && ch != 'e')
										{
											if (ch != '!')
											{
												if (ch != '<')
												{
													if (ch != 'N' && ch != 'n')
													{
														if (ch != 'O' && ch != 'o')
														{
															if (ch != '|')
															{
																if (ch != 'A' && ch != 'a')
																{
																	if (ch == '&')
																	{
																		NextChar();
																		if (ch == '&')
																		{
																			goto IL_04a6;
																		}
																	}
																}
																else
																{
																	NextChar();
																	if (ch == 'N' || ch == 'n')
																	{
																		NextChar();
																		if (ch == 'D' || ch == 'd')
																		{
																			goto IL_04a6;
																		}
																	}
																}
															}
															else
															{
																NextChar();
																if (ch == '|')
																{
																	goto IL_0473;
																}
															}
														}
														else
														{
															NextChar();
															if (ch == 'R' || ch == 'r')
															{
																goto IL_0473;
															}
														}
													}
													else
													{
														NextChar();
														if (ch == 'E' || ch == 'e')
														{
															NextChar();
															if (ch == 'Q' || ch == 'q')
															{
																goto IL_043d;
															}
														}
													}
												}
												else
												{
													NextChar();
													if (ch == '>')
													{
														goto IL_043d;
													}
												}
											}
											else
											{
												NextChar();
												if (ch == '=')
												{
													goto IL_043d;
												}
											}
										}
										else
										{
											NextChar();
											if (ch == 'Q' || ch == 'q')
											{
												goto IL_042f;
											}
										}
									}
									else
									{
										FoundToken(25);
										NextChar();
										if (ch == '=')
										{
											goto IL_042f;
										}
									}
								}
								else
								{
									FoundToken(45);
								}
							}
							else
							{
								FoundToken(10);
							}
						}
						else
						{
							FoundToken(7);
						}
					}
					else
					{
						FoundToken(36);
					}
				}
				else
				{
					FoundToken(9);
				}
			}
			else
			{
				FoundToken(6);
				NextChar();
				if (ch == '?')
				{
					FoundToken(11);
				}
			}
		}
		else
		{
			NextChar();
			if (ch == 'I' || ch == 'i')
			{
				NextChar();
				if (ch == 'K' || ch == 'k')
				{
					NextChar();
					if (ch == 'E' || ch == 'e')
					{
						FoundToken(26);
					}
				}
			}
		}
		goto IL_04e4;
		IL_04e4:
		CommitFoundToken();
		return;
		IL_0473:
		FoundToken(33);
		goto IL_04e4;
		IL_043d:
		FoundToken(27);
		goto IL_04e4;
		IL_042f:
		FoundToken(25);
		goto IL_04e4;
		IL_04a6:
		FoundToken(32);
		goto IL_04e4;
	}

	public void NextToken8()
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
		if (ch != '?')
		{
			if (ch != 'L' && ch != 'l')
			{
				if (ch != '[')
				{
					if (ch != ')')
					{
						if (ch != ':')
						{
							if (ch != ']')
							{
								if (ch != ',')
								{
									if (ch != '<')
									{
										if (ch != 'G' && ch != 'g')
										{
											if (ch != '>')
											{
												if (ch != 'O' && ch != 'o')
												{
													if (ch != '|')
													{
														if (ch != 'A' && ch != 'a')
														{
															if (ch != '&')
															{
																if (ch != '=')
																{
																	if (ch != 'E' && ch != 'e')
																	{
																		if (ch != '!')
																		{
																			if (ch == 'N' || ch == 'n')
																			{
																				NextChar();
																				if (ch == 'E' || ch == 'e')
																				{
																					NextChar();
																					if (ch == 'Q' || ch == 'q')
																					{
																						goto IL_055e;
																					}
																				}
																			}
																		}
																		else
																		{
																			NextChar();
																			if (ch == '=')
																			{
																				goto IL_055e;
																			}
																		}
																	}
																	else
																	{
																		NextChar();
																		if (ch == 'Q' || ch == 'q')
																		{
																			goto IL_05c9;
																		}
																	}
																}
																else
																{
																	FoundToken(25);
																	NextChar();
																	if (ch == '=')
																	{
																		goto IL_05c9;
																	}
																}
															}
															else
															{
																NextChar();
																if (ch == '&')
																{
																	goto IL_05be;
																}
															}
														}
														else
														{
															NextChar();
															if (ch == 'N' || ch == 'n')
															{
																NextChar();
																if (ch == 'D' || ch == 'd')
																{
																	goto IL_05be;
																}
															}
														}
													}
													else
													{
														NextChar();
														if (ch == '|')
														{
															goto IL_0588;
														}
													}
												}
												else
												{
													NextChar();
													if (ch == 'R' || ch == 'r')
													{
														goto IL_0588;
													}
												}
											}
											else
											{
												FoundToken(30);
												NextChar();
												if (ch == '=')
												{
													goto IL_057a;
												}
											}
										}
										else
										{
											NextChar();
											if (ch != 'T' && ch != 't')
											{
												if (ch == 'E' || ch == 'e')
												{
													goto IL_057a;
												}
											}
											else
											{
												FoundToken(30);
											}
										}
									}
									else
									{
										FoundToken(28);
										NextChar();
										if (ch == '=')
										{
											goto IL_0550;
										}
										if (ch == '>')
										{
											goto IL_055e;
										}
									}
								}
								else
								{
									FoundToken(45);
								}
							}
							else
							{
								FoundToken(10);
							}
						}
						else
						{
							FoundToken(7);
						}
					}
					else
					{
						FoundToken(36);
					}
				}
				else
				{
					FoundToken(9);
				}
			}
			else
			{
				NextChar();
				if (ch != 'I' && ch != 'i')
				{
					if (ch != 'T' && ch != 't')
					{
						if (ch == 'E' || ch == 'e')
						{
							goto IL_0550;
						}
					}
					else
					{
						FoundToken(28);
					}
				}
				else
				{
					NextChar();
					if (ch == 'K' || ch == 'k')
					{
						NextChar();
						if (ch == 'E' || ch == 'e')
						{
							FoundToken(26);
						}
					}
				}
			}
		}
		else
		{
			FoundToken(6);
			NextChar();
			if (ch == '?')
			{
				FoundToken(11);
			}
		}
		goto IL_0632;
		IL_0632:
		CommitFoundToken();
		return;
		IL_055e:
		FoundToken(27);
		goto IL_0632;
		IL_05be:
		FoundToken(32);
		goto IL_0632;
		IL_05c9:
		FoundToken(25);
		goto IL_0632;
		IL_057a:
		FoundToken(31);
		goto IL_0632;
		IL_0588:
		FoundToken(33);
		goto IL_0632;
		IL_0550:
		FoundToken(29);
		goto IL_0632;
	}

	public void NextToken9()
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
		if (ch != '+')
		{
			if (ch != '-')
			{
				if (ch != '?')
				{
					if (ch != 'L' && ch != 'l')
					{
						if (ch != '[')
						{
							if (ch != ')')
							{
								if (ch != ':')
								{
									if (ch != ']')
									{
										if (ch != ',')
										{
											if (ch != 'O' && ch != 'o')
											{
												if (ch != '|')
												{
													if (ch != 'A' && ch != 'a')
													{
														if (ch != '&')
														{
															if (ch != '=')
															{
																if (ch != 'E' && ch != 'e')
																{
																	if (ch != '!')
																	{
																		if (ch != '<')
																		{
																			if (ch != 'N' && ch != 'n')
																			{
																				if (ch != 'G' && ch != 'g')
																				{
																					if (ch == '>')
																					{
																						FoundToken(30);
																						NextChar();
																						if (ch == '=')
																						{
																							goto IL_0634;
																						}
																					}
																				}
																				else
																				{
																					NextChar();
																					if (ch != 'T' && ch != 't')
																					{
																						if (ch == 'E' || ch == 'e')
																						{
																							goto IL_0634;
																						}
																					}
																					else
																					{
																						FoundToken(30);
																					}
																				}
																			}
																			else
																			{
																				NextChar();
																				if (ch == 'E' || ch == 'e')
																				{
																					NextChar();
																					if (ch == 'Q' || ch == 'q')
																					{
																						goto IL_05f6;
																					}
																				}
																			}
																		}
																		else
																		{
																			FoundToken(28);
																			NextChar();
																			if (ch == '>')
																			{
																				goto IL_05f6;
																			}
																			if (ch == '=')
																			{
																				goto IL_0596;
																			}
																		}
																	}
																	else
																	{
																		NextChar();
																		if (ch == '=')
																		{
																			goto IL_05f6;
																		}
																	}
																}
																else
																{
																	NextChar();
																	if (ch == 'Q' || ch == 'q')
																	{
																		goto IL_05eb;
																	}
																}
															}
															else
															{
																FoundToken(25);
																NextChar();
																if (ch == '=')
																{
																	goto IL_05eb;
																}
															}
														}
														else
														{
															NextChar();
															if (ch == '&')
															{
																goto IL_05dd;
															}
														}
													}
													else
													{
														NextChar();
														if (ch == 'N' || ch == 'n')
														{
															NextChar();
															if (ch == 'D' || ch == 'd')
															{
																goto IL_05dd;
															}
														}
													}
												}
												else
												{
													NextChar();
													if (ch == '|')
													{
														goto IL_05a4;
													}
												}
											}
											else
											{
												NextChar();
												if (ch == 'R' || ch == 'r')
												{
													goto IL_05a4;
												}
											}
										}
										else
										{
											FoundToken(45);
										}
									}
									else
									{
										FoundToken(10);
									}
								}
								else
								{
									FoundToken(7);
								}
							}
							else
							{
								FoundToken(36);
							}
						}
						else
						{
							FoundToken(9);
						}
					}
					else
					{
						NextChar();
						if (ch != 'I' && ch != 'i')
						{
							if (ch != 'T' && ch != 't')
							{
								if (ch == 'E' || ch == 'e')
								{
									goto IL_0596;
								}
							}
							else
							{
								FoundToken(28);
							}
						}
						else
						{
							NextChar();
							if (ch == 'K' || ch == 'k')
							{
								NextChar();
								if (ch == 'E' || ch == 'e')
								{
									FoundToken(26);
								}
							}
						}
					}
				}
				else
				{
					FoundToken(6);
					NextChar();
					if (ch == '?')
					{
						FoundToken(11);
					}
				}
			}
			else
			{
				FoundToken(21);
			}
		}
		else
		{
			FoundToken(20);
		}
		goto IL_0672;
		IL_05dd:
		FoundToken(32);
		goto IL_0672;
		IL_05a4:
		FoundToken(33);
		goto IL_0672;
		IL_0634:
		FoundToken(31);
		goto IL_0672;
		IL_0596:
		FoundToken(29);
		goto IL_0672;
		IL_05f6:
		FoundToken(27);
		goto IL_0672;
		IL_0672:
		CommitFoundToken();
		return;
		IL_05eb:
		FoundToken(25);
		goto IL_0672;
	}

	public void NextToken10()
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
		if (ch != '*')
		{
			if (ch != '/')
			{
				if (ch != '%')
				{
					if (ch != '?')
					{
						if (ch != 'L' && ch != 'l')
						{
							if (ch != '+')
							{
								if (ch != '-')
								{
									if (ch != '[')
									{
										if (ch != ')')
										{
											if (ch != ':')
											{
												if (ch != ']')
												{
													if (ch != ',')
													{
														if (ch != 'O' && ch != 'o')
														{
															if (ch != '|')
															{
																if (ch != 'A' && ch != 'a')
																{
																	if (ch != '&')
																	{
																		if (ch != '=')
																		{
																			if (ch != 'E' && ch != 'e')
																			{
																				if (ch != '!')
																				{
																					if (ch != '<')
																					{
																						if (ch != 'N' && ch != 'n')
																						{
																							if (ch != 'G' && ch != 'g')
																							{
																								if (ch == '>')
																								{
																									FoundToken(30);
																									NextChar();
																									if (ch == '=')
																									{
																										goto IL_069d;
																									}
																								}
																							}
																							else
																							{
																								NextChar();
																								if (ch != 'T' && ch != 't')
																								{
																									if (ch == 'E' || ch == 'e')
																									{
																										goto IL_069d;
																									}
																								}
																								else
																								{
																									FoundToken(30);
																								}
																							}
																						}
																						else
																						{
																							NextChar();
																							if (ch == 'E' || ch == 'e')
																							{
																								NextChar();
																								if (ch == 'Q' || ch == 'q')
																								{
																									goto IL_065f;
																								}
																							}
																						}
																					}
																					else
																					{
																						FoundToken(28);
																						NextChar();
																						if (ch == '>')
																						{
																							goto IL_065f;
																						}
																						if (ch == '=')
																						{
																							goto IL_05ff;
																						}
																					}
																				}
																				else
																				{
																					NextChar();
																					if (ch == '=')
																					{
																						goto IL_065f;
																					}
																				}
																			}
																			else
																			{
																				NextChar();
																				if (ch == 'Q' || ch == 'q')
																				{
																					goto IL_0654;
																				}
																			}
																		}
																		else
																		{
																			FoundToken(25);
																			NextChar();
																			if (ch == '=')
																			{
																				goto IL_0654;
																			}
																		}
																	}
																	else
																	{
																		NextChar();
																		if (ch == '&')
																		{
																			goto IL_0646;
																		}
																	}
																}
																else
																{
																	NextChar();
																	if (ch == 'N' || ch == 'n')
																	{
																		NextChar();
																		if (ch == 'D' || ch == 'd')
																		{
																			goto IL_0646;
																		}
																	}
																}
															}
															else
															{
																NextChar();
																if (ch == '|')
																{
																	goto IL_060d;
																}
															}
														}
														else
														{
															NextChar();
															if (ch == 'R' || ch == 'r')
															{
																goto IL_060d;
															}
														}
													}
													else
													{
														FoundToken(45);
													}
												}
												else
												{
													FoundToken(10);
												}
											}
											else
											{
												FoundToken(7);
											}
										}
										else
										{
											FoundToken(36);
										}
									}
									else
									{
										FoundToken(9);
									}
								}
								else
								{
									FoundToken(21);
								}
							}
							else
							{
								FoundToken(20);
							}
						}
						else
						{
							NextChar();
							if (ch != 'I' && ch != 'i')
							{
								if (ch != 'T' && ch != 't')
								{
									if (ch == 'E' || ch == 'e')
									{
										goto IL_05ff;
									}
								}
								else
								{
									FoundToken(28);
								}
							}
							else
							{
								NextChar();
								if (ch == 'K' || ch == 'k')
								{
									NextChar();
									if (ch == 'E' || ch == 'e')
									{
										FoundToken(26);
									}
								}
							}
						}
					}
					else
					{
						FoundToken(6);
						NextChar();
						if (ch == '?')
						{
							FoundToken(11);
						}
					}
				}
				else
				{
					FoundToken(24);
				}
			}
			else
			{
				FoundToken(23);
			}
		}
		else
		{
			FoundToken(22);
		}
		goto IL_06db;
		IL_0646:
		FoundToken(32);
		goto IL_06db;
		IL_069d:
		FoundToken(31);
		goto IL_06db;
		IL_065f:
		FoundToken(27);
		goto IL_06db;
		IL_060d:
		FoundToken(33);
		goto IL_06db;
		IL_06db:
		CommitFoundToken();
		return;
		IL_0654:
		FoundToken(25);
		goto IL_06db;
		IL_05ff:
		FoundToken(29);
		goto IL_06db;
	}

	public void NextToken11()
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
		if (ch != '(')
		{
			if (ch != 'I' && ch != 'i')
			{
				if (ch != '.')
				{
					if (ch != '?')
					{
						if (ch != 'L' && ch != 'l')
						{
							if (ch != '+')
							{
								if (ch != '-')
								{
									if (ch != '*')
									{
										if (ch != '/')
										{
											if (ch != '%')
											{
												if (ch != '[')
												{
													if (ch != ')')
													{
														if (ch != ':')
														{
															if (ch != ']')
															{
																if (ch != ',')
																{
																	if (ch != 'O' && ch != 'o')
																	{
																		if (ch != '|')
																		{
																			if (ch != 'A' && ch != 'a')
																			{
																				if (ch != '&')
																				{
																					if (ch != '=')
																					{
																						if (ch != 'E' && ch != 'e')
																						{
																							if (ch != '!')
																							{
																								if (ch != '<')
																								{
																									if (ch != 'N' && ch != 'n')
																									{
																										if (ch != 'G' && ch != 'g')
																										{
																											if (ch == '>')
																											{
																												FoundToken(30);
																												NextChar();
																												if (ch == '=')
																												{
																													goto IL_0771;
																												}
																											}
																										}
																										else
																										{
																											NextChar();
																											if (ch != 'T' && ch != 't')
																											{
																												if (ch == 'E' || ch == 'e')
																												{
																													goto IL_0771;
																												}
																											}
																											else
																											{
																												FoundToken(30);
																											}
																										}
																									}
																									else
																									{
																										NextChar();
																										if (ch == 'E' || ch == 'e')
																										{
																											NextChar();
																											if (ch == 'Q' || ch == 'q')
																											{
																												goto IL_0733;
																											}
																										}
																									}
																								}
																								else
																								{
																									FoundToken(28);
																									NextChar();
																									if (ch == '>')
																									{
																										goto IL_0733;
																									}
																									if (ch == '=')
																									{
																										goto IL_06d3;
																									}
																								}
																							}
																							else
																							{
																								NextChar();
																								if (ch == '=')
																								{
																									goto IL_0733;
																								}
																							}
																						}
																						else
																						{
																							NextChar();
																							if (ch == 'Q' || ch == 'q')
																							{
																								goto IL_0728;
																							}
																						}
																					}
																					else
																					{
																						FoundToken(25);
																						NextChar();
																						if (ch == '=')
																						{
																							goto IL_0728;
																						}
																					}
																				}
																				else
																				{
																					NextChar();
																					if (ch == '&')
																					{
																						goto IL_071a;
																					}
																				}
																			}
																			else
																			{
																				NextChar();
																				if (ch == 'N' || ch == 'n')
																				{
																					NextChar();
																					if (ch == 'D' || ch == 'd')
																					{
																						goto IL_071a;
																					}
																				}
																			}
																		}
																		else
																		{
																			NextChar();
																			if (ch == '|')
																			{
																				goto IL_06e1;
																			}
																		}
																	}
																	else
																	{
																		NextChar();
																		if (ch == 'R' || ch == 'r')
																		{
																			goto IL_06e1;
																		}
																	}
																}
																else
																{
																	FoundToken(45);
																}
															}
															else
															{
																FoundToken(10);
															}
														}
														else
														{
															FoundToken(7);
														}
													}
													else
													{
														FoundToken(36);
													}
												}
												else
												{
													FoundToken(9);
												}
											}
											else
											{
												FoundToken(24);
											}
										}
										else
										{
											FoundToken(23);
										}
									}
									else
									{
										FoundToken(22);
									}
								}
								else
								{
									FoundToken(21);
								}
							}
							else
							{
								FoundToken(20);
							}
						}
						else
						{
							NextChar();
							if (ch != 'I' && ch != 'i')
							{
								if (ch != 'T' && ch != 't')
								{
									if (ch == 'E' || ch == 'e')
									{
										goto IL_06d3;
									}
								}
								else
								{
									FoundToken(28);
								}
							}
							else
							{
								NextChar();
								if (ch == 'K' || ch == 'k')
								{
									NextChar();
									if (ch == 'E' || ch == 'e')
									{
										FoundToken(26);
									}
								}
							}
						}
					}
					else
					{
						FoundToken(6);
						NextChar();
						if (ch == '?')
						{
							FoundToken(11);
						}
					}
				}
				else
				{
					FoundToken(46);
				}
			}
			else
			{
				NextChar();
				if (ch != 'N' && ch != 'n')
				{
					if (ch == 'S' || ch == 's')
					{
						FoundToken(40);
					}
				}
				else
				{
					FoundToken(39);
				}
			}
		}
		else
		{
			FoundToken(35);
		}
		goto IL_07af;
		IL_0728:
		FoundToken(25);
		goto IL_07af;
		IL_06e1:
		FoundToken(33);
		goto IL_07af;
		IL_0733:
		FoundToken(27);
		goto IL_07af;
		IL_07af:
		CommitFoundToken();
		return;
		IL_0771:
		FoundToken(31);
		goto IL_07af;
		IL_06d3:
		FoundToken(29);
		goto IL_07af;
		IL_071a:
		FoundToken(32);
		goto IL_07af;
	}

	public void NextToken12()
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
		if (ch != 'I' && ch != 'i')
		{
			if (ch != '.')
			{
				if (ch != '?')
				{
					if (ch != 'L' && ch != 'l')
					{
						if (ch != '+')
						{
							if (ch != '-')
							{
								if (ch != '*')
								{
									if (ch != '/')
									{
										if (ch != '%')
										{
											if (ch != '[')
											{
												if (ch != ')')
												{
													if (ch != ':')
													{
														if (ch != ']')
														{
															if (ch != ',')
															{
																if (ch != 'O' && ch != 'o')
																{
																	if (ch != '|')
																	{
																		if (ch != 'A' && ch != 'a')
																		{
																			if (ch != '&')
																			{
																				if (ch != '=')
																				{
																					if (ch != 'E' && ch != 'e')
																					{
																						if (ch != '!')
																						{
																							if (ch != '<')
																							{
																								if (ch != 'N' && ch != 'n')
																								{
																									if (ch != 'G' && ch != 'g')
																									{
																										if (ch == '>')
																										{
																											FoundToken(30);
																											NextChar();
																											if (ch == '=')
																											{
																												goto IL_074e;
																											}
																										}
																									}
																									else
																									{
																										NextChar();
																										if (ch != 'T' && ch != 't')
																										{
																											if (ch == 'E' || ch == 'e')
																											{
																												goto IL_074e;
																											}
																										}
																										else
																										{
																											FoundToken(30);
																										}
																									}
																								}
																								else
																								{
																									NextChar();
																									if (ch == 'E' || ch == 'e')
																									{
																										NextChar();
																										if (ch == 'Q' || ch == 'q')
																										{
																											goto IL_0710;
																										}
																									}
																								}
																							}
																							else
																							{
																								FoundToken(28);
																								NextChar();
																								if (ch == '>')
																								{
																									goto IL_0710;
																								}
																								if (ch == '=')
																								{
																									goto IL_06b0;
																								}
																							}
																						}
																						else
																						{
																							NextChar();
																							if (ch == '=')
																							{
																								goto IL_0710;
																							}
																						}
																					}
																					else
																					{
																						NextChar();
																						if (ch == 'Q' || ch == 'q')
																						{
																							goto IL_0705;
																						}
																					}
																				}
																				else
																				{
																					FoundToken(25);
																					NextChar();
																					if (ch == '=')
																					{
																						goto IL_0705;
																					}
																				}
																			}
																			else
																			{
																				NextChar();
																				if (ch == '&')
																				{
																					goto IL_06f7;
																				}
																			}
																		}
																		else
																		{
																			NextChar();
																			if (ch == 'N' || ch == 'n')
																			{
																				NextChar();
																				if (ch == 'D' || ch == 'd')
																				{
																					goto IL_06f7;
																				}
																			}
																		}
																	}
																	else
																	{
																		NextChar();
																		if (ch == '|')
																		{
																			goto IL_06be;
																		}
																	}
																}
																else
																{
																	NextChar();
																	if (ch == 'R' || ch == 'r')
																	{
																		goto IL_06be;
																	}
																}
															}
															else
															{
																FoundToken(45);
															}
														}
														else
														{
															FoundToken(10);
														}
													}
													else
													{
														FoundToken(7);
													}
												}
												else
												{
													FoundToken(36);
												}
											}
											else
											{
												FoundToken(9);
											}
										}
										else
										{
											FoundToken(24);
										}
									}
									else
									{
										FoundToken(23);
									}
								}
								else
								{
									FoundToken(22);
								}
							}
							else
							{
								FoundToken(21);
							}
						}
						else
						{
							FoundToken(20);
						}
					}
					else
					{
						NextChar();
						if (ch != 'I' && ch != 'i')
						{
							if (ch != 'T' && ch != 't')
							{
								if (ch == 'E' || ch == 'e')
								{
									goto IL_06b0;
								}
							}
							else
							{
								FoundToken(28);
							}
						}
						else
						{
							NextChar();
							if (ch == 'K' || ch == 'k')
							{
								NextChar();
								if (ch == 'E' || ch == 'e')
								{
									FoundToken(26);
								}
							}
						}
					}
				}
				else
				{
					FoundToken(6);
					NextChar();
					if (ch == '?')
					{
						FoundToken(11);
					}
				}
			}
			else
			{
				FoundToken(46);
			}
		}
		else
		{
			NextChar();
			if (ch != 'N' && ch != 'n')
			{
				if (ch == 'S' || ch == 's')
				{
					FoundToken(40);
				}
			}
			else
			{
				FoundToken(39);
			}
		}
		goto IL_078c;
		IL_078c:
		CommitFoundToken();
		return;
		IL_0705:
		FoundToken(25);
		goto IL_078c;
		IL_0710:
		FoundToken(27);
		goto IL_078c;
		IL_06be:
		FoundToken(33);
		goto IL_078c;
		IL_074e:
		FoundToken(31);
		goto IL_078c;
		IL_06f7:
		FoundToken(32);
		goto IL_078c;
		IL_06b0:
		FoundToken(29);
		goto IL_078c;
	}

	public void NextToken13()
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
		if (ch != 'T' && ch != 't')
		{
			if (ch != 'F' && ch != 'f')
			{
				if (ch != '(')
				{
					if (ch < '0' || ch > '9')
					{
						if (ch != '\'' && ch != '`' && ch != '‘')
						{
							if (ch != '"' && ch != '«' && ch != '»' && ch != '“' && ch != '”')
							{
								if ((ch >= 'A' && ch <= 'Z') || (ch >= '_' && ch <= 'z'))
								{
									goto IL_03d7;
								}
							}
							else
							{
								do
								{
									NextChar();
									if (ch == '"' || ch == '«' || ch == '»' || ch == '“' || ch == '”')
									{
										FoundToken(55);
										break;
									}
								}
								while (ch != '\uffff');
							}
						}
						else
						{
							do
							{
								NextChar();
								if (ch == '\'' || ch == '`' || ch == '‘')
								{
									FoundToken(54);
									break;
								}
							}
							while (ch != '\uffff');
						}
					}
					else
					{
						FoundToken(47);
						NextChar();
						if (ch < '0' || ch > '9')
						{
							if (ch == '.')
							{
								goto IL_05b1;
							}
						}
						else
						{
							FoundToken(47);
							NextChar();
							if (ch < '0' || ch > '9')
							{
								if (ch == '.')
								{
									goto IL_05b1;
								}
							}
							else
							{
								FoundToken(47);
								NextChar();
								if (ch < '0' || ch > '9')
								{
									if (ch == '.')
									{
										goto IL_05b1;
									}
								}
								else
								{
									FoundToken(47);
									NextChar();
									if (ch != '-')
									{
										if (ch < '0' || ch > '9')
										{
											if (ch == '.')
											{
												goto IL_05b1;
											}
										}
										else
										{
											do
											{
												FoundToken(47);
												NextChar();
											}
											while (ch >= '0' && ch <= '9');
											if (ch == '.')
											{
												goto IL_05b1;
											}
										}
									}
									else
									{
										NextChar();
										if (ch >= '0' && ch <= '9')
										{
											NextChar();
											if (ch >= '0' && ch <= '9')
											{
												NextChar();
												if (ch == '-')
												{
													NextChar();
													if (ch >= '0' && ch <= '9')
													{
														NextChar();
														if (ch >= '0' && ch <= '9')
														{
															FoundToken(50);
															NextChar();
															if (ch == 'T' || ch == 't')
															{
																NextChar();
																if (ch >= '0' && ch <= '9')
																{
																	NextChar();
																	if (ch >= '0' && ch <= '9')
																	{
																		NextChar();
																		if (ch == ':')
																		{
																			NextChar();
																			if (ch >= '0' && ch <= '9')
																			{
																				NextChar();
																				if (ch >= '0' && ch <= '9')
																				{
																					NextChar();
																					if (ch == ':')
																					{
																						NextChar();
																						if (ch >= '0' && ch <= '9')
																						{
																							NextChar();
																							if (ch >= '0' && ch <= '9')
																							{
																								FoundToken(51);
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				else
				{
					FoundToken(35);
				}
			}
			else
			{
				FoundToken(34);
				NextChar();
				if (ch != 'A' && ch != 'a')
				{
					if ((ch >= '0' && ch <= '9') || (ch >= 'B' && ch <= 'Z') || ch == '_' || (ch >= 'b' && ch <= 'z'))
					{
						goto IL_03d7;
					}
				}
				else
				{
					FoundToken(34);
					NextChar();
					if (ch != 'L' && ch != 'l')
					{
						if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
						{
							goto IL_03d7;
						}
					}
					else
					{
						FoundToken(34);
						NextChar();
						if (ch != 'S' && ch != 's')
						{
							if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
							{
								goto IL_03d7;
							}
						}
						else
						{
							FoundToken(34);
							NextChar();
							if (ch != 'E' && ch != 'e')
							{
								if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
								{
									goto IL_03d7;
								}
							}
							else
							{
								FoundToken(53);
								NextChar();
								if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
								{
									goto IL_03d7;
								}
							}
						}
					}
				}
			}
		}
		else
		{
			FoundToken(34);
			NextChar();
			if (ch != 'R' && ch != 'r')
			{
				if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
				{
					goto IL_03d7;
				}
			}
			else
			{
				FoundToken(34);
				NextChar();
				if (ch != 'U' && ch != 'u')
				{
					if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
					{
						goto IL_03d7;
					}
				}
				else
				{
					FoundToken(34);
					NextChar();
					if (ch != 'E' && ch != 'e')
					{
						if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
						{
							goto IL_03d7;
						}
					}
					else
					{
						FoundToken(52);
						NextChar();
						if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
						{
							goto IL_03d7;
						}
					}
				}
			}
		}
		goto IL_0c1e;
		IL_0c1e:
		CommitFoundToken();
		return;
		IL_03d7:
		do
		{
			FoundToken(34);
			NextChar();
		}
		while ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'));
		goto IL_0c1e;
		IL_05b1:
		NextChar();
		if (ch >= '0' && ch <= '9')
		{
			do
			{
				FoundToken(48);
				NextChar();
			}
			while (ch >= '0' && ch <= '9');
		}
		goto IL_0c1e;
	}

	public void NextToken14()
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
		if (ch != ')')
		{
			if (ch != '-')
			{
				if (ch != '!')
				{
					if (ch != 'T' && ch != 't')
					{
						if (ch != 'F' && ch != 'f')
						{
							if (ch != '(')
							{
								if (ch < '0' || ch > '9')
								{
									if (ch != '\'' && ch != '`' && ch != '‘')
									{
										if ((ch >= 'A' && ch <= 'Z') || (ch >= '_' && ch <= 'z'))
										{
											goto IL_032b;
										}
										if (ch == '"' || ch == '«' || ch == '»' || ch == '“' || ch == '”')
										{
											do
											{
												NextChar();
												if (ch == '"' || ch == '«' || ch == '»' || ch == '“' || ch == '”')
												{
													FoundToken(55);
													break;
												}
											}
											while (ch != '\uffff');
										}
									}
									else
									{
										do
										{
											NextChar();
											if (ch == '\'' || ch == '`' || ch == '‘')
											{
												FoundToken(54);
												break;
											}
										}
										while (ch != '\uffff');
									}
								}
								else
								{
									FoundToken(47);
									NextChar();
									if (ch < '0' || ch > '9')
									{
										if (ch == '.')
										{
											goto IL_0617;
										}
									}
									else
									{
										FoundToken(47);
										NextChar();
										if (ch < '0' || ch > '9')
										{
											if (ch == '.')
											{
												goto IL_0617;
											}
										}
										else
										{
											FoundToken(47);
											NextChar();
											if (ch < '0' || ch > '9')
											{
												if (ch == '.')
												{
													goto IL_0617;
												}
											}
											else
											{
												FoundToken(47);
												NextChar();
												if (ch != '-')
												{
													if (ch < '0' || ch > '9')
													{
														if (ch == '.')
														{
															goto IL_0617;
														}
													}
													else
													{
														do
														{
															FoundToken(47);
															NextChar();
														}
														while (ch >= '0' && ch <= '9');
														if (ch == '.')
														{
															goto IL_0617;
														}
													}
												}
												else
												{
													NextChar();
													if (ch >= '0' && ch <= '9')
													{
														NextChar();
														if (ch >= '0' && ch <= '9')
														{
															NextChar();
															if (ch == '-')
															{
																NextChar();
																if (ch >= '0' && ch <= '9')
																{
																	NextChar();
																	if (ch >= '0' && ch <= '9')
																	{
																		FoundToken(50);
																		NextChar();
																		if (ch == 'T' || ch == 't')
																		{
																			NextChar();
																			if (ch >= '0' && ch <= '9')
																			{
																				NextChar();
																				if (ch >= '0' && ch <= '9')
																				{
																					NextChar();
																					if (ch == ':')
																					{
																						NextChar();
																						if (ch >= '0' && ch <= '9')
																						{
																							NextChar();
																							if (ch >= '0' && ch <= '9')
																							{
																								NextChar();
																								if (ch == ':')
																								{
																									NextChar();
																									if (ch >= '0' && ch <= '9')
																									{
																										NextChar();
																										if (ch >= '0' && ch <= '9')
																										{
																											FoundToken(51);
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
							else
							{
								FoundToken(35);
							}
						}
						else
						{
							FoundToken(34);
							NextChar();
							if (ch != 'A' && ch != 'a')
							{
								if ((ch >= '0' && ch <= '9') || (ch >= 'B' && ch <= 'Z') || ch == '_' || (ch >= 'b' && ch <= 'z'))
								{
									goto IL_032b;
								}
							}
							else
							{
								FoundToken(34);
								NextChar();
								if (ch != 'L' && ch != 'l')
								{
									if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
									{
										goto IL_032b;
									}
								}
								else
								{
									FoundToken(34);
									NextChar();
									if (ch != 'S' && ch != 's')
									{
										if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
										{
											goto IL_032b;
										}
									}
									else
									{
										FoundToken(34);
										NextChar();
										if (ch != 'E' && ch != 'e')
										{
											if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
											{
												goto IL_032b;
											}
										}
										else
										{
											FoundToken(53);
											NextChar();
											if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
											{
												goto IL_032b;
											}
										}
									}
								}
							}
						}
					}
					else
					{
						FoundToken(34);
						NextChar();
						if (ch != 'R' && ch != 'r')
						{
							if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
							{
								goto IL_032b;
							}
						}
						else
						{
							FoundToken(34);
							NextChar();
							if (ch != 'U' && ch != 'u')
							{
								if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
								{
									goto IL_032b;
								}
							}
							else
							{
								FoundToken(34);
								NextChar();
								if (ch != 'E' && ch != 'e')
								{
									if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
									{
										goto IL_032b;
									}
								}
								else
								{
									FoundToken(52);
									NextChar();
									if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'))
									{
										goto IL_032b;
									}
								}
							}
						}
					}
				}
				else
				{
					FoundToken(44);
				}
			}
			else
			{
				FoundToken(21);
			}
		}
		else
		{
			FoundToken(36);
		}
		goto IL_0c84;
		IL_032b:
		do
		{
			FoundToken(34);
			NextChar();
		}
		while ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'));
		goto IL_0c84;
		IL_0617:
		NextChar();
		if (ch >= '0' && ch <= '9')
		{
			do
			{
				FoundToken(48);
				NextChar();
			}
			while (ch >= '0' && ch <= '9');
		}
		goto IL_0c84;
		IL_0c84:
		CommitFoundToken();
	}

	public void NextToken15()
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
		if (ch == '(')
		{
			FoundToken(35);
		}
		CommitFoundToken();
	}

	public void NextToken16()
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
		if (ch == 'N' || ch == 'n')
		{
			NextChar();
			if (ch != 'U' && ch != 'u')
			{
				if (ch == 'O' || ch == 'o')
				{
					NextChar();
					if (ch == 'T' || ch == 't')
					{
						FoundToken(42);
					}
				}
			}
			else
			{
				NextChar();
				if (ch == 'L' || ch == 'l')
				{
					NextChar();
					if (ch == 'L' || ch == 'l')
					{
						FoundToken(41);
					}
				}
			}
		}
		CommitFoundToken();
	}

	public void NextToken17()
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
				FoundToken(34);
				NextChar();
			}
			while ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || ch == '_' || (ch >= 'a' && ch <= 'z'));
		}
		CommitFoundToken();
	}

	public void NextToken18()
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
		if (ch != '.')
		{
			if (ch != '?')
			{
				if (ch != 'L' && ch != 'l')
				{
					if (ch != '+')
					{
						if (ch != '-')
						{
							if (ch != '*')
							{
								if (ch != '/')
								{
									if (ch != '%')
									{
										if (ch != '[')
										{
											if (ch != ')')
											{
												if (ch != ':')
												{
													if (ch != ']')
													{
														if (ch != ',')
														{
															if (ch != 'O' && ch != 'o')
															{
																if (ch != '|')
																{
																	if (ch != 'A' && ch != 'a')
																	{
																		if (ch != '&')
																		{
																			if (ch != '=')
																			{
																				if (ch != 'E' && ch != 'e')
																				{
																					if (ch != '!')
																					{
																						if (ch != '<')
																						{
																							if (ch != 'N' && ch != 'n')
																							{
																								if (ch != 'G' && ch != 'g')
																								{
																									if (ch == '>')
																									{
																										FoundToken(30);
																										NextChar();
																										if (ch == '=')
																										{
																											goto IL_06c0;
																										}
																									}
																								}
																								else
																								{
																									NextChar();
																									if (ch != 'T' && ch != 't')
																									{
																										if (ch == 'E' || ch == 'e')
																										{
																											goto IL_06c0;
																										}
																									}
																									else
																									{
																										FoundToken(30);
																									}
																								}
																							}
																							else
																							{
																								NextChar();
																								if (ch == 'E' || ch == 'e')
																								{
																									NextChar();
																									if (ch == 'Q' || ch == 'q')
																									{
																										goto IL_0682;
																									}
																								}
																							}
																						}
																						else
																						{
																							FoundToken(28);
																							NextChar();
																							if (ch == '>')
																							{
																								goto IL_0682;
																							}
																							if (ch == '=')
																							{
																								goto IL_0622;
																							}
																						}
																					}
																					else
																					{
																						NextChar();
																						if (ch == '=')
																						{
																							goto IL_0682;
																						}
																					}
																				}
																				else
																				{
																					NextChar();
																					if (ch == 'Q' || ch == 'q')
																					{
																						goto IL_0677;
																					}
																				}
																			}
																			else
																			{
																				FoundToken(25);
																				NextChar();
																				if (ch == '=')
																				{
																					goto IL_0677;
																				}
																			}
																		}
																		else
																		{
																			NextChar();
																			if (ch == '&')
																			{
																				goto IL_0669;
																			}
																		}
																	}
																	else
																	{
																		NextChar();
																		if (ch == 'N' || ch == 'n')
																		{
																			NextChar();
																			if (ch == 'D' || ch == 'd')
																			{
																				goto IL_0669;
																			}
																		}
																	}
																}
																else
																{
																	NextChar();
																	if (ch == '|')
																	{
																		goto IL_0630;
																	}
																}
															}
															else
															{
																NextChar();
																if (ch == 'R' || ch == 'r')
																{
																	goto IL_0630;
																}
															}
														}
														else
														{
															FoundToken(45);
														}
													}
													else
													{
														FoundToken(10);
													}
												}
												else
												{
													FoundToken(7);
												}
											}
											else
											{
												FoundToken(36);
											}
										}
										else
										{
											FoundToken(9);
										}
									}
									else
									{
										FoundToken(24);
									}
								}
								else
								{
									FoundToken(23);
								}
							}
							else
							{
								FoundToken(22);
							}
						}
						else
						{
							FoundToken(21);
						}
					}
					else
					{
						FoundToken(20);
					}
				}
				else
				{
					NextChar();
					if (ch != 'I' && ch != 'i')
					{
						if (ch != 'T' && ch != 't')
						{
							if (ch == 'E' || ch == 'e')
							{
								goto IL_0622;
							}
						}
						else
						{
							FoundToken(28);
						}
					}
					else
					{
						NextChar();
						if (ch == 'K' || ch == 'k')
						{
							NextChar();
							if (ch == 'E' || ch == 'e')
							{
								FoundToken(26);
							}
						}
					}
				}
			}
			else
			{
				FoundToken(6);
				NextChar();
				if (ch == '?')
				{
					FoundToken(11);
				}
			}
		}
		else
		{
			FoundToken(46);
		}
		goto IL_06fe;
		IL_06c0:
		FoundToken(31);
		goto IL_06fe;
		IL_0677:
		FoundToken(25);
		goto IL_06fe;
		IL_06fe:
		CommitFoundToken();
		return;
		IL_0682:
		FoundToken(27);
		goto IL_06fe;
		IL_0669:
		FoundToken(32);
		goto IL_06fe;
		IL_0622:
		FoundToken(29);
		goto IL_06fe;
		IL_0630:
		FoundToken(33);
		goto IL_06fe;
	}

	public void NextToken19()
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
		if (ch == ')')
		{
			FoundToken(36);
		}
		CommitFoundToken();
	}

	public void NextToken20()
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
		if (ch == ':')
		{
			FoundToken(7);
		}
		CommitFoundToken();
	}

	public void NextToken21()
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
		if (ch != ']')
		{
			if (ch == '[')
			{
				FoundToken(9);
			}
		}
		else
		{
			FoundToken(10);
		}
		CommitFoundToken();
	}

	public void NextToken22()
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
		if (ch != ')')
		{
			if (ch == ',')
			{
				FoundToken(45);
			}
		}
		else
		{
			FoundToken(36);
		}
		CommitFoundToken();
	}

	public void NextToken23()
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
		if (ch == 'N' || ch == 'n')
		{
			NextChar();
			if (ch == 'U' || ch == 'u')
			{
				NextChar();
				if (ch == 'L' || ch == 'l')
				{
					NextChar();
					if (ch == 'L' || ch == 'l')
					{
						FoundToken(41);
					}
				}
			}
		}
		CommitFoundToken();
	}

	public void NextToken24()
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
		if (ch != '?')
		{
			if (ch != '[')
			{
				if (ch != ')')
				{
					if (ch != ':')
					{
						if (ch != ',')
						{
							if (ch == ']')
							{
								FoundToken(10);
							}
						}
						else
						{
							FoundToken(45);
						}
					}
					else
					{
						FoundToken(7);
					}
				}
				else
				{
					FoundToken(36);
				}
			}
			else
			{
				FoundToken(9);
			}
		}
		else
		{
			FoundToken(6);
		}
		CommitFoundToken();
	}

	public void NextToken25()
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
		if (ch != ')')
		{
			if (ch != ':')
			{
				if (ch == ',')
				{
					FoundToken(45);
				}
			}
			else
			{
				FoundToken(7);
			}
		}
		else
		{
			FoundToken(36);
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
