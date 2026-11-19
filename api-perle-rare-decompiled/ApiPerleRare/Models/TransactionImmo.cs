using System;

namespace ApiPerleRare.Models;

public class TransactionImmo
{
	public ulong TRef { get; set; }

	public DateTime TDateTransaction { get; set; }

	public string TNatureMutation { get; set; }

	public decimal TPrix { get; set; }

	public string TAdresseNum { get; set; }

	public string TAdresseSuffixe { get; set; }

	public string TAdresseRue { get; set; }

	public string TAdresseRueNorm { get; set; }

	public string TCp { get; set; }

	public string TCommune { get; set; }

	public string TCodeDepartement { get; set; }

	public string TTypeBien { get; set; }

	public byte? TNbPieces { get; set; }

	public decimal? TSurfaceBatie { get; set; }

	public decimal? TPrixM2 { get; set; }

	public decimal? TSurfaceCarrez { get; set; }

	public ushort? TNbLots { get; set; }

	public string TLotsJson { get; set; }

	public byte TNbDependances { get; set; }

	public decimal? TSurfaceTerrain { get; set; }

	public string TNatureCulture { get; set; }

	public string TIdMutation { get; set; }

	public string TFingerprint { get; set; }

	public string TIdParcelle { get; set; }

	public string TCodeCommuneInsee { get; set; }

	public string TCodeVoieFantoir { get; set; }

	public double? TLat { get; set; }

	public double? TLon { get; set; }

	public byte? TNbChambres { get; set; }

	public short? TEtage { get; set; }

	public decimal? TSurfaceNormee { get; set; }

	public byte? TNbEtages { get; set; }

	public bool? TAscenseur { get; set; }

	public bool? TDernierEtage { get; set; }

	public string TDpe { get; set; }

	public string TRemarques { get; set; }

	public string TPropertyId { get; set; }

	public string TPhotoUrl { get; set; }

	public string TSource { get; set; }

	public DateTime TDateImport { get; set; }

	public DateTime? TDateMaj { get; set; }
}
