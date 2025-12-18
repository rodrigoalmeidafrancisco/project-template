using Data.Contexts;
using Data.Repositories._Base;
using Domain.Contracts;
using Domain.Entities;

namespace Data.Repositories
{
    public class RepositoryExemplo : RepositoryBase<Exemplo>, IRepositoryExemplo
    {
        public RepositoryExemplo(ContextDefault contextDefault) : base(contextDefault)
        {
        }
    }
}
