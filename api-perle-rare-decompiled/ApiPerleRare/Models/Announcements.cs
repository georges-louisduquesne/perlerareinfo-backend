using System;

namespace ApiPerleRare.Models;

public class Announcements
{
	public int Id { get; set; }

	public string Message { get; set; }

	public DateTime VisibleFrom { get; set; }

	public DateTime VisibleTo { get; set; }

	public int CreatedBy { get; set; }
}
