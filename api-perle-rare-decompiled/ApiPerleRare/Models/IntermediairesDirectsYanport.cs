namespace ApiPerleRare.Models;

public class IntermediairesDirectsYanport
{
	public uint IRefIntermediaire { get; set; }

	public ulong IYanportId { get; set; }

	public string ISource { get; set; }

	public virtual IntermediairesDirects IRefIntermediaireNavigation { get; set; }
}
