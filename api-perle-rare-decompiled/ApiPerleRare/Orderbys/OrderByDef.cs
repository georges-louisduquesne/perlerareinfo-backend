using System.Collections.Generic;

namespace ApiPerleRare.Orderbys;

public class OrderByDef
{
	public string[] FieldNames { get; }

	public bool Asc { get; }

	public OrderByDef(List<string> fields, bool asc)
	{
		FieldNames = fields.ToArray();
		Asc = asc;
	}
}
