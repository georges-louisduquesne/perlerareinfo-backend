using System;

namespace ApiPerleRare.YanportModels;

public class Marketing
{
	public bool Active { get; set; }

	public string Type { get; set; }

	public float? Price { get; set; }

	public DateTime? PublicationStartDate { get; set; }

	public DateTime? PublicationEndDate { get; set; }

	public Dealer[] Dealers { get; set; }

	public bool? Occupied { get; set; }

	public bool? ExclusiveMandate { get; set; }

	public PriceEvent[] PriceEvents { get; set; }
}
