using System.Linq;
using System.Reflection;
using ApiPerleRare.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace ApiPerleRare.Tests;

public class ConseillersPersonnelsTypesEvenementsControllerTests
{
	[Fact]
	public void Catalog_is_admin_only()
	{
		var attrs = typeof(ConseillersPersonnelsTypesEvenementsController)
			.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
		Assert.Contains(attrs, attr => (attr.Roles ?? "").Split(',').Any(role => role.Trim() == "Admin"));
	}
}
