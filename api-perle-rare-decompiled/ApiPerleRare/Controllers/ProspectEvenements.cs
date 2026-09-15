using System;

namespace ApiPerleRare.Controllers;

public class ProspectEvenements
{
	public int ERefEvenement { get; set; }

	public DateTime EDate { get; set; }

	public DateTime? EDateCreation { get; set; }

	public string ETypeEvenement { get; set; }

	public string CNomFamille { get; set; }

	public string ETexte { get; set; }

	public string ENomContact { get; set; }

	public uint? ERefContact { get; set; }

	public string CNegociateur { get; set; }

	public string EConseiller_PS { get; set; }
}
