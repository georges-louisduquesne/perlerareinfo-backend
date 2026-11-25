using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApiPerleRare.Application.Files;
using ApiPerleRare.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApiPerleRare;

public class Program
{
	public static void Main(string[] args)
	{
		if (args.Length > 0 && args[0] == "-repair-oodrive-pdf")
		{
			RepairOodrivePdfs(args);
			return;
		}
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

	private static void RepairOodrivePdfs(string[] args)
	{
		if (args.Length < 2)
		{
			Console.WriteLine("Usage: -repair-oodrive-pdf fichier.pdf [...]");
			return;
		}
		for (int i = 1; i < args.Length; i++)
		{
			string path = args[i];
			try
			{
				byte[] input = File.ReadAllBytes(path);
				byte[] output = OodriveMacPdfRepair.Repair(input);
				if (output == null || output.Length == input.Length && ByteSame(output, input))
				{
					Console.WriteLine("unchanged " + path);
					continue;
				}
				string temporary = path + ".macrepair-tmp";
				File.WriteAllBytes(temporary, output);
				File.Move(temporary, path, true);
				Console.WriteLine("repaired " + path);
			}
			catch (Exception ex)
			{
				Console.WriteLine("failed " + path + " " + ex.Message);
			}
		}
	}

	private static bool ByteSame(byte[] left, byte[] right)
	{
		if (left.Length != right.Length)
		{
			return false;
		}
		for (int i = 0; i < left.Length; i++)
		{
			if (left[i] != right[i])
			{
				return false;
			}
		}
		return true;
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
