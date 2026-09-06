using System;

namespace ApiPerleRare.Models;

public class Erreur
{
	public int ECle { get; set; }

	public DateTime EDatetime { get; set; }

	public string EType { get; set; }

	public string EImportance { get; set; }

	public string EFrom { get; set; }

	public string EDetail { get; set; }

	public int EOcurrences { get; set; }
}
