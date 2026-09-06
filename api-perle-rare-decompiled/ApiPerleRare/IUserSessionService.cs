namespace ApiPerleRare;

public interface IUserSessionService
{
	bool IsAdmin { get; }

	bool Filter { get; set; }

	string Login { get; }

	string Statut { get; }

	void ClearCache();
}
