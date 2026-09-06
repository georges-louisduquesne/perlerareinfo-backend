#define TRACE
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiPerleRare.Helpers;

public class CustoSchemaFilter : ISchemaFilter, IOperationFilter, IDocumentFilter
{
	public void Apply(OpenApiSchema schema, SchemaFilterContext context)
	{
		if (!context.Type.FullName.StartsWith("ApiPerleRare"))
		{
			return;
		}
		Trace.WriteLine(context.Type.FullName ?? "");
		IEnumerable<string> navigations = schema.Properties.Keys.Where((string k) => k.EndsWith("Navigation"));
		foreach (string navigation in navigations)
		{
			schema.Properties.Remove(navigation);
		}
	}

	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		foreach (OpenApiTag tag in operation.Tags)
		{
			Trace.WriteLine("Ici2 : " + tag.Name);
		}
	}

	public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
	{
		Trace.WriteLine("ici3");
	}
}
