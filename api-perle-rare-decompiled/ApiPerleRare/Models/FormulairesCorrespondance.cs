namespace ApiPerleRare.Models;

public class FormulairesCorrespondance
{
	public uint IRefIntermediaire { get; set; }

	public uint CRefCtcInter { get; set; }

	public uint CRefContact { get; set; }

	public string Type { get; set; }

	public bool Actif { get; set; }

	public string Nom { get; set; }

	public bool NomIdentique { get; set; }

	public bool EmailIdentique { get; set; }

	public bool TelIdentique { get; set; }

	internal string NomFamille { get; set; }

	internal string Mel1 { get; set; }

	internal string Telephone1 { get; set; }

	internal string Telephone2 { get; set; }

	internal string Mel2 { get; set; }

	internal string Mobile1 { get; set; }

	internal string Mobile2 { get; set; }

	internal string Pro1 { get; set; }

	internal string Pro2 { get; set; }

	internal FormulairesCorrespondance Clone()
	{
		return new FormulairesCorrespondance
		{
			IRefIntermediaire = IRefIntermediaire,
			CRefContact = CRefContact,
			CRefCtcInter = CRefCtcInter,
			Type = Type,
			Actif = Actif,
			Nom = Nom,
			NomIdentique = NomIdentique,
			EmailIdentique = EmailIdentique,
			TelIdentique = TelIdentique
		};
	}
}
