using System;

namespace ApiPerleRare.Controllers;

public class ProspectEvenements
{
	public int ERefEvenement { get; set; }

	public DateTime EDate { get; set; }

	public DateTime? EDateCreation { get; set; }

	public string CStatut { get; set; }

	public string ETypeEvenement { get; set; }

	public string CNomFamille { get; set; }

	public string ETexte { get; set; }

	public string ENomContact { get; set; }

	public uint? ERefContact { get; set; }

	public uint? ERefConseiller { get; set; }

	public string CNegociateur { get; set; }

	public string CNegociateur_PS { get; set; }

	public string CNegociateur_Prenom { get; set; }

	public string EConseiller { get; set; }

	public string EConseiller_Prenom { get; set; }

	public string EConseiller_Nom { get; set; }

	public string EConseiller_PS { get; set; }

	public string EAppointmentId { get; set; }
}
