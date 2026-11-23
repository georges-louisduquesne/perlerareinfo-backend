using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Application.Conseillers;

/// <summary>
/// Republie les pages vitrine dont le contenu vient d'être écrit.
/// Le script PHP charge WordPress et demande à NitroPack d'oublier ces URL.
/// Absent en local : l'enregistrement CRM n'est pas bloqué.
/// </summary>
internal static class PublicPageRepublish
{
	internal const string DefaultScript = "/var/www/perle-rare.com-v2/web/wp-content/pr-purge-public-pages.php";

	internal const string DefaultPhp = "/usr/bin/php8.3";

	internal static bool TryProfileUrl(string url, out string canonical)
	{
		canonical = null;
		if (!ConseillerPublicParcours.TryPerleRarePage(url, out string language, out string slug))
		{
			return false;
		}
		canonical = "https://perle-rare.com/" + language + "/" + slug + "/";
		return true;
	}

	internal static async Task RunAsync(IEnumerable<string> urls, IEnumerable<long> pageIds, ILogger logger, CancellationToken cancellationToken)
	{
		if (string.Equals(Environment.GetEnvironmentVariable("PR_PUBLIC_PAGE_PURGE"), "off", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		string script = Environment.GetEnvironmentVariable("PR_PUBLIC_PAGE_PURGE_SCRIPT");
		if (string.IsNullOrWhiteSpace(script))
		{
			script = DefaultScript;
		}
		if (!File.Exists(script))
		{
			return;
		}
		List<string> cleanUrls = new List<string>();
		foreach (string url in urls ?? Array.Empty<string>())
		{
			if (TryProfileUrl(url, out string canonical) && !cleanUrls.Contains(canonical))
			{
				cleanUrls.Add(canonical);
			}
		}
		List<long> cleanIds = (pageIds ?? Array.Empty<long>()).Where((id) => id > 0).Distinct().ToList();
		if (cleanUrls.Count == 0 && cleanIds.Count == 0)
		{
			return;
		}
		string php = Environment.GetEnvironmentVariable("PR_PUBLIC_PAGE_PHP");
		if (string.IsNullOrWhiteSpace(php))
		{
			php = DefaultPhp;
		}
		ProcessStartInfo info = new ProcessStartInfo
		{
			FileName = php,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		info.ArgumentList.Add(script);
		info.Environment["PR_PURGE_URLS"] = string.Join("\n", cleanUrls);
		info.Environment["PR_PURGE_IDS"] = string.Join("\n", cleanIds);
		using Process process = Process.Start(info);
		if (process == null)
		{
			logger.LogWarning("Republication impossible : php n'a pas démarré.");
			return;
		}
		using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
		using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
		try
		{
			await process.WaitForExitAsync(linked.Token);
		}
		catch (OperationCanceledException)
		{
			try
			{
				process.Kill(entireProcessTree: true);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Republication interrompue, php n'a pas pu être arrêté.");
			}
			return;
		}
		string error = await process.StandardError.ReadToEndAsync();
		if (process.ExitCode != 0)
		{
			logger.LogWarning("Republication des pages publiques en échec ({Code}) {Error}.", process.ExitCode, error.Trim());
		}
	}
}
