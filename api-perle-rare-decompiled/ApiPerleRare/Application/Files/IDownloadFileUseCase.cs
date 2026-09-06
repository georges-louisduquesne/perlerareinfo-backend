namespace ApiPerleRare.Application.Files;

public interface IDownloadFileUseCase
{
	FileDownloadResponse Execute(FileDownloadRequest request);
}
