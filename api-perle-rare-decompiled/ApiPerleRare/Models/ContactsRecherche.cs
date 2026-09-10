using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Models;

public class ContactsRecherche
{
	public class Localisation
	{
		public string[] CP { get; set; }
	}

	private Dictionary<string, object> _vals = new Dictionary<string, object>();

	public DateOnly CDateCreation { get; set; }

	public uint CRefContact { get; set; }

	public string CPrenom { get; set; }

	public string CNomFamille { get; set; }

	public string CAdresse { get; set; }

	public string CCodePostal { get; set; }

	public string CVille { get; set; }

	public string CPaysRegion { get; set; }

	public string CTitre { get; set; }

	public string CTelPersonnel1 { get; set; }

	public string CTelPersonnel2 { get; set; }

	public string CTelProfessionnel1 { get; set; }

	public string CTelProfessionnel2 { get; set; }

	public string CTelMobile1 { get; set; }

	public string CTelMobile2 { get; set; }

	public string CMel1 { get; set; }

	public string CMel2 { get; set; }

	public string CStatut { get; set; }

	public string CTypeRecherche { get; set; }

	public string CApporteur { get; set; }

	public string CPourcentageApp { get; set; }

	public string C2emeApporteur { get; set; }

	public string CNegociateur { get; set; }

	public string CPourcentageNeg { get; set; }

	public string C2emeNegociateur { get; set; }

	public string CRemarques { get; set; }

	public string CNomFamilleConseiller { get; set; }

	public string CPourcentageCons { get; set; }

	public string C2emeConseiller { get; set; }

	public string CCodeSupp1 { get; set; }

	public string CCodeSupp2 { get; set; }

	public string CCodeSupp3 { get; set; }

	public string CKeyWord1 { get; set; }

	public string CKeyWord2 { get; set; }

	public string CKeyWord3 { get; set; }

	public int? CNumeroMandat { get; set; }

	public string CMandat { get; set; }

	public string CQrecherche { get; set; }

	public int? CFraisPercus { get; set; }

	public int? CRemise { get; set; }

	public decimal? CMttHono { get; set; }

	public string CContratsSignes { get; set; }

	public DateOnly? CDate { get; set; }

	public DateOnly? CDateFin { get; set; }

	public string COrigine { get; set; }

	public string CConditionCom { get; set; }

	public string CRechercheCom { get; set; }

	public int CIdTypeMission { get; set; }

	public string CTypeTransaction { get; set; }

	public string CTypeBien { get; set; }

	public string CLocalisation { get; set; }

	public string CSurface { get; set; }

	public string CBudget { get; set; }

	public string CBudgetC { get; set; }

	public string CNbPieces { get; set; }

	public string CNbChambres { get; set; }

	public string CEtage { get; set; }

	public string CDernEtage { get; set; }

	public string CExclusivite { get; set; }

	public string CEvoPrix { get; set; }

	public string CAnciennete { get; set; }

	public string CTags { get; set; }

	public int? CRefFormulaire { get; set; }

	public string CRecherche { get; set; }

	public string CLibelleClient { get; set; }

	public int? CFactureHon { get; set; }

	public int? CFacturePs { get; set; }

	public string CAdresseBienFacture { get; set; }

	public sbyte CAlerteMail { get; set; }

	public virtual Formulaires CRefFormulaireNavigation { get; set; }

	public virtual ICollection<Evenements> Evenements { get; set; } = new List<Evenements>();

	public virtual ICollection<PropertyContact> PropertyContact { get; set; } = new List<PropertyContact>();

	public virtual ICollection<Taches> Taches { get; set; } = new List<Taches>();

	public bool IsMatch(Property property)
	{
		if (AreNotEmpty(CTypeTransaction, property.PTypeTransaction) && CTypeTransaction != property.PTypeTransaction)
		{
			return false;
		}
		if (AreNotEmpty(CTypeBien, property.PType) && !CTypeBien.Contains(property.PType))
		{
			return false;
		}
		if (AreNotEmpty(CLocalisation, property.PCp))
		{
			Localisation loc = Deserialize<Localisation>("Loc", CLocalisation);
			if (loc != null && !loc.CP.Contains(property.PCp))
			{
				return false;
			}
		}
		if (AreNotEmpty(CSurface, property.PSurface))
		{
			int[] range = Deserialize<int[]>("Surface", CSurface);
			if (range != null && range.Length == 3 && range[0] < range[1] && (property.PSurface < (double)range[0] || property.PSurface > (double)range[1]))
			{
				return false;
			}
		}
		if (AreNotEmpty(CBudget, property.PPrix))
		{
			int[] range2 = Deserialize<int[]>("Prix", CBudget);
			if (range2 != null && range2.Length == 3 && range2[0] < range2[1] && (property.PPrix < (double)range2[0] || property.PPrix > (double)range2[1]))
			{
				return false;
			}
		}
		if (AreNotEmpty(CBudgetC, property.PPrix, property.PSurface))
		{
			double ps = (double)(float)property.PPrix.Value / property.PSurface.Value;
			int[] range3 = Deserialize<int[]>("PrixM", CBudget);
			if (range3 != null && range3.Length == 3 && range3[0] < range3[1] && (ps < (double)range3[0] || ps > (double)range3[1]))
			{
				return false;
			}
		}
		if (AreNotEmpty(CNbPieces, property.PNbPieces))
		{
			byte[] nbs = Deserialize<byte[]>("Nbp", CNbPieces);
			if (nbs != null && nbs.Length != 0 && !nbs.Contains(property.PNbPieces.Value))
			{
				return false;
			}
		}
		if (AreNotEmpty(CNbChambres, property.PNbChambres))
		{
			byte[] nbc = Deserialize<byte[]>("Nbc", CNbChambres);
			if (nbc != null && nbc.Length != 0 && !nbc.Contains(property.PNbChambres.Value))
			{
				return false;
			}
		}
		return true;
	}

	private T Deserialize<T>(string key, string val)
	{
		if (!_vals.TryGetValue(key, out var res))
		{
			try
			{
				string json = PhpSerializer.ToJson(val);
				res = JsonSerializer.Deserialize<T>(json);
			}
			catch
			{
				res = null;
			}
			_vals.Add(key, res);
		}
		return (T)res;
	}

	private bool AreNotEmpty(params object[] vals)
	{
		foreach (object v in vals)
		{
			if (v == null)
			{
				return false;
			}
			if (v is string s && !string.IsNullOrEmpty(s))
			{
				return false;
			}
			if (v is float f && f == 0f)
			{
				return false;
			}
			if (v is int i2 && i2 == 0)
			{
				return false;
			}
		}
		return true;
	}
}
