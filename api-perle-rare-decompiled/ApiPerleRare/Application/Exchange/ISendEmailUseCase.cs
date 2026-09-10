using ApiPerleRare.Controllers;

namespace ApiPerleRare.Application.Exchange;

public interface ISendEmailUseCase
{
	string Execute(Email email);
}
