using System.Collections.Generic;

namespace ApiPerleRare.RecupInfos;

public class RecupInfoCountAvecBienIds : RecupInfoCount
{
	public List<long> BienIds { get; set; } = new List<long>();
}
