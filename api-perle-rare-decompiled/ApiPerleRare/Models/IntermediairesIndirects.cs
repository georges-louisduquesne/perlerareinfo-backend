using System.Collections.Generic;

namespace ApiPerleRare.Models;

public class IntermediairesIndirects
{
	public uint I2RefIntermIndirect { get; set; }

	public string I2NomIntermIndirect { get; set; }

	public sbyte I2Actif { get; set; }

	public string I2UrlSite { get; set; }

	public sbyte? I2SystemeAlertes { get; set; }

	public string I2Categorie { get; set; }

	public string I2Commentaires { get; set; }

	public sbyte? I2Paris1 { get; set; }

	public sbyte? I2Paris2 { get; set; }

	public sbyte? I2Paris3 { get; set; }

	public sbyte? I2Paris4 { get; set; }

	public sbyte? I2Paris5 { get; set; }

	public sbyte? I2Paris6 { get; set; }

	public sbyte? I2Paris7 { get; set; }

	public sbyte? I2Paris8 { get; set; }

	public sbyte? I2Paris9 { get; set; }

	public sbyte? I2Paris10 { get; set; }

	public sbyte? I2Paris11 { get; set; }

	public sbyte? I2Paris12 { get; set; }

	public sbyte? I2Paris13 { get; set; }

	public sbyte? I2Paris14 { get; set; }

	public sbyte? I2Paris15 { get; set; }

	public sbyte? I2Paris16 { get; set; }

	public sbyte? I2Paris17 { get; set; }

	public sbyte? I2Paris18 { get; set; }

	public sbyte? I2Paris19 { get; set; }

	public sbyte? I2Paris20 { get; set; }

	public sbyte? I2IssyLesMoulineaux { get; set; }

	public sbyte? I2Vanves { get; set; }

	public sbyte? I2Boulogne { get; set; }

	public sbyte? I2Levallois { get; set; }

	public sbyte? I2Neuilly { get; set; }

	public sbyte? I2Puteaux { get; set; }

	public sbyte? I2Courbevoie { get; set; }

	public sbyte? I2Vincennes { get; set; }

	public sbyte? I2SaintMande { get; set; }

	public sbyte? I2Charenton { get; set; }

	public string I2Standing { get; set; }

	public string I2TypeBien { get; set; }

	public int? I2VolumetrieAnnonces { get; set; }

	public int? I2NbDagencesSurZone { get; set; }

	public int? I2NbTotalDagences { get; set; }

	public string I2Icon { get; set; }

	public virtual ICollection<Biens> Biens { get; set; } = new List<Biens>();

	public virtual ICollection<Evenements> Evenements { get; set; } = new List<Evenements>();

	public virtual ICollection<IntermediairesDirects> IntermediairesDirects { get; set; } = new List<IntermediairesDirects>();
}
