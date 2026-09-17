using System.Text.RegularExpressions;

namespace ApiPerleRare.Application.Events;

/// <summary>
/// Métamoteur « Supprimer tous les résultats » : only a contact-scoped
/// <c>property_contact</c> delete is accepted (no free-form WHERE).
/// </summary>
public static class PropertyContactBulkDelete
{
	private static readonly Regex ContactRefWhere = new(
		@"^\s*(PC_RefContact|PcRefContact|pcRefContact)\s*(=|eq)\s*(\d+)\s*$",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	public static bool TryParseContactRef(string where, out uint contactRef)
	{
		contactRef = 0;
		if (string.IsNullOrWhiteSpace(where))
		{
			return false;
		}
		Match match = ContactRefWhere.Match(where);
		if (!match.Success)
		{
			return false;
		}
		return uint.TryParse(match.Groups[3].Value, out contactRef) && contactRef > 0;
	}
}
