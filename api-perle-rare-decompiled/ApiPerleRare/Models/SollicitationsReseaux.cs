using System;

namespace ApiPerleRare.Models;

public class SollicitationsReseaux
{
	public uint SrId { get; set; }

	public string SrPostId { get; set; }

	public string SrReseau { get; set; }

	public uint SrRefConseiller { get; set; }

	public DateTime SrDatePublication { get; set; }

	public DateTime SrDateEnvoi { get; set; }

	public string SrType { get; set; }
}
