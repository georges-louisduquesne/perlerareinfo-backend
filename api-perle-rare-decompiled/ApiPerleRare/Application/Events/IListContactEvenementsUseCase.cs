using System.Collections.Generic;
using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Events;

public interface IListContactEvenementsUseCase
{
	Task<List<ContactEvenements>> Execute(EntityQuery query);
}
