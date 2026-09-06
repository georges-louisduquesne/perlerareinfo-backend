using System.Collections.Generic;

namespace ApiPerleRare.Models;

public class IntermediairesDirects
{
	public uint IRefIntermediaire { get; set; }

	public long IIdYanport { get; set; }

	public sbyte IActif { get; set; }

	public string INomIntermediaire { get; set; }

	public string IGroupeEventuel { get; set; }

	public string IDiffusionSitePropre { get; set; }

	public string ICategorie { get; set; }

	public int? IVolumetrieAnnonces { get; set; }

	public string IAdresse1 { get; set; }

	public int? ICodePostal { get; set; }

	public string IVille { get; set; }

	public string ITelephone { get; set; }

	public string ITelephone2 { get; set; }

	public string IMel { get; set; }

	public string ICommentaires { get; set; }

	public sbyte? IParis1 { get; set; }

	public sbyte? IParis2 { get; set; }

	public sbyte? IParis3 { get; set; }

	public sbyte? IParis4 { get; set; }

	public sbyte? IParis5 { get; set; }

	public sbyte? IParis6 { get; set; }

	public sbyte? IParis7 { get; set; }

	public sbyte? IParis8 { get; set; }

	public sbyte? IParis9 { get; set; }

	public sbyte? IParis10 { get; set; }

	public sbyte? IParis11 { get; set; }

	public sbyte? IParis12 { get; set; }

	public sbyte? IParis13 { get; set; }

	public sbyte? IParis14 { get; set; }

	public sbyte? IParis15 { get; set; }

	public sbyte? IParis16 { get; set; }

	public sbyte? IParis17 { get; set; }

	public sbyte? IParis18 { get; set; }

	public sbyte? IParis19 { get; set; }

	public sbyte? IParis20 { get; set; }

	public sbyte? IIssyLesMoulineaux { get; set; }

	public sbyte? IVanves { get; set; }

	public sbyte? IBoulogne { get; set; }

	public sbyte? ILevallois { get; set; }

	public sbyte? INeuilly { get; set; }

	public sbyte? IPuteaux { get; set; }

	public sbyte? ICourbevoie { get; set; }

	public sbyte? IVincennes { get; set; }

	public sbyte? ISaintMande { get; set; }

	public sbyte? ICharenton { get; set; }

	public string IStanding { get; set; }

	public string ITypeBien { get; set; }

	public string IQualiteRelation { get; set; }

	public string IIntercabinet { get; set; }

	public string IAdhIntercabinet { get; set; }

	public string ISirenYanport { get; set; }

	public uint? IRefGroupe { get; set; }

	public string ILogo { get; set; }

	public virtual ICollection<Biens> Biens { get; set; } = new List<Biens>();

	public virtual ICollection<ContactIntermediaire> ContactIntermediaire { get; set; } = new List<ContactIntermediaire>();

	public virtual ICollection<Evenements> Evenements { get; set; } = new List<Evenements>();

	public virtual IntermediairesIndirects IRefGroupeNavigation { get; set; }

	public virtual ICollection<IntermediairesDirectsYanport> IntermediairesDirectsYanport { get; set; } = new List<IntermediairesDirectsYanport>();

	public virtual ICollection<Property> PProperty { get; set; } = new List<Property>();
}
