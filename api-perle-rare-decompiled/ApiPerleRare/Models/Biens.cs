using System.Collections.Generic;

namespace ApiPerleRare.Models;

public class Biens
{
	public uint BRef { get; set; }

	public string BCp { get; set; }

	public string BAdresse { get; set; }

	public uint? BRefAnnAgc { get; set; }

	public uint? BRefCtcInter { get; set; }

	public uint? BRefInterD { get; set; }

	public uint? BRefInterI { get; set; }

	public virtual ContactIntermediaire BRefCtcInterNavigation { get; set; }

	public virtual IntermediairesDirects BRefInterDNavigation { get; set; }

	public virtual IntermediairesIndirects BRefInterINavigation { get; set; }

	public virtual ICollection<Evenements> Evenements { get; set; } = new List<Evenements>();
}
