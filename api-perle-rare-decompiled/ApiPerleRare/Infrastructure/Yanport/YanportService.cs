using System;
using System.IO;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace ApiPerleRare.Infrastructure.Yanport;

public class YanportService : IYanportService
{
	private readonly string _token;

	public bool IsConfigure => !string.IsNullOrWhiteSpace(_token);

	public bool DevMode => false;

	public YanportService(IConfiguration configuration)
	{
		_token = configuration.GetSection("AppSettings")["YanportToken"];
	}

	public string Get(int requestId, string url)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_token))
			{
				return null;
			}
			HttpWebRequest request = WebRequest.CreateHttp(url);
			request.Headers[HttpRequestHeader.ContentType] = "application/json";
			request.Headers[HttpRequestHeader.Authorization] = "Bearer " + _token;
			request.Method = "GET";
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			using StreamReader sr = new StreamReader(response.GetResponseStream());
			return sr.ReadToEnd();
		}
		catch (WebException ex)
		{
			HttpWebResponse hwr = (HttpWebResponse)ex.Response;
			if (hwr.StatusCode == HttpStatusCode.NotFound)
			{
				return YanportServiceConstant.NotFound;
			}
			if (hwr.StatusCode == HttpStatusCode.InternalServerError)
			{
				return YanportServiceConstant.Error500;
			}
			throw new Exception("Erreur YanPort sur l'url '" + url + "'", ex);
		}
		catch (Exception innerException)
		{
			throw new Exception("Erreur YanPort sur l'url '" + url + "'", innerException);
		}
	}
}
