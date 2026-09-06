using ApiPerleRare.Models;

namespace ApiPerleRare.Controllers;

public class TachesEx : Taches, ITachesEx
{
	public string TQui_PS { get; set; }

	public string CNegociateur { get; set; }

	public string CNegociateur_PS { get; set; }
}
