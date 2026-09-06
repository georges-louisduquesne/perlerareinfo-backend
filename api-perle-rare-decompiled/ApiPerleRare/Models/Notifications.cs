using System;

namespace ApiPerleRare.Models;

public class Notifications
{
	public uint NId { get; set; }

	public string NType { get; set; }

	public uint NRefConseiller { get; set; }

	public uint NRefContact { get; set; }

	public uint NRefAnn { get; set; }

	public string NPropertyId { get; set; }

	public DateTime NCreatedOn { get; set; }

	public sbyte NState { get; set; }

	public string NOldPrix { get; set; }

	public string NNewPrix { get; set; }
}
