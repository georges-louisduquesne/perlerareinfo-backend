using System;

namespace ApiPerleRare.Models;

public class PublicationsReseaux
{
	public uint PubId { get; set; }

	public string PubPostId { get; set; }

	public string PubReseau { get; set; }

	public string PubUrl { get; set; }

	public DateTime PubDatePublication { get; set; }

	public string PubVignette { get; set; }

	public string PubTexte { get; set; }

	public DateTime PubMaj { get; set; }
}
