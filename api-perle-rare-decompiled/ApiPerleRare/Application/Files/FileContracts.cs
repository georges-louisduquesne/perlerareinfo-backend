namespace ApiPerleRare.Application.Files;

/// <summary>
/// HTTP file payloads. Property names are the frozen Angular contract (camelCase JSON).
/// </summary>
public class FileUploadRequest
{
	public string FileName { get; set; }

	public string FileContent { get; set; }
}

public class FileResponse
{
	public bool IsSuccess { get; set; }

	public string Errors { get; set; }

	public string FullPath { get; set; }
}

public class FileDownloadRequest
{
	public string FileName { get; set; }
}

public class FileDownloadResponse : FileResponse
{
	public string FileContent { get; set; }
}
