using System.Runtime.Serialization;

namespace ApiPerleRare.Controllers;

[DataContract]
public class DeleteAppointmentFromERefEvenementResponse
{
	[DataMember]
	public string Error { get; set; }

	[DataMember]
	public bool IsDeleted { get; set; }
}
