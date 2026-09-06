using System;

namespace ApiPerleRare.Models;

public class Templates
{
	public uint Id { get; set; }

	public string Name { get; set; }

	public DateTime Createdon { get; set; }

	public DateTime Updatedon { get; set; }

	public string File { get; set; }

	public uint State { get; set; }

	public uint? Createdby { get; set; }

	public uint? Updatedby { get; set; }

	public DateTime? Testedon { get; set; }

	public string Error { get; set; }

	public string Nompj { get; set; }

	public string ContactStatut { get; set; }

	public string TypeTransaction { get; set; }
}
