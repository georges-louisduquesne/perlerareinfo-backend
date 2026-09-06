using System.Linq;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Http;

namespace ApiPerleRare.Services;

public class UserSessionService : IUserSessionService
{
	private readonly ApplicationDbContext _dbContext;

	private readonly HttpContext _httpContext;

	private ConseillersPersonnels _infos;

	private bool? _isAdmin;

	public bool IsAdmin
	{
		get
		{
			if (!_isAdmin.HasValue)
			{
				int? val = _httpContext.Session.GetInt32("IsAdmin");
				if (!val.HasValue)
				{
					CheckCache();
					_isAdmin = _infos?.CpAdmin ?? false;
					_httpContext.Session.SetInt32("IsAdmin", _isAdmin.Value ? 1 : 0);
				}
				else
				{
					_isAdmin = val == 1;
				}
			}
			return _isAdmin.Value;
		}
		set
		{
			_isAdmin = value;
		}
	}

	public bool Filter
	{
		get
		{
			if (!IsAdmin)
			{
				return true;
			}
			if (_httpContext == null)
			{
				return false;
			}
			return _httpContext.Session.GetInt32("Filter") == 1;
		}
		set
		{
			_httpContext.Session.SetInt32("Filter", value ? 1 : 0);
		}
	}

	public string Login => _httpContext.User.GetLogin();

	public string Statut
	{
		get
		{
			CheckCache();
			return _infos.CpStatut;
		}
	}

	public UserSessionService(ApplicationDbContext dbContext, IHttpContextAccessor contextAccessor)
	{
		_dbContext = dbContext;
		_httpContext = contextAccessor?.HttpContext;
	}

	private void CheckCache()
	{
		if (_infos == null)
		{
			_infos = _dbContext.ConseillersPersonnels.Where((ConseillersPersonnels cp) => (long)cp.CpRefConseiller == (long)_httpContext.User.GetId()).FirstOrDefault();
		}
	}

	public void ClearCache()
	{
		_httpContext.Session.Clear();
		_isAdmin = null;
		_infos = null;
	}
}
