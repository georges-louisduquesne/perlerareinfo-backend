using System;

namespace ApiPerleRare.Models;

public class PropertyContact
{
	public uint PcId { get; set; }

	public string PcPropertyId { get; set; }

	public uint PcRefContact { get; set; }

	public bool PcActif { get; set; }

	public uint PcRate { get; set; }

	public string PcCom { get; set; }

	public bool PcVu { get; set; }

	public DateTime PcDateAff { get; set; }

	public virtual Property PcProperty { get; set; }

	public virtual ContactsRecherche PcRefContactNavigation { get; set; }
}
