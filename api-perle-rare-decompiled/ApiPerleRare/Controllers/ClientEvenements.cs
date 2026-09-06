using System;

namespace ApiPerleRare.Controllers;

public class ClientEvenements
{
	public int ERefEvenement { get; set; }

	public DateTime EDate { get; set; }

	public string ETypeEvenement { get; set; }

	public string ETexte { get; set; }

	public uint? ERefContact { get; set; }

	public string CNomFamille { get; set; }

	public string CNomFamilleConseiller { get; set; }

	public decimal? CMttHono { get; set; }

	public string BCp { get; set; }

	public string BAdresse { get; set; }

	public uint BRef { get; set; }

	public string EConseiller_PS { get; set; }
}
