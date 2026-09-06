using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiPerleRare.Entities;

public class RegisterModel
{
	[Required]
	public string FirstName { get; set; }

	[Required]
	public string LastName { get; set; }

	[Required]
	public string Login { get; set; }

	[Required]
	public string Password { get; set; }

	public List<RoleModel> Roles { get; set; }
}
