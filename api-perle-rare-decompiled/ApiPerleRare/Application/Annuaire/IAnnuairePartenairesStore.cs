using System.Collections.Generic;

namespace ApiPerleRare.Application.Annuaire;

public interface IAnnuairePartenairesStore
{
	AnnuairePartenairesDocument Read();

	AnnuairePartenairesDocument Save(IReadOnlyList<AnnuairePartenaireRow> rows);
}
