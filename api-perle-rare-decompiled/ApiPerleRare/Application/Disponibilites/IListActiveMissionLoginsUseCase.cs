using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiPerleRare.Application.Disponibilites;

public interface IListActiveMissionLoginsUseCase
{
	Task<List<string>> Execute();
}
