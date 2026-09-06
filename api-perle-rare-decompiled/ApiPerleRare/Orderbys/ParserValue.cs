namespace ApiPerleRare.Orderbys;

internal class ParserValue
{
	public int SymbolId;

	public object Value;

	public ParserValue()
	{
	}

	public ParserValue(int symbolId)
	{
		SymbolId = symbolId;
	}

	public ParserValue(int symbolId, object value)
	{
		SymbolId = symbolId;
		Value = value;
	}

	public override string ToString()
	{
		return $"{SymbolId}:{Value}";
	}
}
