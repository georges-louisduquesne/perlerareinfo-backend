namespace ApiPerleRare;

public class MailAttachment
{
	public string FileName { get; }

	public byte[] Content { get; }

	public bool IsInLine { get; set; }

	public string ContentId { get; set; }

	public MailAttachment(string fileName, byte[] content)
	{
		FileName = fileName;
		Content = content;
	}
}
