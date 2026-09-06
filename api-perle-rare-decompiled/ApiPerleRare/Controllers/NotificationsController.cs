using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;

namespace ApiPerleRare.Controllers;

public class NotificationsController : QueryEntitiesController<Notifications>
{
	public NotificationsController(IQueryEntitiesUseCase<Notifications> query)
		: base(query)
	{
	}
}
