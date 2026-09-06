using ApiPerleRare.Models;

namespace ApiPerleRare.Controllers;

public class Negociateur : ConseillersPersonnels
{
	public int NbFormsEnRetard { get; set; }

	public int NbFormsEnRetard30 { get; set; }

	public int NbFormsDuJour { get; set; }

	public int NbTachesEnRetard { get; set; }

	public int NbTachesDuJour { get; set; }
}
