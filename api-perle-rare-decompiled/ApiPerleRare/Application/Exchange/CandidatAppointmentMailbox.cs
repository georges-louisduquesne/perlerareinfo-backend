namespace ApiPerleRare.Application.Exchange;

/// <summary>
/// Agenda d’un rendez-vous candidat déjà posé : l’organisateur Exchange, pas la première boîte où la copie a été trouvée.
/// </summary>
public static class CandidatAppointmentMailbox
{
	public static string AgendaMailbox(string appointmentOrganizer, string mailboxEmail)
	{
		string organizer = ExchangeOrganizerIdentity.SmtpOrEmpty(appointmentOrganizer);
		if (organizer.Length > 0)
		{
			return organizer;
		}
		return (mailboxEmail ?? string.Empty).Trim();
	}
}
