using System;

namespace ApiPerleRare.Models;

public class AnnoncesGlobalesVerifs
{
	public uint AgvRef { get; set; }

	public DateOnly AgvDate { get; set; }

	public string AgvStatut { get; set; }

	public virtual AnnoncesGlobales AgvRefNavigation { get; set; }
}
