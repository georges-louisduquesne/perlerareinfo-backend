using System;

namespace ApiPerleRare.YanportModels;

public class Hit
{
	public Guid Id { get; set; }

	public Guid AggregatePropertyId { get; set; }

	public string Type { get; set; }

	public Marketing Marketing { get; set; }

	public Ad[] Ads { get; set; }

	public Features Features { get; set; }

	public Address Address { get; set; }

	public bool IsActive => true;
}
