using System;

namespace ApiPerleRare.Models;

public class HomePageLoads
{
	public long HplRef { get; set; }

	public uint HplRefConseiller { get; set; }

	public int HplDurationMs { get; set; }

	public DateTime HplMeasuredAt { get; set; }

	/// <summary>JSON v1 (télémétrie fine), voir HomePageLoadBreakdown.</summary>
	public string HplBreakdown { get; set; }
}
