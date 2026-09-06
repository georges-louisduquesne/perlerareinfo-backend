using System;

namespace ApiPerleRare.Models;

public class Evenements
{
	public int ERefEvenement { get; set; }

	public string ETypeEvenement { get; set; }

	public uint? ERefBien { get; set; }

	public uint? ERefConseiller { get; set; }

	public uint? ERefCtcInter { get; set; }

	public uint? ERefInterD { get; set; }

	public uint? ERefInterI { get; set; }

	public uint? ERefAnnAgc { get; set; }

	public byte EStatut { get; set; }

	public uint? ERefContact { get; set; }

	public string ENomContact { get; set; }

	public string ETexte { get; set; }

	public DateTime EDate { get; set; }

	public string ECr { get; set; }

	public string EMail { get; set; }

	public string EPropertyId { get; set; }

	public virtual Biens ERefBienNavigation { get; set; }

	public virtual ConseillersPersonnels ERefConseillerNavigation { get; set; }

	public virtual ContactsRecherche ERefContactNavigation { get; set; }

	public virtual ContactIntermediaire ERefCtcInterNavigation { get; set; }

	public virtual IntermediairesDirects ERefInterDNavigation { get; set; }

	public virtual IntermediairesIndirects ERefInterINavigation { get; set; }
}
