using System;
using System.Collections.Generic;

namespace ApiPerleRare.Models;

public class ConseillersPersonnels
{
	public uint CpRefConseiller { get; set; }

	public string CpPrenom { get; set; }

	public string CpNomFamille { get; set; }

	public string CpTitre { get; set; }

	public string CpMel { get; set; }

	public byte[] CpPhotographie { get; set; }

	public string CpTelPersonnel { get; set; }

	public sbyte? CpActif { get; set; }

	public string CpStatut { get; set; }

	public string CpLogin { get; set; }

	public string CpMotDePasse { get; set; }

	public bool CpAdmin { get; set; }

	public bool CpNegociateur { get; set; }

	public string CpAccueilPos { get; set; }

	public string CpMandat { get; set; }

	public string CpAdresse { get; set; }

	public int CpCp { get; set; }

	public string CpVille { get; set; }

	public DateOnly? CpDateNaissance { get; set; }

	public string CpPieceIdentite { get; set; }

	public string CpMelPerso { get; set; }

	public string CpPhoto { get; set; }

	public DateOnly CpDateDebut { get; set; }

	public DateOnly CpDateFin { get; set; }

	public DateOnly? CpDateSignatureContrat { get; set; }

	public DateOnly? CpDateContrat { get; set; }

	public bool? CpPortage { get; set; }

	public bool CpEnSociete { get; set; }

	public long CpSiret { get; set; }

	public string CpRsac { get; set; }

	public bool CpTva { get; set; }

	public string CpFonction { get; set; }

	public DateOnly? CpDateCreation { get; set; }

	public string CpLibelle { get; set; }

	public string CpCommentaire { get; set; }

	public string CpCv { get; set; }

	public string CpNumrsac { get; set; }

	public string CpMelMotDePasse { get; set; }

	public string CpMelSignature { get; set; }

	public sbyte CpDispo { get; set; }

	public string CpAutoLogin { get; set; }

	public string CpPhotoSignature { get; set; }

	public string CpInitiales { get; set; }

	public virtual ICollection<ConseillersPersonnelsEvenements> ConseillersPersonnelsEvenements { get; set; } = new List<ConseillersPersonnelsEvenements>();

	public virtual ICollection<ConseillersPersonnelsTaches> ConseillersPersonnelsTaches { get; set; } = new List<ConseillersPersonnelsTaches>();

	public virtual ICollection<Evenements> Evenements { get; set; } = new List<Evenements>();

	public virtual ICollection<Formulaires> Formulaires { get; set; } = new List<Formulaires>();
}
