using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using ApiPerleRare.Models;
using ApiPerleRare.Properties;
using Newtonsoft.Json;

namespace ApiPerleRare.ScheduledTasks;

public class NotificationContainer
{
	private readonly NotificationHelper _helper;

	private readonly Notifications _notif;

	private readonly Property _property;

	private readonly string _conseillerMail;

	public string ContactNomFamille { get; }

	public string Type => _notif.NType;

	public Property Property => _property;

	public uint RefContact => _notif.NRefContact;

	public string NewPrix => _notif.NNewPrix;

	public string OldPrix => _notif.NOldPrix;

	public string ImageFileName { get; private set; }

	public byte[] ImageContent { get; private set; }

	public NotificationContainer(NotificationHelper helper, Notifications notif, Property property, string conseillerMail, string contactNomFamille)
	{
		_helper = helper;
		_notif = notif;
		_property = property;
		_conseillerMail = conseillerMail;
		ContactNomFamille = contactNomFamille;
	}

	public bool TryGetImage(out string fileName, out byte[] content)
	{
		if (string.IsNullOrWhiteSpace(_property.PImages))
		{
			fileName = null;
			content = null;
			return false;
		}
		try
		{
			string imageUrl = JsonConvert.DeserializeObject<string[]>(_property.PImages).FirstOrDefault();
			if (imageUrl == null)
			{
				fileName = null;
				content = null;
				return false;
			}
			Match m = Regex.Match(imageUrl, "\\.\\w{3,4}$");
			string ext = (m.Success ? m.Value : "");
			using (HttpClient wc = new HttpClient())
			{
				content = wc.GetByteArrayAsync(imageUrl).Result;
			}
			fileName = "img" + ext;
			return true;
		}
		catch (Exception ex)
		{
			_helper.LogError("Erreur récupération image propriété " + _property.PPropertyId, ex.ToString());
			fileName = null;
			content = null;
			return false;
		}
	}

	internal sbyte SendNotification()
	{
		AbstractNotificationManager manager = _helper.GetManager(this);
		if (manager == null)
		{
			_helper.LogError("Aucun notificationmanager pour '" + Type + "'", "");
			return 2;
		}
		if (TryGetImage(out var fileName, out var content))
		{
			ImageFileName = fileName;
			ImageContent = content;
		}
		string subject = manager.GetSubject(this);
		string body = Resources.MailTemplate.Replace("%CONTENTS%", manager.GetBody(this));
		return (sbyte)(_helper.SendMail(subject, body, "Perlerare-Info", "alerte@perle-rare.com", "TOURIST28", new string[1] { _conseillerMail }, null, new string[1] { "georges-louis.duquesne@perle-rare.com" }, manager.GetAttachments(this).ToArray(), highImportance: false) ? 1 : (-1));
	}
}
