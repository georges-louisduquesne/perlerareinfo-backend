using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using ApiPerleRare.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableCors]
[Authorize]
public class MailsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	private readonly ILogger<MailsController> _logger;

	private readonly IExchangeService _exchangeService;

	private const string _emailRegex = "(?(\")(\".+?(?<!\\\\)\"@)|(([0-9a-z]((\\.(?!\\.))|[-!#\\$%&'\\*\\+/=\\?\\^`\\{\\}\\|~\\w])*)(?<=[0-9a-z])@))(?(\\[)(\\[(\\d{1,3}\\.){3}\\d{1,3}\\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\\.)+[a-z0-9][\\-a-z0-9]{0,22}[a-z0-9]))";

	public MailsController(ApplicationDbContext context, ILogger<MailsController> logger, IExchangeService exchangeService)
	{
		_context = context;
		_logger = logger;
		_exchangeService = exchangeService;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<Mails>>> GetMails([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		DbSet<Mails> query = _context.Mails;
		return await EFHelper<Mails>.Select(query, where, orderby, take, skip, select);
	}

	private ActionResult<string> SendMail([FromQuery] int mailId, [FromQuery] bool force = false)
	{
		Mails mail = _context.Mails.Single((Mails m) => (long)m.Id == (long)mailId);
		if (mail.State == 6 && !force)
		{
			return "Mail déjà envoyé";
		}
		try
		{
			ConseillersPersonnels sender = _context.ConseillersPersonnels.FirstOrDefault((ConseillersPersonnels cp) => (uint?)cp.CpRefConseiller == mail.Envoyeur);
			if (sender == null)
			{
				throw new Exception("CP " + mail.Refcontact + " introuvable");
			}
			string sujet = EncodingHelper.FixEncoding(mail.Subject);
			string contents = mail.Contents;
			if (!contents.Contains("<html"))
			{
				contents = Resources.MailTemplate.Replace("%CONTENTS%", contents);
			}
			List<MailAttachment> attachments = new List<MailAttachment>();
			throw new Exception("A finir !!!");
		}
		catch (Exception ex)
		{
			mail.State = 7u;
			mail.Error = ex.ToString();
			if (mail.Error.Length > 990)
			{
				mail.Error = mail.Error.Substring(0, 990);
			}
			_logger.LogError("Exception envoi de mail (" + mailId + ")", ex);
			_exchangeService.SendError("Exception envoi de mail (" + mailId + ")", ex.ToString());
		}
		_context.SaveChanges();
		return mail.Error ?? "OK";
	}

	private string GetEmails(string value, out string[] emails)
	{
		List<string> list = new List<string>();
		if (value != null)
		{
			list.AddRange(from Match m in Regex.Matches(value, "(?(\")(\".+?(?<!\\\\)\"@)|(([0-9a-z]((\\.(?!\\.))|[-!#\\$%&'\\*\\+/=\\?\\^`\\{\\}\\|~\\w])*)(?<=[0-9a-z])@))(?(\\[)(\\[(\\d{1,3}\\.){3}\\d{1,3}\\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\\.)+[a-z0-9][\\-a-z0-9]{0,22}[a-z0-9]))", RegexOptions.IgnoreCase)
				select m.Value);
		}
		emails = list.ToArray();
		return string.Join(", ", emails);
	}
}
