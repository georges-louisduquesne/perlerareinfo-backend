using System;

namespace ApiPerleRare.Models;

public class Taches
{
	public uint TRef { get; set; }

	public DateTime TDateCreation { get; set; }

	public uint TRefContact { get; set; }

	public string TType { get; set; }

	public string TQui { get; set; }

	public string TCom { get; set; }

	public string TEtat { get; set; }

	public DateTime TDateRealisation { get; set; }

	public int TRefAnnonce { get; set; }

	public string TLien { get; set; }

	public string TPropertyId { get; set; }

	public virtual ContactsRecherche TRefContactNavigation { get; set; }
}
