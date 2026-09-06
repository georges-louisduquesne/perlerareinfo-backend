using System;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.ScheduledTasks;

public abstract class AbstractScheduledTask
{
	private ApplicationDbContext _context;

	private uint _logId;

	public static bool IsPauseTime
	{
		get
		{
			double time = (double)DateTime.Now.Hour + (double)DateTime.Now.Minute / 60.0;
			return time >= 1.5 && time <= 3.0;
		}
	}

	public void Init(ApplicationDbContext context, uint logId)
	{
		_context = context;
		_logId = logId;
	}

	public void Log(string message)
	{
		if (_context == null || _logId == 0 || message == null)
		{
			return;
		}
		try
		{
			_context.Database.ExecuteSqlRaw($"UPDATE `scheduled_task_logs` SET `STL_Results`=@p0 WHERE  `STL_Id`={_logId};", message.Truncate(1000));
		}
		catch
		{
		}
	}

	protected int GetParam(ApplicationDbContext dbContext, string paramName, int defaultValue)
	{
		AdminVersions param = dbContext.AdminVersions.Find(paramName);
		if (param == null)
		{
			param = new AdminVersions
			{
				AvParamName = paramName,
				AvVersion = defaultValue
			};
			dbContext.AdminVersions.Add(param);
			dbContext.SaveChanges();
		}
		return param.AvVersion;
	}

	public abstract string Execute(DateTime lastExecution);
}
