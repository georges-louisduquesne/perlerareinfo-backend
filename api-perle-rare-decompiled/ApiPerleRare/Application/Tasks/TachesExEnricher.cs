using System;
using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Application.Tasks;

public sealed class TachesExEnricher
{
	private readonly IUserService _userService;

	public TachesExEnricher(IUserService userService)
	{
		_userService = userService;
	}

	public T CompleteTachesExes<T>(T taches) where T : IEnumerable<ITachesEx>
	{
		foreach (ITachesEx t in taches)
		{
			if (!string.IsNullOrEmpty(t.TQui))
			{
				t.TQui_PS = _userService.GetConseiller(t.TQui)?.CpPhotoSignature;
			}
			if (!string.IsNullOrEmpty(t.CNegociateur))
			{
				t.CNegociateur_PS = _userService.GetConseiller(t.CNegociateur)?.CpPhotoSignature;
			}
		}
		return taches;
	}

	public SelectResult<T> CompleteTachesExes<T>(SelectResult<T> taches) where T : ITachesEx
	{
		CompleteTachesExes(((IEnumerable<T>)taches.Items).Select((Func<T, ITachesEx>)((T v) => v)));
		return taches;
	}
}
