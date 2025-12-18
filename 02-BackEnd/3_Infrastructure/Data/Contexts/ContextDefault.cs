using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Reflection;

namespace Data.Contexts
{
    public class ContextDefault : DbContext
    {
        public ContextDefault(DbContextOptions<ContextDefault> options) : base(options)
        {

        }

        #region DbSet's

        //SCHEMA: Aplicativo
        //public DbSet<Exemplo> Exemplo { get; set; }


        #endregion DbSet's

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Aplica todas as configurações de mapeamento de entidades do assembly atual
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        #region Suporte a Triggers - EntityFramework >= 8.0

        //Documentação https://learn.microsoft.com/pt-br/ef/core/what-is-new/ef-core-7.0/breaking-changes?tabs=v7#sqlserver-tables-with-triggers

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Conventions.Add(_ => new BlankTriggerAddingConvention());
        }

        public class BlankTriggerAddingConvention : IModelFinalizingConvention
        {
            public virtual void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
            {
                foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
                {
                    var table = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);
                    if (table != null
                        && entityType.GetDeclaredTriggers().All(t => t.GetDatabaseName(table.Value) == null)
                        && (entityType.BaseType == null || entityType.GetMappingStrategy() != RelationalAnnotationNames.TphMappingStrategy))
                    {
                        entityType.Builder.HasTrigger(table.Value.Name + "_Trigger");
                    }

                    foreach (var fragment in entityType.GetMappingFragments(StoreObjectType.Table))
                    {
                        if (entityType.GetDeclaredTriggers().All(t => t.GetDatabaseName(fragment.StoreObject) == null))
                        {
                            entityType.Builder.HasTrigger(fragment.StoreObject.Name + "_Trigger");
                        }
                    }
                }
            }
        }

        #endregion Suporte a Triggers - EntityFramework >= 8.0

    }
}
