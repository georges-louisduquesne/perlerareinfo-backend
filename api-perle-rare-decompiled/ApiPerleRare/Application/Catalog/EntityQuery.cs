namespace ApiPerleRare.Application.Catalog;

/// <summary>
/// Frozen list query: <c>select</c>, <c>where</c>, <c>orderby</c>, <c>skip</c>, <c>take</c>.
/// </summary>
public sealed class EntityQuery
{
	public string Select { get; init; }

	public string Where { get; init; }

	public string OrderBy { get; init; }

	public int Skip { get; init; }

	public int Take { get; init; }
}
