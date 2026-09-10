using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using ApiPerleRare.Application.Abstractions;
using ApiPerleRare.Application.Annonces;
using ApiPerleRare.Application.Authentication;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Application.Contacts;
using ApiPerleRare.Application.Events;
using ApiPerleRare.Application.Exchange;
using ApiPerleRare.Application.Files;
using ApiPerleRare.Application.Search;
using ApiPerleRare.Application.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Infrastructure.Files;
using ApiPerleRare.Infrastructure.Mail;
using ApiPerleRare.Infrastructure.Yanport;
using ApiPerleRare.Models;
using ApiPerleRare.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
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
		services.AddResponseCompression(delegate(ResponseCompressionOptions options)
		{
			options.EnableForHttps = true;
			options.Providers.Add<BrotliCompressionProvider>();
			options.Providers.Add<GzipCompressionProvider>();
			options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new string[1] { "application/json" });
		});
		services.Configure<BrotliCompressionProviderOptions>(delegate(BrotliCompressionProviderOptions options)
		{
			options.Level = System.IO.Compression.CompressionLevel.Fastest;
		});
		services.Configure<GzipCompressionProviderOptions>(delegate(GzipCompressionProviderOptions options)
		{
			options.Level = System.IO.Compression.CompressionLevel.Fastest;
		});
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
		services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
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
		services.AddMemoryCache();
		services.AddScoped<IUserService, UserService>();
		services.AddScoped<IAuthenticateUseCase, AuthenticateUseCase>();
		services.AddScoped<ISwitchDispoUseCase, SwitchDispoUseCase>();
		services.AddScoped<ISwitchFilterUseCase, SwitchFilterUseCase>();
		services.AddSingleton<IFileStorage, LocalFileStorage>();
		services.AddScoped<IUploadFileUseCase, UploadFileUseCase>();
		services.AddScoped<IDownloadFileUseCase, DownloadFileUseCase>();
		services.AddScoped(typeof(IQueryEntitiesUseCase<>), typeof(QueryEntitiesUseCase<>));
		services.AddScoped<IListEncaissementsEnCoursUseCase, ListEncaissementsEnCoursUseCase>();
		services.AddScoped<IListHomeClientEvenementsUseCase, ListHomeClientEvenementsUseCase>();
		services.AddScoped<IListProspectEvenementsUseCase, ListProspectEvenementsUseCase>();
		services.AddScoped<IListClientLastEvenementsUseCase, ListClientLastEvenementsUseCase>();
		services.AddScoped<IListEvenementsExUseCase, ListEvenementsExUseCase>();
		services.AddScoped<ICountEvenementsUseCase, CountEvenementsUseCase>();
		services.AddScoped<IListContactEvenementsUseCase, ListContactEvenementsUseCase>();
		services.AddScoped<IListOffresEnCoursUseCase, ListOffresEnCoursUseCase>();
		services.AddScoped<TachesExEnricher>();
		services.AddScoped<IListTachesExUseCase, ListTachesExUseCase>();
		services.AddScoped<ICountTachesUseCase, CountTachesUseCase>();
		services.AddScoped<IListProspectTachesUseCase, ListProspectTachesUseCase>();
		services.AddScoped<IListClientTachesUseCase, ListClientTachesUseCase>();
		services.AddScoped<ICountActiveTachesUseCase, CountActiveTachesUseCase>();
		services.AddScoped<IListContactsRechercheExUseCase, ListContactsRechercheExUseCase>();
		services.AddScoped<IListContactsRechercheAccueilUseCase, ListContactsRechercheAccueilUseCase>();
		services.AddScoped<ICountContactsRechercheUseCase, CountContactsRechercheUseCase>();
		services.AddScoped<IGetContactsRechercheByIdUseCase, GetContactsRechercheByIdUseCase>();
		services.AddScoped<ICreateContactsRechercheUseCase, CreateContactsRechercheUseCase>();
		services.AddScoped<IUpdateContactsRechercheUseCase, UpdateContactsRechercheUseCase>();
		services.AddScoped<IListAnnoncesGlobalesUseCase, ListAnnoncesGlobalesUseCase>();
		services.AddScoped<IGetAnnoncesGlobalesByIdUseCase, GetAnnoncesGlobalesByIdUseCase>();
		services.AddScoped<IViderInfosAnnoncesUseCase, ViderInfosAnnoncesUseCase>();
		services.AddScoped<IRecupInfosAnnoncesUseCase, RecupInfosAnnoncesUseCase>();
		services.AddScoped<IRecupInfosAnnonces2UseCase, RecupInfosAnnonces2UseCase>();
		services.AddScoped<IConseillerMailboxLookup, ConseillerMailboxLookup>();
		services.AddScoped<ISendEmailUseCase, SendEmailUseCase>();
		services.AddScoped<IGetUnreadEmailCountUseCase, GetUnreadEmailCountUseCase>();
		services.AddScoped<IGetTodayAppointmentsUseCase, GetTodayAppointmentsUseCase>();
		services.AddScoped<IAddAppointmentUseCase, AddAppointmentUseCase>();
		services.AddScoped<IFindAppointmentByEvenementUseCase, FindAppointmentByEvenementUseCase>();
		services.AddScoped<IDeleteAppointmentByEvenementUseCase, DeleteAppointmentByEvenementUseCase>();
		services.AddScoped<ISearchUseCase, SearchUseCase>();
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
		app.UseResponseCompression();
		bool exposeDocs = env.IsDevelopment() || IsLocalSafe;
		if (exposeDocs)
		{
			string demoPrefix = (Environment.GetEnvironmentVariable("PR_DEMO_PATH_PREFIX") ?? "").TrimEnd('/');
			app.UseSwagger();
			app.UseSwaggerUI(delegate(SwaggerUIOptions c)
			{
				c.SwaggerEndpoint(demoPrefix + "/swagger/v1/swagger.json", "Perle-rare.info API V1");
			});
			RewriteOptions option = new RewriteOptions();
			option.AddRedirect("^$", string.IsNullOrEmpty(demoPrefix) ? "swagger" : demoPrefix.TrimStart('/') + "/swagger");
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
