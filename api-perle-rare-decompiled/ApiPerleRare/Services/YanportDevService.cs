using System.IO;
using System.Text;
using System.Text.Json;
using ApiPerleRare.YanportModels;
using Microsoft.Extensions.Configuration;

namespace ApiPerleRare.Services;

public class YanportDevService : IYanportService
{
	private readonly YanportService _originalYanportService;

	public bool IsConfigure => _originalYanportService.IsConfigure;

	public bool DevMode => true;

	public YanportDevService(IConfiguration configuration)
	{
		_originalYanportService = new YanportService(configuration);
	}

	public string Get(int requestId, string url)
	{
		string path = Path.Combine("C:\\Temp", $"yanport-{requestId}.json");
		if (File.Exists(path))
		{
			string json = File.ReadAllText(path, Encoding.UTF8);
			PropertiesResponse res = JsonSerializer.Deserialize<PropertiesResponse>(json, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});
			return JsonSerializer.Serialize(res);
		}
		string json2 = _originalYanportService.Get(requestId, url);
		File.WriteAllText(path, json2, Encoding.UTF8);
		return json2;
	}
}
