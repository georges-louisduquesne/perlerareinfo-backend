namespace ApiPerleRare.ScheduledTasks.NotificationManagers;

internal class NouveauBien : AbstractNotificationManager
{
	protected override string Type => "NOUVEAU BIEN";

	public override string GetSubject(NotificationContainer notif)
	{
		return $"NEW {notif.Property.PCp} {AdjPrix(notif.NewPrix)}€ {AdjSurf(notif.Property.PSurface)}m² - {notif.ContactNomFamille}";
	}
}
