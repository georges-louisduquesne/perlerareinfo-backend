namespace ApiPerleRare.Application.Annuaire;

public sealed class AnnuairePartenaireRow
{
	public string Id { get; set; }

	public string Categorie { get; set; }

	public string Nom { get; set; }

	public string Societe { get; set; }

	public string Site { get; set; }

	public string Telephone { get; set; }

	public string Email { get; set; }

	public string Adresse { get; set; }

	public string TelephoneFixe { get; set; }
}

public sealed class AnnuairePartenairesDocument
{
	public bool Configured { get; set; }

	public System.Collections.Generic.List<AnnuairePartenaireRow> Items { get; set; }
}

public sealed class AnnuairePartenairesWrite
{
	public System.Collections.Generic.List<AnnuairePartenaireRow> Items { get; set; }
}
