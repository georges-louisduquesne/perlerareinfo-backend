using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Application.Exchange;
using ApiPerleRare.Models;
using ApiPerleRare.ScheduledTasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Services;

/// <summary>
/// Activé seulement par <c>PR_TRANSACTION_EVENT_MAIL=on</c> (API v2 prod).
/// Au premier passage, le curseur se cale sur le dernier évènement : pas de rattrapage.
/// </summary>
public class TransactionEventMailHostedService : IHostedService, IDisposable
{
	private readonly ILogger<TransactionEventMailHostedService> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IConfiguration _configuration;

	private Timer _timer;

	private int _running;

	public TransactionEventMailHostedService(ILogger<TransactionEventMailHostedService> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
		_configuration = configuration;
	}

	public Task StartAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("TransactionEventMail actif.");
		_timer = new Timer(Tick, null, TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(30.0));
		return Task.CompletedTask;
	}

	private void Tick(object state)
	{
		if (Interlocked.Exchange(ref _running, 1) == 1)
		{
			return;
		}
		try
		{
			Poll();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "TransactionEventMail");
		}
		finally
		{
			Interlocked.Exchange(ref _running, 0);
		}
	}

	private void Poll()
	{
		using IServiceScope scope = _serviceScopeFactory.CreateScope();
		using ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		int? cursor = ReadCursor(db);
		if (!cursor.HasValue)
		{
			int max = db.Evenements.Max((Evenements e) => (int?)e.ERefEvenement) ?? 0;
			WriteCursor(db, max);
			_logger.LogInformation("TransactionEventMail initialisé au dernier évènement {Id}, sans envoi.", max);
			return;
		}
		List<PendingEvent> pending = (from e in db.Evenements.AsNoTracking()
			where e.ERefEvenement > cursor.Value && (e.ETypeEvenement == "OFFRE" || e.ETypeEvenement == "OFFRE ACCEPTEE" || e.ETypeEvenement == "OFFRE REFUSEE" || e.ETypeEvenement == "RV COMPROMIS" || e.ETypeEvenement == "RV ACTE AUTHENT.")
			orderby e.ERefEvenement
			select new PendingEvent
			{
				Id = e.ERefEvenement,
				Type = e.ETypeEvenement,
				EventName = e.ENomContact,
				Comment = e.ETexte,
				When = e.EDate,
				ContactId = e.ERefContact
			}).Take(20).ToList();
		if (pending.Count == 0)
		{
			return;
		}
		IExchangeService exchange = scope.ServiceProvider.GetRequiredService<IExchangeService>();
		IConseillerMailboxLookup mailboxes = scope.ServiceProvider.GetRequiredService<IConseillerMailboxLookup>();
		string password = SupportPassword(mailboxes);
		if (string.IsNullOrWhiteSpace(password))
		{
			_logger.LogWarning("TransactionEventMail : mot de passe de support@perle-rare.com introuvable, {Count} évènement(s) en attente.", pending.Count);
			return;
		}
		Dictionary<uint, ContactLabel> contacts = LoadContacts(db, pending);
		Dictionary<string, string> people = LoadPeople(db, contacts.Values);
		int sent = 0;
		int position = cursor.Value;
		foreach (PendingEvent item in pending)
		{
			ContactLabel contact = null;
			if (item.ContactId.HasValue)
			{
				contacts.TryGetValue(item.ContactId.Value, out contact);
			}
			int missionId = contact?.MissionId ?? 0;
			string family = contact?.FamilyName;
			string subject = TransactionEventMail.Subject(item.Type, missionId, TransactionEventMail.ClientName(item.EventName, family), item.When);
			string body = TransactionEventMail.HtmlBody(item.Comment, Person(people, contact?.NegotiatorLogin), Person(people, contact?.AdvisorLogin));
			bool ok;
			try
			{
				ok = exchange.SendMail(subject, body, "Perle Rare", TransactionEventMail.FromAddress, password, TransactionEventMail.Recipients, null, null, null, highImportance: false);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "TransactionEventMail envoi échoué pour l'évènement {Id}", item.Id);
				WriteLog(db, "erreur evt " + item.Id + " : " + ex.Message);
				break;
			}
			if (!ok)
			{
				_logger.LogWarning("TransactionEventMail envoi refusé pour l'évènement {Id}", item.Id);
				WriteLog(db, "envoi refusé evt " + item.Id);
				break;
			}
			position = TransactionEventMail.CursorAfterSend(position, item.Id, sent: true);
			WriteCursor(db, position);
			sent++;
			_logger.LogInformation("TransactionEventMail envoyé evt {Id} : {Subject}", item.Id, subject);
		}
		if (sent > 0)
		{
			WriteLog(db, "Envoyés : " + sent);
		}
	}

	private string SupportPassword(IConseillerMailboxLookup mailboxes)
	{
		string configured = SendEmailUseCase.SupportPasswordFrom(_configuration);
		if (!string.IsNullOrWhiteSpace(configured))
		{
			return configured;
		}
		try
		{
			ConseillerMailbox mailbox = mailboxes?.GetByEmail(TransactionEventMail.FromAddress);
			if (mailbox != null && string.Equals((mailbox.Email ?? "").Trim(), TransactionEventMail.FromAddress, StringComparison.OrdinalIgnoreCase))
			{
				return mailbox.Password;
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "TransactionEventMail : lecture de la boîte support impossible.");
		}
		return null;
	}

	private static Dictionary<uint, ContactLabel> LoadContacts(ApplicationDbContext db, List<PendingEvent> pending)
	{
		List<uint> ids = pending.Where((PendingEvent p) => p.ContactId.HasValue).Select((PendingEvent p) => p.ContactId.Value).Distinct().ToList();
		if (ids.Count == 0)
		{
			return new Dictionary<uint, ContactLabel>();
		}
		return (from c in db.ContactsRecherche.AsNoTracking()
			where ids.Contains(c.CRefContact)
			select new ContactLabel
			{
				Id = c.CRefContact,
				MissionId = c.CIdTypeMission,
				FamilyName = c.CNomFamille,
				NegotiatorLogin = c.CNegociateur,
				AdvisorLogin = c.CNomFamilleConseiller
			}).ToDictionary((ContactLabel c) => c.Id);
	}

	private static Dictionary<string, string> LoadPeople(ApplicationDbContext db, IEnumerable<ContactLabel> contacts)
	{
		List<string> logins = new List<string>();
		foreach (ContactLabel contact in contacts)
		{
			AddLogin(logins, contact.NegotiatorLogin);
			AddLogin(logins, contact.AdvisorLogin);
		}
		Dictionary<string, string> people = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (logins.Count == 0)
		{
			return people;
		}
		var rows = (from p in db.ConseillersPersonnels.AsNoTracking()
			where logins.Contains(p.CpLogin)
			select new
			{
				p.CpLogin,
				p.CpPrenom,
				p.CpNomFamille
			}).ToList();
		foreach (var row in rows)
		{
			string login = (row.CpLogin ?? "").Trim();
			if (login.Length == 0 || people.ContainsKey(login))
			{
				continue;
			}
			people[login] = TransactionEventMail.PersonName(row.CpPrenom, row.CpNomFamille);
		}
		return people;
	}

	private static void AddLogin(List<string> logins, string login)
	{
		string trimmed = (login ?? "").Trim();
		if (trimmed.Length == 0)
		{
			return;
		}
		for (int i = 0; i < logins.Count; i++)
		{
			if (string.Equals(logins[i], trimmed, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
		}
		logins.Add(trimmed);
	}

	private static string Person(Dictionary<string, string> people, string login)
	{
		string trimmed = (login ?? "").Trim();
		if (trimmed.Length == 0 || people == null || !people.TryGetValue(trimmed, out string name))
		{
			return "";
		}
		return name;
	}

	private static int? ReadCursor(ApplicationDbContext db)
	{
		AdminVersions row = db.AdminVersions.Find(TransactionEventMail.CursorParam);
		if (row == null)
		{
			return null;
		}
		return row.AvVersion;
	}

	private static void WriteCursor(ApplicationDbContext db, int eventId)
	{
		AdminVersions row = db.AdminVersions.Find(TransactionEventMail.CursorParam);
		if (row == null)
		{
			db.AdminVersions.Add(new AdminVersions
			{
				AvParamName = TransactionEventMail.CursorParam,
				AvVersion = eventId
			});
		}
		else
		{
			row.AvVersion = eventId;
		}
		db.SaveChanges();
	}

	private static void WriteLog(ApplicationDbContext db, string message)
	{
		if (message != null && message.Length > 1000)
		{
			message = message.Substring(0, 1000);
		}
		DateTime now = DateTime.Now;
		db.ScheduledTaskLogs.Add(new ScheduledTaskLogs
		{
			StlNom = "TransactionEventMail",
			StlDateDebut = now,
			StlDateFin = now,
			StlResults = message
		});
		db.SaveChanges();
	}

	public Task StopAsync(CancellationToken stoppingToken)
	{
		_timer?.Change(-1, 0);
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		_timer?.Dispose();
	}

	private sealed class PendingEvent
	{
		public int Id { get; set; }

		public string Type { get; set; }

		public string EventName { get; set; }

		public string Comment { get; set; }

		public DateTime When { get; set; }

		public uint? ContactId { get; set; }
	}

	private sealed class ContactLabel
	{
		public uint Id { get; set; }

		public int MissionId { get; set; }

		public string FamilyName { get; set; }

		public string NegotiatorLogin { get; set; }

		public string AdvisorLogin { get; set; }
	}
}
