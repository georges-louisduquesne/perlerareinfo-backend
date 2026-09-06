using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApiPerleRare.YanportModels;
using Newtonsoft.Json;

namespace ApiPerleRare.ScheduledTasks;

public abstract class AbstractNotificationManager
{
	protected abstract string Type { get; }

	public virtual bool IsMatch(NotificationContainer notif)
	{
		return notif.Type == Type;
	}

	public abstract string GetSubject(NotificationContainer notif);

	public virtual string GetBody(NotificationContainer notif)
	{
		string image = "";
		if (notif.ImageContent != null)
		{
			image = "<img width='250px' height='180px' id='1' src='cid:img'>";
		}
		string adresse = (string.IsNullOrEmpty(notif.Property.PAdresseRue) ? "" : $"{notif.Property.PAdresseNum} {notif.Property.PAdresseRue} {notif.Property.PCp}<br/>");
		StringBuilder body = new StringBuilder();
		Ad[] ads = JsonConvert.DeserializeObject<Ad[]>(notif.Property.PAds);
		Ad[] array = ads;
		StringBuilder stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler;
		foreach (Ad ad in array)
		{
			YanportScheduledTask.Source s;
			string source = ((!YanportScheduledTask.TryGetSource(ad.CrawlSource, out s)) ? ad.CrawlSource : s.NomMoteur);
			if (string.IsNullOrEmpty(ad.Url))
			{
				stringBuilder = body;
				StringBuilder stringBuilder2 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(31, 1, stringBuilder);
				handler.AppendLiteral("aucun lien vers l'annonce ");
				handler.AppendFormatted(source);
				handler.AppendLiteral("<br/>");
				stringBuilder2.AppendLine(ref handler);
			}
			else
			{
				stringBuilder = body;
				StringBuilder stringBuilder3 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(40, 2, stringBuilder);
				handler.AppendLiteral("<a href='");
				handler.AppendFormatted(ad.Url);
				handler.AppendLiteral("'>lien vers l'annonce ");
				handler.AppendFormatted(source);
				handler.AppendLiteral("</a><br/>");
				stringBuilder3.AppendLine(ref handler);
			}
		}
		stringBuilder = body;
		StringBuilder stringBuilder4 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(98, 1, stringBuilder);
		handler.AppendLiteral("\r\n<a href='https://perle-rare.info/#/view/page/contacts_recherche_form&ref%3D");
		handler.AppendFormatted(notif.RefContact);
		handler.AppendLiteral("'>lien .info</a><br/>");
		stringBuilder4.AppendLine(ref handler);
		body.AppendLine(adresse);
		Dealer[] dealers = JsonConvert.DeserializeObject<Dealer[]>(notif.Property.PAnnonceurs);
		Dealer[] array2 = dealers;
		foreach (Dealer d in array2)
		{
			string infos = string.Join(" / ", new string[2] { d.PhoneNumber, d.Email }.Where((string value) => !string.IsNullOrWhiteSpace(value)));
			stringBuilder = body;
			StringBuilder stringBuilder5 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(11, 3, stringBuilder);
			handler.AppendFormatted(d.Name);
			handler.AppendLiteral(" - ");
			handler.AppendFormatted(d.PhoneNumber);
			handler.AppendLiteral(" / ");
			handler.AppendFormatted(d.Email);
			handler.AppendLiteral("<br/>");
			stringBuilder5.AppendLine(ref handler);
		}
		stringBuilder = body;
		StringBuilder stringBuilder6 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder);
		handler.AppendFormatted(notif.Property.PDescription);
		handler.AppendLiteral(" <br/>");
		stringBuilder6.AppendLine(ref handler);
		body.AppendLine(image);
		return body.ToString();
	}

	public virtual IEnumerable<MailAttachment> GetAttachments(NotificationContainer notif)
	{
		if (notif.ImageContent != null)
		{
			yield return new MailAttachment(notif.ImageFileName, notif.ImageContent)
			{
				IsInLine = true,
				ContentId = "img"
			};
		}
	}

	protected string AdjPrix(string prix)
	{
		if (string.IsNullOrWhiteSpace(prix))
		{
			return "nc";
		}
		return AddThousands(prix);
	}

	private static string AddThousands(string val)
	{
		for (int i = val.Length - 3; i >= 1; i -= 3)
		{
			val = val.Insert(i, ".");
		}
		return val;
	}

	protected string AdjSurf(int surf)
	{
		if (surf <= 0)
		{
			return "nc";
		}
		return AddThousands(surf.ToString());
	}

	protected string AdjSurf(double? surf)
	{
		return AdjSurf((int)surf.GetValueOrDefault());
	}
}
