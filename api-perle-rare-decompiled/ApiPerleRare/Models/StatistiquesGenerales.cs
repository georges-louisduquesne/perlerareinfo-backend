using System;

namespace ApiPerleRare.Models;

public class StatistiquesGenerales
{
	public string SgMoteur { get; set; }

	public DateOnly SgDate { get; set; }

	public int SgStock { get; set; }

	public int SgFluxIn { get; set; }

	public int SgFluxOut { get; set; }

	public DateTime SgTimeUpdate { get; set; }

	public int SgBiensStock { get; set; }

	public int SgBiensIn { get; set; }

	public int SgBiensOut { get; set; }
}
