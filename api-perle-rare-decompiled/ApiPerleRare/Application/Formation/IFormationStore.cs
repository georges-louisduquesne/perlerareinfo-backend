using System.Collections.Generic;

namespace ApiPerleRare.Application.Formation;

public interface IFormationStore
{
	long CreateSeries(int userId, string section, IReadOnlyList<int> questions);

	void RecordAnswer(int userId, long serie, int ordre, int question, string resultat);

	int CloseScore(int userId, long serie);
}
