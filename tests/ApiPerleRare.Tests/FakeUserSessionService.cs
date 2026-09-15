namespace ApiPerleRare.Tests;

internal sealed class FakeUserSessionService : IUserSessionService
{
	public bool IsAdmin => true;

	public bool Filter { get; set; }

	public string Login => "demo";

	public string Statut => "";

	public void ClearCache()
	{
	}
}
