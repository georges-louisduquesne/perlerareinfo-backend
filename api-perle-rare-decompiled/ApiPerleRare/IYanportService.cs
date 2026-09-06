namespace ApiPerleRare;

public interface IYanportService
{
	bool IsConfigure { get; }

	bool DevMode { get; }

	string Get(int requestId, string url);
}
