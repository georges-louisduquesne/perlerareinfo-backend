using System;

namespace ApiPerleRare.Models;

public class HomePageLoads
{
	public long HplRef { get; set; }

	public uint HplRefConseiller { get; set; }

	public int HplDurationMs { get; set; }

	public DateTime HplMeasuredAt { get; set; }
}
