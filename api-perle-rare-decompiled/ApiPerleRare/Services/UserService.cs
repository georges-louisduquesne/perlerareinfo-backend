using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ApiPerleRare.Entities;
using ApiPerleRare.Exceptions;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
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
		if (password != user.CpMotDePasse)
		{
			return null;
		}
		_userSessionService.ClearCache();
		string date = DateTime.Now.ToString("ddMM");
		if (user.CpAutoLogin == null || !user.CpAutoLogin.StartsWith(date))
		{
			user.CpAutoLogin = date + new Random().Next();
			if (user.CpAutoLogin.Length > 15)
			{
				user.CpAutoLogin = user.CpAutoLogin.Substring(0, 15);
			}
			_context.SaveChanges();
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
		JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler
		{
			TokenLifetimeInMinutes = 10080
		};
		byte[] key = Encoding.ASCII.GetBytes(_appSettings.Secret);
		SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(claims),
			Expires = DateTime.UtcNow.AddDays(7.0),
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

	private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
	{
		if (password == null)
		{
			throw new ArgumentNullException("password");
		}
		if (string.IsNullOrWhiteSpace(password))
		{
			throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
		}
		passwordSalt = new byte[16];
		using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
		{
			rng.GetBytes(passwordSalt);
		}
		passwordHash = KeyDerivation.Pbkdf2(password, passwordSalt, KeyDerivationPrf.HMACSHA1, 10000, 32);
	}

	private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
	{
		if (password == null)
		{
			throw new ArgumentNullException("password");
		}
		if (string.IsNullOrWhiteSpace(password))
		{
			throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
		}
		if (storedHash.Length != 32)
		{
			throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
		}
		if (storedSalt.Length != 16)
		{
			throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");
		}
		byte[] computedHash = KeyDerivation.Pbkdf2(password, storedSalt, KeyDerivationPrf.HMACSHA1, 10000, 32);
		for (int i = 0; i < computedHash.Length; i++)
		{
			if (computedHash[i] != storedHash[i])
			{
				return false;
			}
		}
		return true;
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
		if (!_conseillers.Contains(login))
		{
			lock (_conseillers)
			{
				if (!_conseillers.Contains(login))
				{
					ConseillersPersonnels c = _context.ConseillersPersonnels.AsNoTracking().SingleOrDefault((ConseillersPersonnels conseillersPersonnels) => conseillersPersonnels.CpLogin == login) ?? cp_notFound;
					_conseillers.Add(login, c, new CacheItemPolicy
					{
						SlidingExpiration = TimeSpan.FromHours(1.0)
					});
				}
			}
		}
		return _conseillers[login] as ConseillersPersonnels;
	}

	public string GetInfo()
	{
		return $"Taille du cache : {_conseillers.GetCount()}";
	}
}
