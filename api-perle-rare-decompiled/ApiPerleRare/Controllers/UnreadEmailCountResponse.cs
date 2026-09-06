using System.Runtime.Serialization;

namespace ApiPerleRare.Controllers;

[DataContract]
public class UnreadEmailCountResponse
{
	[DataMember(EmitDefaultValue = false)]
	public int? Unread { get; set; }

	[DataMember(EmitDefaultValue = false)]
	public string Error { get; set; }
}
