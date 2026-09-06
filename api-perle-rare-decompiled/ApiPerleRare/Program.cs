using System;
using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApiPerleRare;

public class Program
{
	public static void Main(string[] args)
	{
		IHost host = CreateHostBuilder(args).Build();
		if (args.Length == 0)
		{
			host.Run();
			return;
		}
		string text = args[0];
		string text2 = text;
		if (!(text2 == "-list"))
		{
			if (text2 == "-execute")
			{
				ExecuteTask(host, args.Skip(1).FirstOrDefault());
			}
		}
		else
		{
			ListTasks(host);
		}
	}

	private static void ExecuteTask(IHost host, string taskName)
	{
		if (string.IsNullOrWhiteSpace(taskName))
		{
			Console.WriteLine("Merci de spécifier une tâche");
			return;
		}
		IServiceScopeFactory scopeService = host.Services.GetService<IServiceScopeFactory>();
		using IServiceScope scope = scopeService.CreateScope();
		foreach (AbstractTask task in GetTasks(scope))
		{
			if (IsMatch(task.GetType().Name, taskName))
			{
				try
				{
					task.Run();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
			}
		}
	}

	private static bool IsMatch(string name, string filter)
	{
		return name.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) != -1;
	}

	private static void ListTasks(IHost host)
	{
		IServiceScopeFactory scopeService = host.Services.GetService<IServiceScopeFactory>();
		using IServiceScope scope = scopeService.CreateScope();
		foreach (AbstractTask t in GetTasks(scope))
		{
			Console.WriteLine(t.GetType().Name + " : " + t.Description);
		}
	}

	private static IEnumerable<AbstractTask> GetTasks(IServiceScope scope)
	{
		return (from t in typeof(AbstractTask).Assembly.GetTypes()
			where !t.IsAbstract && typeof(AbstractTask).IsAssignableFrom(t)
			select (AbstractTask)ActivatorUtilities.CreateInstance(scope.ServiceProvider, t)).ToArray();
	}

	public static IHostBuilder CreateHostBuilder(string[] args)
	{
		return Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(delegate(IWebHostBuilder webBuilder)
		{
			webBuilder.UseStartup<Startup>();
		});
	}
}
