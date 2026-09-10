
namespace ApiPerleRare.Application.Tasks;

public interface ICountTachesUseCase
{
	System.Threading.Tasks.Task<int> Execute(string where, string option, string userLogin, bool applyQuiFilter);
}
