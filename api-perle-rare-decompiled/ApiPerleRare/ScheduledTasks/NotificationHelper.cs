using System.Linq;
using ApiPerleRare.ScheduledTasks.NotificationManagers;

namespace ApiPerleRare.ScheduledTasks;

public class NotificationHelper
{
	private readonly IExchangeService _exchangeService;

	private readonly AbstractNotificationManager[] _managers;

	public NotificationHelper(IExchangeService exchangeService)
	{
		_exchangeService = exchangeService;
		_managers = new AbstractNotificationManager[2]
		{
			new NouveauBien(),
			new ModifPrix()
		};
	}

	public void LogError(string subject, string error)
	{
		YanportScheduledTask.LogError(_exchangeService, subject, error);
	}

	public AbstractNotificationManager GetManager(NotificationContainer nc)
	{
		return _managers.FirstOrDefault((AbstractNotificationManager m) => m.IsMatch(nc));
	}

	public bool SendMail(string subject, string contents, string senderName, string senderEmail, string senderPassword, string[] to, string[] cc, string[] cci, MailAttachment[] attachments, bool highImportance)
	{
		return _exchangeService.SendMail(subject, contents, senderName, senderEmail, senderPassword, to, cc, cci, attachments, highImportance);
	}
}
