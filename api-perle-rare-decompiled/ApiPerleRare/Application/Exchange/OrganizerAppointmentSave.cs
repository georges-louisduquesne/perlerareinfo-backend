namespace ApiPerleRare.Application.Exchange;

/// <summary>
/// La copie organisateur doit être créée dans le calendrier de la boîte,
/// y compris lorsqu'il y a des invités. Save() sans dossier la laisse hors
/// du calendrier sur cet Exchange (le rendez-vous 509312 de Laurence Berry).
/// </summary>
public static class OrganizerAppointmentSave
{
	public static bool SaveInCalendarFolder(bool hasAttendees)
	{
		return true;
	}
}
