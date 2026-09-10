using System.Threading.Tasks;

namespace ApiPerleRare.Application.Exchange;

public interface IConseillerMailboxLookup
{
	Task<ConseillerMailbox> GetByConseillerIdAsync(int conseillerId);

	ConseillerMailbox GetByConseillerId(int conseillerId);
}
