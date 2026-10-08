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
	public string SenderPassword { get; set; }

	[DataMember]
	public string To { get; set; }

	[DataMember]
	public string Subject { get; set; }

	[DataMember]
	public string HtmlContents { get; set; }

	[DataMember]
	public bool HighImportance { get; set; }

	[DataMember]
	public string Cc { get; set; }

	[DataMember]
	public EmailAttachment[] Attachments { get; set; }
}

[DataContract]
public class EmailAttachment
{
	[DataMember]
	public string FileName { get; set; }

	[DataMember]
	public string ContentType { get; set; }

	[DataMember]
	public string ContentBase64 { get; set; }
}
