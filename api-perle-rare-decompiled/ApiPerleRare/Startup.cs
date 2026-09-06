using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using ApiPerleRare.Application.Authentication;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using ApiPerleRare.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace ApiPerleRare;

public class Startup
{
	public IConfiguration Configuration { get; }

	public static bool IsLocalSafe => string.Equals(Environment.GetEnvironmentVariable("PR_LOCAL_SAFE"), "1", StringComparison.Ordinal);

	public static bool IsDevMachine => IsLocalSafe || Environment.UserName == "jbhuber" || Environment.MachineName == "PORT0623001";

	public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}

	public void ConfigureServices(IServiceCollection services)
	{
		string connectionString = Configuration.GetConnectionString("PerleRareDB");
		services.AddDbContext<ApplicationDbContext>(delegate(DbContextOptionsBuilder opt)
		{
			var serverVersion = IsLocalSafe
				? ServerVersion.Parse("10.5.0-mariadb")
				: ServerVersion.AutoDetect(connectionString);
			opt.UseMySql(connectionString, serverVersion, delegate(MySqlDbContextOptionsBuilder o)
			{
				o.CommandTimeout(180);
			});
		});
		services.AddCors(delegate(CorsOptions options)
		{
			options.AddDefaultPolicy(delegate(CorsPolicyBuilder builder)
			{
				var origins = new List<string>
				{
					"https://dev.perle-rare.info",
					"https://perle-rare.info",
					"https://warm-canverns-48629-92fab798385f.herokuapp.com",
					"https://prinfo.flutterflow.app"
				};
				if (IsLocalSafe)
				{
					origins.Add("http://localhost:4200");
					origins.Add("http://127.0.0.1:4200");
				}
				builder.WithOrigins(origins.ToArray()).AllowCredentials().AllowAnyHeader()
					.AllowAnyMethod();
			});
		});
		services.AddControllers().AddJsonOptions(delegate(JsonOptions opt)
		{
			opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			opt.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
			opt.JsonSerializerOptions.DefaultIgnoreCondition = (JsonIgnoreCondition)3;
		});
		services.AddSwaggerGen(delegate(SwaggerGenOptions c)
		{
			c.SwaggerDoc("v1", new OpenApiInfo
			{
				Title = "Api.Data.Perle-rare.info",
				Version = "v1"
			});
			c.SchemaFilter<CustoSchemaFilter>(Array.Empty<object>());
			c.OperationFilter<CustoSchemaFilter>(Array.Empty<object>());
			c.DocumentFilter<CustoSchemaFilter>(Array.Empty<object>());
		});
		IConfigurationSection appSettingsSection = Configuration.GetSection("AppSettings");
		services.Configure<AppSettings>(appSettingsSection);
		AppSettings appSettings = appSettingsSection.Get<AppSettings>();
		byte[] key = Encoding.ASCII.GetBytes(appSettings.Secret);
		services.AddAuthentication(delegate(AuthenticationOptions x)
		{
			x.DefaultAuthenticateScheme = "Bearer";
			x.DefaultChallengeScheme = "Bearer";
		}).AddJwtBearer(delegate(JwtBearerOptions x)
		{
			x.RequireHttpsMetadata = false;
			x.SaveToken = true;
			x.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = false,
				ValidateAudience = false,
				ClockSkew = TimeSpan.Zero
			};
		});
		services.AddControllersWithViews();
		services.AddAuthorization(delegate(AuthorizationOptions options)
		{
			options.AddPolicy("Admin", delegate(AuthorizationPolicyBuilder policy)
			{
				policy.RequireClaim("Admin");
			});
			options.AddPolicy("GetAll", delegate(AuthorizationPolicyBuilder policy)
			{
				policy.RequireClaim("GetAll");
			});
			options.AddPolicy("PostAll", delegate(AuthorizationPolicyBuilder policy)
			{
				policy.RequireClaim("PostAll");
			});
			options.AddPolicy("PutAll", delegate(AuthorizationPolicyBuilder policy)
			{
				policy.RequireClaim("PutAll");
			});
			options.AddPolicy("DeleteAll", delegate(AuthorizationPolicyBuilder policy)
			{
				policy.RequireClaim("DeleteAll");
			});
		});
		services.AddHttpContextAccessor();
		services.AddScoped<IUserService, UserService>();
		services.AddScoped<IAuthenticateUseCase, AuthenticateUseCase>();
		services.AddScoped<ITestService, TestService>();
		services.AddScoped<ISearchService, SearchService>();
		services.AddScoped<IAuditService, AuditService>();
		services.AddScoped<IExchangeService, PRExchangeService>();
		services.AddScoped<IUserSessionService, UserSessionService>();
		services.AddDistributedMemoryCache();
		services.AddSession(delegate(SessionOptions options)
		{
			options.IdleTimeout = TimeSpan.FromDays(1.0);
			options.Cookie.HttpOnly = true;
			options.Cookie.IsEssential = true;
		});
		if (!IsLocalSafe)
		{
			services.AddHostedService<QueuedHostedService>();
			services.AddHostedService<TimedHostedService>();
		}
		services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
		if (IsDevMachine)
		{
			services.AddSingleton<IYanportService, YanportDevService>();
		}
		else
		{
			services.AddSingleton<IYanportService, YanportService>();
		}
		services.AddSingleton<IWebSiteService, WebSiteService>();
		services.AddSingleton<ILocalhostMailService, LocalhostMailService>();
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		bool exposeDocs = env.IsDevelopment() || IsLocalSafe;
		if (exposeDocs)
		{
			app.UseSwagger();
			app.UseSwaggerUI(delegate(SwaggerUIOptions c)
			{
				c.SwaggerEndpoint("/swagger/v1/swagger.json", "Perle-rare.info API V1");
			});
			RewriteOptions option = new RewriteOptions();
			option.AddRedirect("^$", "swagger");
			app.UseRewriter(option);
		}
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}
		if (!IsLocalSafe)
		{
			app.UseHttpsRedirection();
		}
		app.UseRouting();
		app.UseCors();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseSession();
		app.UseEndpoints(delegate(IEndpointRouteBuilder endpoints)
		{
			endpoints.MapControllers();
		});
	}
}
