using System;

namespace ApiPerleRare.Models;

public class UrlSearch
{
	public int UsCle { get; set; }

	public int UsMoteur { get; set; }

	public string UsUrl { get; set; }

	public bool UsSuspendu { get; set; }

	public int UsMajoration { get; set; }

	public DateTime UsDernierPassage { get; set; }

	public DateTime UsProchainPassage { get; set; }

	public bool UsEnCours { get; set; }

	public string UsLocalite { get; set; }

	public string UsBien { get; set; }

	public string UsTransaction { get; set; }

	public int UsLastCount { get; set; }

	public int UsMaxCount { get; set; }

	public short? UsType { get; set; }
}
