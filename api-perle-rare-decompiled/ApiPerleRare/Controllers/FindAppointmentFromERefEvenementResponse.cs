using System.Runtime.Serialization;

namespace ApiPerleRare.Controllers;

[DataContract]
public class FindAppointmentFromERefEvenementResponse
{
	[DataMember]
	public string Error { get; set; }

	[DataMember]
	public RendezVous Appointment { get; set; }

	[DataMember]
	public string OrganizerEmail { get; set; }
}
