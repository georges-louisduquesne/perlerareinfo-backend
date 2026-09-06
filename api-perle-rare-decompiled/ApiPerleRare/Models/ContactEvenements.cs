using System;

namespace ApiPerleRare.Models;

public class ContactEvenements
{
	public uint? ERefContact { get; set; }

	public int ERefEvenement { get; set; }

	public DateTime EDate { get; set; }

	public string ETypeEvenement { get; set; }

	public string ECr { get; set; }

	public string EMail { get; set; }

	public uint? ERefBien { get; set; }

	public uint? ERefInterI { get; set; }

	public uint? ERefInterD { get; set; }

	public string ETexte { get; set; }

	public uint? BRef { get; set; }

	public string BCp { get; set; }

	public string BAdresse { get; set; }

	public uint? I2RefIntermIndirect { get; set; }

	public string I2NomIntermIndirect { get; set; }

	public string I2Icon { get; set; }

	public uint? IRefIntermediaire { get; set; }

	public string INomIntermediaire { get; set; }

	public string QSigneInterD { get; set; }

	public uint? CRefCtcInter { get; set; }

	public string CNomIntermediaire { get; set; }

	public string CNom { get; set; }

	public string CPrenom { get; set; }

	public string QSigneCtcInter { get; set; }
}
