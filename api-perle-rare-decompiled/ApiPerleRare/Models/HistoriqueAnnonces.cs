using System;

namespace ApiPerleRare.Models;

public class HistoriqueAnnonces
{
	public uint HRef { get; set; }

	public uint HRefAnnonce { get; set; }

	public DateOnly HDate { get; set; }

	public string HTypeVariation { get; set; }

	public uint HAncienneValeur { get; set; }

	public uint HNouvelleValeur { get; set; }

	public virtual AnnoncesGlobales HRefAnnonceNavigation { get; set; }
}
