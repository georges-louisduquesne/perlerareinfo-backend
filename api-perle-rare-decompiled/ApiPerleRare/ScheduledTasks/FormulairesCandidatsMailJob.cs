using System;

namespace ApiPerleRare.ScheduledTasks;

/// <summary>
/// Job dédié (hosted service), indépendant du bundle <see cref="TimedHostedService"/>.
/// Un seul processus actif à la fois (v2 prod, démo ou local), pas l'API v1 legacy.
/// </summary>
public static class FormulairesCandidatsMailJob
{
	internal const string EnvVar = "PR_FORMULAIRES_CANDIDATS_MAIL";

	internal const string LogName = "FormulairesCandidatsMail";

	internal static bool Enabled(string value)
	{
		return !string.Equals(value?.Trim(), "off", StringComparison.OrdinalIgnoreCase);
	}
}
