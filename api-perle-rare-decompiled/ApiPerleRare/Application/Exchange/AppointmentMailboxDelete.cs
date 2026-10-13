namespace ApiPerleRare.Application.Exchange;

/// <summary>
/// Une suppression vise la boîte demandée. L'identifiant Exchange stocké
/// appartient à l'organisateur : nettoyer une autre boîte ne doit pas l'effacer
/// ni supprimer le rendez-vous de l'organisateur.
/// </summary>
public static class AppointmentMailboxDelete
{
	public readonly struct Target
	{
		public int MailboxId { get; init; }

		public string AppointmentId { get; init; }

		public bool ClearStoredAppointmentId { get; init; }
	}

	public static Target ForRequestedMailbox(int requestedMailboxId, int? organizerId, string storedAppointmentId)
	{
		bool organizerKnown = organizerId.HasValue;
		bool ownsStoredId = !organizerKnown || organizerId.Value == requestedMailboxId;
		string appointmentId = ownsStoredId && !string.IsNullOrWhiteSpace(storedAppointmentId)
			? storedAppointmentId
			: null;
		return new Target
		{
			MailboxId = requestedMailboxId,
			AppointmentId = appointmentId,
			ClearStoredAppointmentId = appointmentId != null
		};
	}
}
