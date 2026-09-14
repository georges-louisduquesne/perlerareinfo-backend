using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Text;
using ApiPerleRare.Entities;
using ApiPerleRare.Exceptions;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApiPerleRare.Services;

public class UserService : IUserService
{
	private readonly AppSettings _appSettings;

	private readonly ApplicationDbContext _context;

	private readonly IAuditService _auditService;

	private readonly IUserSessionService _userSessionService;

	private readonly ConseillersPersonnels cp_notFound = new ConseillersPersonnels();

	private MemoryCache _conseillers = MemoryCache.Default;

	public UserService(IOptions<AppSettings> appSettings, ApplicationDbContext context, IAuditService auditService, IUserSessionService userSessionService)
	{
		_appSettings = appSettings.Value;
		_context = context;
		_auditService = auditService;
		_userSessionService = userSessionService;
	}

	public ConseillersPersonnels Authenticate(string login, string password, out string token)
	{
		ConseillersPersonnels user = _context.ConseillersPersonnels.SingleOrDefault((ConseillersPersonnels x) => x.CpLogin == login && (int?)x.CpActif == (int?)1);
		token = null;
		if (user == null)
		{
			return null;
		}
		if (!PasswordAuth.TryAuthenticate(password, user.CpMotDePasse, out string upgradedHash))
		{
			return null;
		}
		bool needsHashRewrite = upgradedHash != null;
		if (needsHashRewrite)
		{
			user.CpMotDePasse = upgradedHash;
		}

		_userSessionService.ClearCache();
		string date = DateTime.Now.ToString("ddMM");
		bool needsAutoLogin = user.CpAutoLogin == null || !user.CpAutoLogin.StartsWith(date);
		if (needsAutoLogin)
		{
			user.CpAutoLogin = date + new Random().Next();
			if (user.CpAutoLogin.Length > 15)
			{
				user.CpAutoLogin = user.CpAutoLogin.Substring(0, 15);
			}
		}
		if (needsHashRewrite || needsAutoLogin)
		{
			// Demo / local: MariaDB is SELECT-only — persist hash + autologin only on writable APIs.
			bool readOnlyDemo = string.Equals(Environment.GetEnvironmentVariable("PR_LOCAL_SAFE"), "1", StringComparison.Ordinal);
			if (!readOnlyDemo)
			{
				_context.SaveChanges();
			}
		}
		List<Claim> claims = new List<Claim>();
		claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", user.CpRefConseiller.ToString()));
		claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", user.CpLogin));
		if (user.CpAdmin)
		{
			claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "Admin"));
		}
		if (user.CpNegociateur)
		{
			claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "Negociateur"));
		}
		// 48 h — decided with client; activating in prod still implies reconnect when secret/TTL ship together.
		const int tokenLifetimeMinutes = 2880;
		JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler
		{
			TokenLifetimeInMinutes = tokenLifetimeMinutes
		};
		byte[] key = Encoding.ASCII.GetBytes(_appSettings.Secret);
		SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(claims),
			Expires = DateTime.UtcNow.AddMinutes(tokenLifetimeMinutes),
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256")
		};
		SecurityToken secToken = tokenHandler.CreateToken(tokenDescriptor);
		token = tokenHandler.WriteToken(secToken);
		return user;
	}

	public IEnumerable<ConseillersPersonnels> GetAll()
	{
		return _context.ConseillersPersonnels;
	}

	public ConseillersPersonnels GetById(uint id)
	{
		return _context.ConseillersPersonnels.Find(id);
	}

	public ConseillersPersonnels Create(ConseillersPersonnels user, RegisterModel model)
	{
		if (string.IsNullOrWhiteSpace(model.Password))
		{
			throw new AppException("Password is required");
		}
		if (_context.ConseillersPersonnels.Any((ConseillersPersonnels x) => x.CpLogin == user.CpLogin))
		{
			throw new AppException("Login \"" + user.CpLogin + "\" is already taken");
		}
		foreach (RoleModel role in model.Roles)
		{
		}
		_context.ConseillersPersonnels.Add(user);
		_context.SaveChanges();
		return user;
	}

	public void Update(ConseillersPersonnels userParam, string password = null)
	{
		ConseillersPersonnels user = _context.ConseillersPersonnels.Find(userParam.CpRefConseiller);
		if (user == null)
		{
			throw new AppException("User not found");
		}
		throw new Exception("A finir");
	}

	public void Delete(uint id)
	{
		ConseillersPersonnels user = _context.ConseillersPersonnels.Find(id);
		if (user != null)
		{
			_context.ConseillersPersonnels.Remove(user);
			_context.SaveChanges();
		}
	}

	public bool SwitchDispo(int refConseiller, string remoteIpAddress)
	{
		_context.Database.ExecuteSqlRaw($"UPDATE conseillers_personnels SET CP_Dispo = 1-CP_Dispo WHERE CP_RefConseiller={refConseiller}");
		var info = (from cp in _context.ConseillersPersonnels
			where (long)cp.CpRefConseiller == (long)refConseiller
			select new { cp.CpDispo, cp.CpLogin }).Single();
		_auditService.Add(_context, info.CpLogin, "conseillers_personnels", "UPDATE", $"CP_Dispo: {1 - info.CpDispo}>{info.CpDispo}", "CP_Login=" + info.CpLogin, remoteIpAddress);
		return info.CpDispo == 1;
	}

	public ConseillersPersonnels GetConseiller(string login)
	{
		if (string.IsNullOrEmpty(login))
		{
			return null;
		}
		if (!_conseillers.Contains(login))
		{
			lock (_conseillers)
			{
				if (!_conseillers.Contains(login))
				{
					ConseillersPersonnels c = _context.ConseillersPersonnels.AsNoTracking().SingleOrDefault((ConseillersPersonnels conseillersPersonnels) => conseillersPersonnels.CpLogin == login) ?? cp_notFound;
					_conseillers.Add(login, c, CachePolicy());
				}
			}
		}
		return _conseillers[login] as ConseillersPersonnels;
	}

	public void WarmConseillers(IEnumerable<string> logins)
	{
		if (logins == null)
		{
			return;
		}
		List<string> missing = new List<string>();
		foreach (string login in logins)
		{
			if (!string.IsNullOrEmpty(login) && !_conseillers.Contains(login))
			{
				missing.Add(login);
			}
		}
		if (missing.Count == 0)
		{
			return;
		}
		List<ConseillersPersonnels> found = _context.ConseillersPersonnels.AsNoTracking()
			.Where((ConseillersPersonnels c) => missing.Contains(c.CpLogin))
			.ToList();
		Dictionary<string, ConseillersPersonnels> byLogin = found.ToDictionary((ConseillersPersonnels c) => c.CpLogin);
		lock (_conseillers)
		{
			foreach (string login in missing)
			{
				if (_conseillers.Contains(login))
				{
					continue;
				}
				ConseillersPersonnels c = byLogin.TryGetValue(login, out ConseillersPersonnels hit) ? hit : cp_notFound;
				_conseillers.Add(login, c, CachePolicy());
			}
		}
	}

	private static CacheItemPolicy CachePolicy()
	{
		return new CacheItemPolicy
		{
			SlidingExpiration = TimeSpan.FromHours(1.0)
		};
	}

	public string GetInfo()
	{
		return $"Taille du cache : {_conseillers.GetCount()}";
	}
}
