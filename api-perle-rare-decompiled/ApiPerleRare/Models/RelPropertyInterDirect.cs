namespace ApiPerleRare.Models;

public class RelPropertyInterDirect
{
	public string PPropertyId { get; set; }

	public uint IRefIntermediaire { get; set; }

	public virtual IntermediairesDirects IRefIntermediaireNavigation { get; set; }

	public virtual Property PProperty { get; set; }
}
