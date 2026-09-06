using System;

namespace ApiPerleRare.Models;

public class UrlLogs
{
	public int UlId { get; set; }

	public string UlUrl { get; set; }

	public sbyte UlIsIn { get; set; }

	public sbyte UlIsUpdate { get; set; }

	public int UlNb { get; set; }

	public DateTime UlDate { get; set; }
}
