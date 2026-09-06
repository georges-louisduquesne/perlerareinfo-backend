using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiPerleRare.Models;

public class AnnoncesGlobales
{
	public uint AgRef { get; set; }

	public uint AgRefAgc { get; set; }

	public string AgLien { get; set; }

	public string AgLienId { get; set; }

	public DateOnly AgDateDebut { get; set; }

	public DateTime? AgDateFin { get; set; }

	public string AgTypeTransaction { get; set; }

	public string AgType { get; set; }

	public string AgImage { get; set; }

	public uint? AgPrix { get; set; }

	public byte? AgNbPieces { get; set; }

	public ushort? AgSurface { get; set; }

	public string AgCp { get; set; }

	public byte? AgEtage { get; set; }

	public byte? AgNbChambres { get; set; }

	public string AgDesc { get; set; }

	public bool AgEstExclusif { get; set; }

	public bool AgEstDernierEtage { get; set; }

	public string AgAnnonceur { get; set; }

	public string AgTelAnnonceur { get; set; }

	public string AgQuartier { get; set; }

	public string AgQuartier2 { get; set; }

	public string AgListeTags { get; set; }

	public int AgRefMetaMoteur { get; set; }

	public string AgChampsVerrouilles { get; set; }

	public DateTime AgTime { get; set; }

	public bool AgAsc { get; set; }

	public short? AgAnnee { get; set; }

	public byte? AgNbEtages { get; set; }

	public bool? AgEstRecent { get; set; }

	public string AgAdresseNum { get; set; }

	public string AgAdresseRue { get; set; }

	public double? AgAdresseLat { get; set; }

	public double? AgAdresseLon { get; set; }

	public DateTime? AgTimeY { get; set; }

	public DateTime? AgTimeEndY { get; set; }

	public long? AgIdAgenceYanport { get; set; }

	public string AgIdAnnonceYanport { get; set; }

	public string AgIdPropertyYanport { get; set; }

	public string AgEmailAnnonceur { get; set; }

	public string AgTypeAnnonceur { get; set; }

	public string AgSsTypeAnnonceur { get; set; }

	public byte? AgEstOccupe { get; set; }

	public long? AgIdVille { get; set; }

	public long? AgIdQuartier { get; set; }

	public string AgConsumptionLetter { get; set; }

	public string AgGreenhouseGasConsumptionLetter { get; set; }

	public virtual MetaMoteurs AgRefMetaMoteurNavigation { get; set; }

	public virtual AnnoncesGlobalesVerifs AnnoncesGlobalesVerifs { get; set; }

	public virtual ICollection<HistoriqueAnnonces> HistoriqueAnnonces { get; set; } = new List<HistoriqueAnnonces>();

	[NotMapped]
	public PhotosAnnonces[] Photos { get; set; }
}
