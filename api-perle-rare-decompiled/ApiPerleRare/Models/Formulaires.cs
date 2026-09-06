using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiPerleRare.Models;

public class Formulaires
{
	public int FRef { get; set; }

	public DateTime FDate { get; set; }

	public byte FType { get; set; }

	public short FStatut { get; set; }

	public string FNom { get; set; }

	public string FPrenom { get; set; }

	public string FSociete { get; set; }

	public string FAdresse { get; set; }

	public string FCp { get; set; }

	public string FVille { get; set; }

	public string FEmail { get; set; }

	public string FTel { get; set; }

	public string FSouhaits { get; set; }

	public string FTexte { get; set; }

	public uint? FRefNegociateur { get; set; }

	public string FFichierContenu { get; set; }

	public string FUtmSource { get; set; }

	public string FUtmMedium { get; set; }

	public string FUtmCampaign { get; set; }

	public string FUtmTerm { get; set; }

	public string FUtmContent { get; set; }

	public string FIpAddress { get; set; }

	public string FVariant { get; set; }

	public string FDateRdv { get; set; }

	public virtual ICollection<ContactsRecherche> ContactsRecherche { get; set; } = new List<ContactsRecherche>();

	public virtual ConseillersPersonnels FRefNegociateurNavigation { get; set; }

	[NotMapped]
	public FormulairesCorrespondance[] Correspondances { get; set; }
}
