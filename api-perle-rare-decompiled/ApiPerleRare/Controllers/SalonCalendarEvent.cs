using System;

namespace ApiPerleRare.Controllers;

public class SalonCalendarEvent
{
	public string Room { get; set; }

	public string Subject { get; set; }

	public DateTime Start { get; set; }

	public DateTime End { get; set; }

	public string OrganizerEmail { get; set; }

	public string OrganizerName { get; set; }
}
