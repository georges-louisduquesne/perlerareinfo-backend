using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ApiPerleRare.Controllers;

[DataContract]
public class ExchangeAppointmentItem
{
	[DataMember]
	public string Id { get; set; }

	[DataMember]
	public string Subject { get; set; }

	[DataMember]
	public DateTime Start { get; set; }

	[DataMember]
	public DateTime End { get; set; }

	[DataMember]
	public string Location { get; set; }

	[DataMember]
	public string LegacyFreeBusyStatus { get; set; }
}

[DataContract]
public class AppointmentsResponse
{
	[DataMember]
	public string Error { get; set; }

	[DataMember]
	public List<ExchangeAppointmentItem> Appointments { get; set; }
}
