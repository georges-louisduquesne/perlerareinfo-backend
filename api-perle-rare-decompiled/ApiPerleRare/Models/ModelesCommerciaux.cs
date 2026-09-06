using System;

namespace ApiPerleRare.Models;

public class ModelesCommerciaux
{
	public int McRef { get; set; }

	public short? McCategorie { get; set; }

	public string McTitre { get; set; }

	public string McTitreAbr { get; set; }

	public byte[] McFichierDoc { get; set; }

	public DateOnly? McFichierDocMaj { get; set; }

	public byte[] McFichierPdf { get; set; }

	public DateOnly? McFichierPdfMaj { get; set; }

	public virtual TypesModeles McCategorieNavigation { get; set; }
}
