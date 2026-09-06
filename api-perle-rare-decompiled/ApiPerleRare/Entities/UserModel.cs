namespace ApiPerleRare.Entities;

public class UserModel
{
	public uint Id { get; set; }

	public string FirstName { get; set; }

	public string LastName { get; set; }

	public string Login { get; set; }

	public string Token { get; set; }

	public bool IsAdmin { get; set; }

	public bool IsNegociateur { get; set; }

	public string AutoLogin { get; set; }

	public sbyte Dispo { get; set; }

	public bool Filter { get; set; }

	public string Email { get; set; }
}
