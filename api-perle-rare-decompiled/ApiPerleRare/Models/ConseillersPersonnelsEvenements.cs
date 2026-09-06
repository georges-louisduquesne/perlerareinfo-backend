using System;

namespace ApiPerleRare.Models;

public class ConseillersPersonnelsEvenements
{
	public int CpeRefEvenement { get; set; }

	public string CpeTypeEvenement { get; set; }

	public uint? CpeRefConseiller { get; set; }

	public DateOnly CpeDateCreation { get; set; }

	public DateOnly? CpeDateRealisation { get; set; }

	public string CpeCommentaire { get; set; }

	public sbyte CpeStatut { get; set; }

	public virtual ConseillersPersonnels CpeRefConseillerNavigation { get; set; }
}
