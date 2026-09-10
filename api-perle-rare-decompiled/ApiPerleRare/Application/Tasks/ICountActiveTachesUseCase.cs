
namespace ApiPerleRare.Application.Tasks;

public interface ICountActiveTachesUseCase
{
	System.Threading.Tasks.Task<int> Execute(string userLogin);
}
