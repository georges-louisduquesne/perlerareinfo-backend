using System;
using System.Runtime.Serialization;

namespace ApiPerleRare;

[DataContract]
public class RendezVous
{
	[DataMember]
	public DateTime Start { get; set; }

	[DataMember]
	public DateTime ReminderDueBy { get; set; }

	[DataMember]
	public string Subject { get; set; }

	[DataMember]
	public string HtmlBody { get; set; }

	[DataMember]
	public DateTime End { get; set; }

	[DataMember]
	public string Location { get; set; }

	[DataMember]
	public string[] RequiredAttendees { get; set; }

	[DataMember]
	public string[] OptionalAttendees { get; set; }

	[DataMember]
	public int ERefEvenement { get; set; }

	[DataMember]
	public int ReminderMinutesBeforeStart { get; set; }
}
