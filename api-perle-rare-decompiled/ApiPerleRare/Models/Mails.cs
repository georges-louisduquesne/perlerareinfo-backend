using System;

namespace ApiPerleRare.Models;

public class Mails
{
	public uint Id { get; set; }

	public string Name { get; set; }

	public DateTime Createdon { get; set; }

	public DateTime Updatedon { get; set; }

	public string Subject { get; set; }

	public string Contents { get; set; }

	public DateOnly? Startdate { get; set; }

	public DateOnly? EndDate { get; set; }

	public uint State { get; set; }

	public uint? Createdby { get; set; }

	public uint? Updatedby { get; set; }

	public uint? Template1 { get; set; }

	public uint? Template2 { get; set; }

	public uint? Template3 { get; set; }

	public string Pjmanuel1 { get; set; }

	public string Pjmanuel2 { get; set; }

	public string Pjmanuel3 { get; set; }

	public uint? Refcontact { get; set; }

	public uint? Envoyeur { get; set; }

	public string Error { get; set; }

	public string Cc { get; set; }

	public string To { get; set; }

	public string Cci { get; set; }

	public byte? Importancehaute { get; set; }

	public uint? Template31 { get; set; }

	public DateTime? Senton { get; set; }

	public string Contact { get; set; }

	public string Modele { get; set; }
}
