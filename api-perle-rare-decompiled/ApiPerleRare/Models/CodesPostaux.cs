using System.Collections.Generic;

namespace ApiPerleRare.Models;

public class CodesPostaux
{
	public uint CpId { get; set; }

	public string CpNom { get; set; }

	public bool? CpActif { get; set; }

	public int CpInsee { get; set; }

	public string CpAnnoncesjaunes { get; set; }

	public string CpAvendrealouer { get; set; }

	public string CpExplorimmo { get; set; }

	public string CpLeboncoin { get; set; }

	public string CpLogicimmo { get; set; }

	public string CpPap { get; set; }

	public string CpRefleximmo { get; set; }

	public string CpSeloger { get; set; }

	public int CpIdYanport { get; set; }

	public virtual ICollection<Quartiers> Quartiers { get; set; } = new List<Quartiers>();
}
