using System;

namespace ApiPerleRare.Controllers;

public class ProspectTaches : ITachesEx
{
	public uint TRef { get; set; }

	public DateTime TDateRealisation { get; set; }

	public string CNomFamille { get; set; }

	public string TType { get; set; }

	public string TCom { get; set; }

	public uint TRefContact { get; set; }

	public string TQui { get; set; }

	public string TQui_PS { get; set; }

	public string CNegociateur { get; set; }

	public string CNegociateur_PS { get; set; }
}
