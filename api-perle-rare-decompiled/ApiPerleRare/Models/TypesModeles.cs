using System.Collections.Generic;

namespace ApiPerleRare.Models;

public class TypesModeles
{
	public short TmRef { get; set; }

	public string TmNom { get; set; }

	public virtual ICollection<ModelesCommerciaux> ModelesCommerciaux { get; set; } = new List<ModelesCommerciaux>();
}
