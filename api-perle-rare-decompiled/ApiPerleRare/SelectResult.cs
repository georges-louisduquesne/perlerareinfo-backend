namespace ApiPerleRare;

public class SelectResult
{
	public string Description { get; set; }

	public int Id { get; set; }

	public string Url { get; set; }

	public string Category { get; set; }

	public string CategoryId { get; set; }

	public string Color { get; set; }

	public string Url2 { get; set; }

	/// <summary>Agence, agent, prospect, client, réseau, conseiller ou candidat inactif.</summary>
	public bool Inactive { get; set; }
}
