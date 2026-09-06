using System;

namespace ApiPerleRare.Models;

public class HistoriqueAspiration
{
	public string HaMoteur { get; set; }

	public int HaCp { get; set; }

	public DateOnly HaDate { get; set; }

	public int HaNbAnnoncesStock { get; set; }

	public int HaMoyenneStock { get; set; }

	public int HaFluxInDuJour { get; set; }

	public int HaMoyenneFluxIn { get; set; }

	public int HaFluxOutDuJour { get; set; }

	public int HaMoyenneFluxOut { get; set; }

	public DateTime HaUpdateAnnoncesGlobales { get; set; }

	public DateTime HaUpdateAspiration { get; set; }

	public string HaEtat { get; set; }

	public int HaBiensStock { get; set; }

	public int HaBiensIn { get; set; }

	public int HaBiensOut { get; set; }
}
