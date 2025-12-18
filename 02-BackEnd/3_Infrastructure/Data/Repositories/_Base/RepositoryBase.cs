using Data.Contexts;
using Domain.Contracts._Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Data.Repositories._Base
{
    public class RepositoryBase<T>(ContextDefault contextDefault) : IRepositoryBase<T> where T : class
    {
        protected readonly ContextDefault _contextDefault = contextDefault ?? throw new ArgumentNullException(nameof(contextDefault));

        /// <summary>
        /// Adiciona uma entidade ao contexto e salva as alterações
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            ValidateEntity(entity);
            await _contextDefault.AddAsync(entity, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Adiciona uma coleção de entidades ao contexto e salva as alterações
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var entitiesList = ValidateEntities(entities);
            await _contextDefault.AddRangeAsync(entitiesList, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Atualiza uma entidade no contexto e salva as alterações
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            ValidateEntity(entity);
            _contextDefault.Update(entity);
            await SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Atualiza uma coleção de entidades no contexto e salva as alterações
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var entitiesList = ValidateEntities(entities);
            _contextDefault.UpdateRange(entitiesList);
            await SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Remove uma entidade do contexto e salva as alterações
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
        {
            ValidateEntity(entity);
            _contextDefault.Remove(entity);
            await SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Remove uma coleção de entidades do contexto e salva as alterações
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var entitiesList = ValidateEntities(entities);
            _contextDefault.RemoveRange(entitiesList);
            await SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Salva as alterações pendentes no contexto
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _contextDefault.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Verifica se existe alguma entidade que atenda aos critérios especificados
        /// </summary>
        /// <param name="queryWhere">Expressão de filtro</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se existe, false caso contrário</returns>
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> queryWhere, CancellationToken cancellationToken = default)
        {
            if (queryWhere == null)
            {
                throw new ArgumentNullException(nameof(queryWhere));
            }

            var query = BaseQuery(true, queryWhere);
            return await query.AnyAsync(queryWhere, cancellationToken);
        }

        /// <summary>
        /// Conta o número de entidades que atendem aos critérios especificados
        /// </summary>
        /// <param name="queryWhere">Expressão de filtro (opcional)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Número de entidades</returns>
        public async Task<int> CountAsync(Expression<Func<T, bool>> queryWhere = null, CancellationToken cancellationToken = default)
        {
            var query = BaseQuery(true, queryWhere);
            return await query.CountAsync(cancellationToken);
        }

        /// <summary>
        /// Obtém a primeira entidade baseada nos critérios especificados
        /// </summary>
        /// <param name="readOnly">Indica se a consulta deve ser somente leitura</param>
        /// <param name="queryWhere">Expressão de filtro para a consulta</param>
        /// <param name="queryIncludes">Expressões de include para objetos relacionados</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>A entidade encontrada ou null se não encontrada</returns>
        public async Task<T> GetFirstAsync(bool readOnly, Expression<Func<T, bool>> queryWhere = null, Expression<Func<T, object>>[] queryIncludes = null,
            CancellationToken cancellationToken = default)
        {
            var query = BaseQuery(readOnly, queryWhere, queryIncludes);
            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Obtém a primeira entidade baseada nos critérios especificados, porém lança uma exceção se mais de uma entidade for encontrada
        /// </summary>
        /// <param name="readOnly">Indica se a consulta deve ser somente leitura</param>
        /// <param name="queryWhere">Expressão de filtro para a consulta</param>
        /// <param name="queryIncludes">Expressões de include para objetos relacionados</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>A entidade encontrada ou null se não encontrada</returns>
        public async Task<T> GetSingleAsync(bool readOnly, Expression<Func<T, bool>> queryWhere = null, Expression<Func<T, object>>[] queryIncludes = null,
            CancellationToken cancellationToken = default)
        {
            var query = BaseQuery(readOnly, queryWhere, queryIncludes);
            return await query.SingleOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Seleciona uma lista de registros com base nos critérios especificados
        /// </summary>
        /// <param name="readOnly">Indica se a consulta deve ser somente leitura</param>
        /// <param name="queryWhere">Expressão de filtro para a consulta</param>
        /// <param name="queryIncludes">Expressões de include para objetos relacionados</param>
        /// <param name="orderBy">Expressão de ordenação</param>
        /// <param name="orderDescending">Indica se a ordenação deve ser descendente</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de entidades</returns>
        public async Task<List<T>> GetAllAsync(bool readOnly, Expression<Func<T, bool>> queryWhere = null, Expression<Func<T, object>>[] queryIncludes = null,
             Expression<Func<T, object>> orderBy = null, bool orderDescending = false, CancellationToken cancellationToken = default)
        {
            var query = BaseQuery(readOnly, queryWhere, queryIncludes, orderBy, orderDescending);
            return await query.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Seleciona uma lista paginada de registros
        /// </summary>
        /// <param name="readOnly">Indica se a consulta deve ser somente leitura</param>
        /// <param name="pageNumber">Número da página (deve ser maior ou igual a 1)</param>
        /// <param name="pageSize">Tamanho da página (deve ser maior que 0)</param>
        /// <param name="orderBy">Expressão de ordenação (obrigatória para paginação consistente)</param>
        /// <param name="orderDescending">Indica se a ordenação deve ser descendente</param>
        /// <param name="queryWhere">Expressão de filtro para a consulta</param>
        /// <param name="queryIncludes">Expressões de include para objetos relacionados</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Tupla contendo o total de registros e a lista paginada</returns>
        /// <exception cref="ArgumentException">Quando os parâmetros de paginação são inválidos</exception>
        public async Task<Tuple<int, List<T>>> GetAllPagedAsync(bool readOnly, int pageNumber, int pageSize, Expression<Func<T, object>> orderBy, bool orderDescending = false,
            Expression<Func<T, bool>> queryWhere = null, Expression<Func<T, object>>[] queryIncludes = null, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                throw new ArgumentException("O número da página deve ser maior ou igual a 1.", nameof(pageNumber));
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("O tamanho da página deve ser maior que 0.", nameof(pageSize));
            }

            if (orderBy == null)
            {
                throw new ArgumentException("O parâmetro 'orderBy' é obrigatório para paginação consistente.", nameof(orderBy));
            }

            // Query para contagem (sem includes para melhor performance)
            var countQuery = BaseQuery(true, queryWhere);

            // Obtém o total de registros
            var countTask = await countQuery.CountAsync(cancellationToken);

            // Query principal com paginação
            var itemsQuery = BaseQuery(readOnly, queryWhere, queryIncludes, orderBy, orderDescending);
            itemsQuery = itemsQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            // Obtém os itens da página
            var itemsTask = await itemsQuery.ToListAsync(cancellationToken);

            //Retornando o resultado como uma tupla
            return new Tuple<int, List<T>>(countTask, itemsTask);
        }

        #region Métodos Auxiliares Privados

        private IQueryable<T> BaseQuery(bool readOnly, Expression<Func<T, bool>> queryWhere = null, Expression<Func<T, object>>[] queryIncludes = null,
            Expression<Func<T, object>> orderBy = null, bool orderDescending = false)
        {
            IQueryable<T> query;

            query = readOnly ? _contextDefault.Set<T>().AsNoTracking() : _contextDefault.Set<T>();

            if (queryWhere != null)
            {
                query = query.Where(queryWhere);
            }

            if (queryIncludes?.Any() == true)
            {
                query = queryIncludes.Aggregate(query, (current, include) => current.Include(include));
            }

            if (orderBy != null)
            {
                query = orderDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }

            return query;
        }

        private static void ValidateEntity(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
        }

        private static List<T> ValidateEntities(IEnumerable<T> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);

            var entitiesList = entities.ToList();

            if (entitiesList.Any() == false)
            {
                throw new ArgumentException("A coleção de entidades não pode estar vazia.", nameof(entities));
            }

            return entitiesList;
        }

        #endregion Métodos Auxiliares Privados
    }
}
