using System;

namespace ApiPerleRare.Models;

public class YanportInterruptions
{
	public int YiId { get; set; }

	public string YiRaison { get; set; }

	public DateTime? YiDateDebut { get; set; }

	public DateTime? YiDateFin { get; set; }
}
