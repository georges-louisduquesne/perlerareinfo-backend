namespace ApiPerleRare.Application.Files;

public interface IUploadFileUseCase
{
	FileResponse Execute(FileUploadRequest request);
}
