using System;

namespace ApiPerleRare.Models;

public class ScheduledTaskLogs
{
	public uint StlId { get; set; }

	public string StlNom { get; set; }

	public DateTime StlDateDebut { get; set; }

	public DateTime? StlDateFin { get; set; }

	public string StlResults { get; set; }
}
