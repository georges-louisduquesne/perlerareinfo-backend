using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiPerleRare.Models;

public class FormulairesCandidats
{
	public int FcRef { get; set; }

	public string FcMessageId { get; set; }

	public DateTime FcDate { get; set; }

	public string FcFrom { get; set; }

	public string FcSubject { get; set; }

	public string FcPrenom { get; set; }

	public string FcNom { get; set; }

	public string FcTel { get; set; }

	public string FcEmail { get; set; }

	public string FcVille { get; set; }

	public string FcLinkedin { get; set; }

	public string FcPourquoi { get; set; }

	public string FcDelai { get; set; }

	public string FcCorps { get; set; }

	/// <summary>1 à attribuer, 2 attribué, 4 rejeté.</summary>
	public short FcStatut { get; set; }

	public string FcCauseRejet { get; set; }

	public uint? FcRefAdmin { get; set; }

	public uint? FcRefConseiller { get; set; }

	public DateTime? FcDateTraitement { get; set; }

	[NotMapped]
	public FormulaireCandidatCorrespondance[] Correspondances { get; set; }
}

public class FormulaireCandidatCorrespondance
{
	public uint? CRefContact { get; set; }

	public uint? CRefCtcInter { get; set; }

	public uint? CRefConseiller { get; set; }

	public string Type { get; set; }

	public string Nom { get; set; }

	public string Prenom { get; set; }

	public string NomFamille { get; set; }

	public DateTime? DateCreation { get; set; }

	public bool NomIdentique { get; set; }

	public bool EmailIdentique { get; set; }

	public bool TelIdentique { get; set; }
}
