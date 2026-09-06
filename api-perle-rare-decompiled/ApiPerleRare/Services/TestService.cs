using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace ApiPerleRare.Services;

public class TestService : ITestService
{
	private readonly IConfiguration _configuration;

	public TestService(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public string GetInfo()
	{
		string cs = _configuration.GetConnectionString("PerleRareDB");
		string dbName = Regex.Match(cs, "Database=(?<name>[^;]+)").Groups["name"].Value;
		return "DBName=" + dbName;
	}
}
