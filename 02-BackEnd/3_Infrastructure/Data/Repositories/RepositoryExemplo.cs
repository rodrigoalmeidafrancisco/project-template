using Data.Contexts;
using Data.Repositories._Base;
using Domain.Contracts;
using Domain.Entities;

namespace Data.Repositories
{
    public class RepositoryExemplo(ContextDefault contextDefault) : RepositoryBase<Exemplo>(contextDefault), IRepositoryExemplo
    {
    }
}
