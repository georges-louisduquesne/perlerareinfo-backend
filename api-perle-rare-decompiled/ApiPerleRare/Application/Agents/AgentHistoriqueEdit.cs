namespace ApiPerleRare.Application.Agents;

/// <summary>
/// C_Historique n’est modifiable que par un admin. Un autre enregistrement
/// de la fiche conserve le texte déjà en base.
/// </summary>
public static class AgentHistoriqueEdit
{
	public static string Resolve(bool isAdmin, string incoming, string stored)
	{
		return isAdmin ? incoming : stored;
	}
}

public class AgentHistoriqueBody
{
	public string Text { get; set; }
}
