namespace ApiPerleRare.Models;

public class Quartiers
{
	public uint QRef { get; set; }

	public bool? QActif { get; set; }

	public uint QCp { get; set; }

	public string QNomQuartier { get; set; }

	public string QTag { get; set; }

	public long QQuarterIdYanport { get; set; }

	public virtual CodesPostaux QCpNavigation { get; set; }
}
