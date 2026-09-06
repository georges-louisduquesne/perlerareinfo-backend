using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;

namespace ApiPerleRare.Services;

internal class WebSiteService : IWebSiteService
{
	private readonly string _siteUrl;

	public WebSiteService()
	{
		_siteUrl = "https://old.perle-rare.info/";
		ServicePointManager.Expect100Continue = true;
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
	}

	public void SendRequest(string url, string method = "GET", object post = null)
	{
		using (InternalSendRequest(url, method, post))
		{
		}
	}

	protected byte[] SendRequestAndGetResponse(string url, string method = "GET", object post = null)
	{
		using HttpWebResponse response = InternalSendRequest(url, method, post);
		using MemoryStream ms = new MemoryStream();
		if (response.StatusCode != HttpStatusCode.OK)
		{
			throw new Exception("Echec de la requête HTTP '" + url + "' : " + response.StatusCode);
		}
		response.GetResponseStream().CopyTo(ms);
		return ms.ToArray();
	}

	private HttpWebResponse InternalSendRequest(string url, string method, object post = null)
	{
		try
		{
			url = url.TrimStart('/', '\\');
			HttpWebRequest request = WebRequest.CreateHttp(_siteUrl + url);
			request.Method = method;
			request.ContentType = "application/json";
			if (post != null)
			{
				string st = JsonConvert.SerializeObject(post);
				using StreamWriter sw = new StreamWriter(request.GetRequestStream(), Encoding.UTF8);
				sw.Write(st);
			}
			return (HttpWebResponse)request.GetResponse();
		}
		catch (WebException ex)
		{
			HttpWebResponse hwr = (HttpWebResponse)ex.Response;
			throw new Exception(hwr.StatusCode.ToString() + " : " + hwr.StatusDescription);
		}
	}

	public T SendRequest<T>(string url, string method = "GET", object post = null)
	{
		string json = SendRequestAndGetString(url, method, post);
		return JsonConvert.DeserializeObject<T>(json);
	}

	public string SendRequestAndGetString(string url, string method = "GET", object post = null)
	{
		using HttpWebResponse response = InternalSendRequest(url, method, post);
		StringBuilder sb = new StringBuilder();
		using MemoryStream ms = new MemoryStream();
		using Stream stream = response.GetResponseStream();
		stream.CopyTo(ms);
		Encoding encoding = Encoding.GetEncoding(response.CharacterSet);
		return encoding.GetString(ms.ToArray());
	}
}
