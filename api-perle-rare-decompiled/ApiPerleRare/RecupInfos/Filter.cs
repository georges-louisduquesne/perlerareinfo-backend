namespace ApiPerleRare.RecupInfos;

public class Filter
{
	public TypeTransaction TypeTransaction { get; set; }

	public TypeBien[] TypeBien { get; set; }

	public string[] Infos { get; set; }

	public int[] CP { get; set; }

	public string[] Quartiers { get; set; }

	public string[] NbPieces { get; set; }

	public string[] NbChambres { get; set; }

	public string[] Etage { get; set; }

	public bool? EstDernierEtage { get; set; }

	public bool? EstExclusif { get; set; }

	public bool? AvecBaissePrix { get; set; }

	public int SurfaceMin { get; set; }

	public int SurfaceMax { get; set; }

	public int BudgetMin { get; set; }

	public int BudgetMax { get; set; }

	public int BudgetSurfaceMin { get; set; }

	public int BudgetSurfaceMax { get; set; }

	public int AncienneteMin { get; set; }

	public int AncienneteMax { get; set; }

	public int[] Tags { get; set; }

	public bool Apply { get; set; }

	public uint ContactRef { get; set; }
}
