using System;
using System.Runtime.Serialization;

namespace ApiPerleRare.Controllers;

[DataContract]
public class TodayAppointmentsResponse
{
	[DataMember]
	public string Error { get; set; }

	[DataMember]
	public int Count { get; set; }

	[DataMember]
	public DateTime? NextDate { get; set; }

	[DataMember]
	public string NextSubject { get; set; }
}
