namespace ApiPerleRare.ScheduledTasks.NotificationManagers;

internal class ModifPrix : AbstractNotificationManager
{
	protected override string Type => "MODIF PRIX";

	public override string GetSubject(NotificationContainer notif)
	{
		return $"PRIX {notif.Property.PCp} {AdjPrix(notif.NewPrix)} <{AdjPrix(notif.OldPrix)}>€ {AdjSurf(notif.Property.PSurface)}m² - {notif.ContactNomFamille}";
	}
}
