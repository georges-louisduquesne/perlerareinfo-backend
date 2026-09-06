using System;

namespace ApiPerleRare.Models;

public class AnnoncesRefcontact
{
	public ulong ARefAnn { get; set; }

	public uint ARefAnnGlob { get; set; }

	public sbyte AActif { get; set; }

	public string ASource { get; set; }

	public string AAnnonceur { get; set; }

	public string ATelAnnonceur { get; set; }

	public DateTime ADateAspi { get; set; }

	public DateTime ADateAff { get; set; }

	public int APos { get; set; }

	public string AImage { get; set; }

	public string APrix { get; set; }

	public string AVille { get; set; }

	public string AType { get; set; }

	public byte? ANbPieces { get; set; }

	public uint ASurf { get; set; }

	public string AEtage { get; set; }

	public string ANbChambres { get; set; }

	public string ADesc { get; set; }

	public string ALien { get; set; }

	public string ACom { get; set; }

	public uint ARate { get; set; }

	public uint ADedouble { get; set; }

	public bool AEstExclusif { get; set; }

	public bool AEstDernierEtage { get; set; }
}
