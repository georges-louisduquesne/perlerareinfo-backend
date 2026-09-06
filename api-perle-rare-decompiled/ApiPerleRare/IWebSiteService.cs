namespace ApiPerleRare;

public interface IWebSiteService
{
	T SendRequest<T>(string url, string method = "GET", object post = null);

	void SendRequest(string url, string method = "GET", object post = null);

	string SendRequestAndGetString(string url, string method = "GET", object post = null);
}
