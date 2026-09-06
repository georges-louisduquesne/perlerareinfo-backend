using System;

namespace ApiPerleRare.Models;

public class StratalisExpired
{
	public string AnnonceId { get; set; }

	public DateTime? LastSeenOn { get; set; }

	public DateTime Timestamp { get; set; }
}
