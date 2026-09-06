using System.Runtime.Serialization;

namespace ApiPerleRare.Controllers;

[DataContract]
public class Email
{
	[DataMember]
	public string SenderEmail { get; set; }

	[DataMember]
	public string SenderName { get; set; }

	[DataMember]
	public string To { get; set; }

	[DataMember]
	public string Subject { get; set; }

	[DataMember]
	public string HtmlContents { get; set; }
}
