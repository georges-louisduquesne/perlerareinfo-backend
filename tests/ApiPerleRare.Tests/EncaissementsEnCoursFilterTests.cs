using Xunit;
using ApiPerleRare.Application.Events;

namespace ApiPerleRare.Tests;

public class EncaissementsEnCoursFilterTests
{
	[Theory]
	[InlineData(null, false)]
	[InlineData(0, false)]
	[InlineData(26950, true)]
	[InlineData(-1, true)]
	public void Honoraires_to_collect_ignore_empty_amounts(int? hono, bool expected)
	{
		decimal? value = hono.HasValue ? hono.Value : null;
		Assert.Equal(expected, EncaissementsEnCoursFilter.HasHonorairesToCollect(value));
	}

	[Theory]
	[InlineData("CLIENT ACTIF", true)]
	[InlineData("CLIENT ACTIF ", true)]
	[InlineData("client actif", true)]
	[InlineData("CLIENT MORT", false)]
	[InlineData("PROSPECT ACTIF", false)]
	[InlineData(null, false)]
	public void Only_active_clients_wait_for_collection(string statut, bool expected)
	{
		Assert.Equal(expected, EncaissementsEnCoursFilter.IsActiveClient(statut));
	}

	[Fact]
	public void Accueil_sql_keeps_php_like_client_actif()
	{
		Assert.Equal("contacts_recherche.C_Statut like 'CLIENT ACTIF'", EncaissementsEnCoursFilter.ActiveClientSql);
	}

	[Theory]
	[InlineData("RV ACTE AUTHENT.", true)]
	[InlineData("RV COMPROMIS", false)]
	[InlineData("OFFRE", false)]
	[InlineData(null, false)]
	public void Realized_transaction_is_acte_authentique_only(string type, bool expected)
	{
		Assert.Equal(expected, EncaissementsEnCoursFilter.IsRealizedTransaction(type));
	}
}
