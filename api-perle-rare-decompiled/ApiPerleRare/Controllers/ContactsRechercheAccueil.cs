using System;

namespace ApiPerleRare.Controllers;

public class ContactsRechercheAccueil : ContactsRechercheEx
{
	public int _NbBiensLus { get; set; }

	public int _NbBiensDemiEtoile { get; set; }

	public int _NbBiensEtoile { get; set; }

	public int _NbBiensNonLues { get; set; }

	public DateTime? _BiensNonLuesDate { get; set; }

	public bool _AvecTaches { get; set; }

	public int _NbAnnoncesNonLues { get; set; }

	public DateTime? _AnnoncesNonLuesDate { get; set; }

	public int _NbTaches { get; set; }
}
