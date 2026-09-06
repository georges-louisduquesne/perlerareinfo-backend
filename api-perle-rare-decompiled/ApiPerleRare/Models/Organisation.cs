using System;

namespace ApiPerleRare.Models;

public class Organisation
{
	public string ORefContact { get; set; }

	public string OIntermediaire { get; set; }

	public DateOnly OAlerteDate { get; set; }

	public DateOnly ODateContactTel { get; set; }

	public DateOnly ODateVisite { get; set; }
}
