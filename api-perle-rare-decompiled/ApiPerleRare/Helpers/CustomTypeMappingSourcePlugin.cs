using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal;

namespace ApiPerleRare.Helpers;

public class CustomTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
	private readonly IMySqlOptions _options;

	public CustomTypeMappingSourcePlugin(IMySqlOptions options)
	{
		_options = options;
	}

	public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
	{
		serviceCollection.AddSingleton<IRelationalTypeMappingSourcePlugin, CustomTypeMappingSourcePlugin>();
	}

	public RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
	{
		if (mappingInfo.StoreTypeName != null && mappingInfo.StoreTypeName.StartsWith("set("))
		{
			return new MySqlStringTypeMapping(mappingInfo.StoreTypeName, _options, StoreTypePostfix.None);
		}
		return null;
	}

	RelationalTypeMapping? IRelationalTypeMappingSourcePlugin.FindMapping(in RelationalTypeMappingInfo mappingInfo)
	{
		return FindMapping(in mappingInfo);
	}
}
