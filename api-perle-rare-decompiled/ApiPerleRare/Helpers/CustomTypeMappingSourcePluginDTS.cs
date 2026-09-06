using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPerleRare.Helpers;

public class CustomTypeMappingSourcePluginDTS : IDesignTimeServices
{
	public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
	{
		serviceCollection.AddSingleton<IRelationalTypeMappingSourcePlugin, CustomTypeMappingSourcePlugin>();
	}
}
