using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApiPerleRare.Models;

public class ContactIntermediaire
{
	public uint CRefCtcInter { get; set; }

	public string CNomIntermediaire { get; set; }

	public string CNom { get; set; }

	public string CPrenom { get; set; }

	public string CTel { get; set; }

	public string CMel { get; set; }

	public string CQualiteRelation { get; set; }

	public string CCom { get; set; }

	public string CStatut { get; set; }

	public bool CDirecteur { get; set; }

	public bool CRepondMailRecherche { get; set; }

	public bool CPasDeMails { get; set; }

	public uint? CRefIntermediaire { get; set; }

	public string CUrl { get; set; }

	public string CPhoto { get; set; }

	public virtual ICollection<Biens> Biens { get; set; } = new List<Biens>();

	[JsonIgnore]
	public virtual IntermediairesDirects CRefIntermediaireNavigation { get; set; }

	[JsonIgnore]
	public virtual ICollection<Evenements> Evenements { get; set; } = new List<Evenements>();

	[JsonIgnore]
	public virtual ICollection<Property> PProperty { get; set; } = new List<Property>();
}
