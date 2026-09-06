namespace ApiPerleRare.Predicates;

public class StringInfo
{
	public string AsString;

	public int OpOrder;

	public static implicit operator StringInfo(string s)
	{
		return new StringInfo
		{
			AsString = s
		};
	}

	public override string ToString()
	{
		return AsString;
	}
}
