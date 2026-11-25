using System;

namespace ApiPerleRare.Application.Events;

public sealed class ConseillerMissionRow
{
	public int Id { get; set; }

	public DateTime Date { get; set; }

	public string Type { get; set; }

	public string Texte { get; set; }

	public uint? ContactId { get; set; }

	public string ContactNom { get; set; }

	public string ContactPrenom { get; set; }

	public string ContactStatut { get; set; }

	public string Cp { get; set; }

	public string Adresse { get; set; }

	public string[] Roles { get; set; }
}
