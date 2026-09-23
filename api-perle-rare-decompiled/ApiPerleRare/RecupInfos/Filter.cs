using System.Text.Json.Serialization;

namespace ApiPerleRare.RecupInfos;

public class Filter
{
	[JsonConverter(typeof(TypeTransactionListJsonConverter))]
	public TypeTransaction[] TypeTransaction { get; set; }

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

	/// <summary>
	/// Optional OR-group of required tags (at least one). Combined with Tags AND / NOT.
	/// </summary>
	public int[] TagsOr { get; set; }

	/// <summary>
	/// Fallback when TagsOr is empty: "or" treats all positive Tags as the OR group.
	/// </summary>
	public string TagsMode { get; set; }

	public bool Apply { get; set; }

	public uint ContactRef { get; set; }
}
