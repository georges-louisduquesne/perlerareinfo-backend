using System;
using System.Collections.Generic;
using System.IO;
using ApiPerleRare;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiPerleRare.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
	public const string JwtSecret = "TEST-JWT-SECRET-DO-NOT-USE-IN-PROD-32";

	public string UploadRoot { get; } = Path.Combine(Path.GetTempPath(), "pr-api-tests-upload");

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		Environment.SetEnvironmentVariable("PR_LOCAL_SAFE", "1");
		Directory.CreateDirectory(UploadRoot);

		builder.UseEnvironment("Development");
		builder.ConfigureAppConfiguration((_, config) =>
		{
			config.AddInMemoryCollection(new Dictionary<string, string>
			{
				["AppSettings:Secret"] = JwtSecret,
				["AppSettings:OldWebSiteFolder"] = UploadRoot,
				["AppSettings:YanportToken"] = "",
				["ConnectionStrings:PerleRareDB"] = "Server=127.0.0.1;Database=perle-rareinfo;Uid=test;Pwd=test;Port=3306;",
				["ConnectionStrings:Exchange"] = "",
				["AllowedHosts"] = "*"
			});
		});
		builder.ConfigureTestServices(services =>
		{
			services.RemoveAll<IUserService>();
			services.AddSingleton<IUserService, FakeUserService>();
		});
	}
}
