using System.Runtime.Serialization;

namespace ApiPerleRare.Controllers;

[DataContract]
public class AddAppointmentResponse
{
	[DataMember]
	public string Error { get; set; }

	[DataMember]
	public string Id { get; set; }

	[DataMember]
	public bool Updated { get; set; }
}
