using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApiPerleRare.Models;

public class Property
{
	public string PPropertyId { get; set; }

	public string PDoublonPropertyId { get; set; }

	public string PImages { get; set; }

	public string PAds { get; set; }

	public string PType { get; set; }

	public string PAnnonceurs { get; set; }

	public string PDescription { get; set; }

	public string PQuartier { get; set; }

	public string PQuartier2 { get; set; }

	public string PListeTags { get; set; }

	public string PAdresseNum { get; set; }

	public string PAdresseRue { get; set; }

	public string PCp { get; set; }

	public string PTypeTransaction { get; set; }

	public bool? PAsc { get; set; }

	public double? PPrix { get; set; }

	public string PPrixHistorique { get; set; }

	public sbyte? PPrixEvol { get; set; }

	public byte? PNbChambres { get; set; }

	public byte? PNbPieces { get; set; }

	public byte? PNbEtages { get; set; }

	public double? PSurface { get; set; }

	public byte? PEtage { get; set; }

	public bool? PEstExclusif { get; set; }

	public bool? PEstDernierEtage { get; set; }

	public bool? PEstRecent { get; set; }

	public bool? PEstOccupe { get; set; }

	public int? PAnnee { get; set; }

	public DateTime? PDateDebut { get; set; }

	public DateTime? PDateFin { get; set; }

	public double? PAdresseLat { get; set; }

	public double? PAdresseLon { get; set; }

	public long? PIdVille { get; set; }

	public long? PIdQuartier { get; set; }

	public string PConsumptionLetter { get; set; }

	public string PGreenhouseGasConsumptionLetter { get; set; }

	public DateTime? PDateCreation { get; set; }

	public DateTime? PDateLastUpdate { get; set; }

	public uint PState { get; set; }

	[JsonIgnore]
	public virtual ICollection<PropertyContact> PropertyContact { get; set; } = new List<PropertyContact>();

	[JsonIgnore]
	public virtual ICollection<ContactIntermediaire> CRefCtcInter { get; set; } = new List<ContactIntermediaire>();

	[JsonIgnore]
	public virtual ICollection<IntermediairesDirects> IRefIntermediaire { get; set; } = new List<IntermediairesDirects>();
}
