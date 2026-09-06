using System.Collections.Generic;

namespace ApiPerleRare.Models;

public class MetaMoteurs
{
	public string MNom { get; set; }

	public byte MStatut { get; set; }

	public bool MProxyActif { get; set; }

	public int MInterindirect { get; set; }

	public int Etape { get; set; }

	public string MUrlSearch { get; set; }

	public string MUrlTagLocalisation { get; set; }

	public string MUrlTagTransaction { get; set; }

	public string MUrlTagBien { get; set; }

	public string MUrlTagSurfaceMin { get; set; }

	public string MUrlTagSurfaceMax { get; set; }

	public string MUrlTagPrixMin { get; set; }

	public string MUrlTagPrixMax { get; set; }

	public bool? MUrlCpOrInsee { get; set; }

	public string MUrlTypeBienMaison { get; set; }

	public string MUrlTypeBienAppart { get; set; }

	public string MUrlTypeBienLocal { get; set; }

	public string MUrlTypeTransacAchat { get; set; }

	public string MUrlTypeTransacLoc { get; set; }

	public int? MOrdre { get; set; }

	public virtual ICollection<AnnoncesGlobales> AnnoncesGlobales { get; set; } = new List<AnnoncesGlobales>();
}
