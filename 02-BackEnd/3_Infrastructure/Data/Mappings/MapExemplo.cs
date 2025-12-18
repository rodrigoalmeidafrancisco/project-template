using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Mappings
{
    public class MapExemplo : IEntityTypeConfiguration<Exemplo>
    {
        public void Configure(EntityTypeBuilder<Exemplo> builder)
        {
            //Nome da tabela no banco de dados (nome, schema)
            builder.ToTable("Exemplo", "dbo");

            //Propriedades Base
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UserLog).HasMaxLength(100).IsRequired();
            builder.Property(x => x.DateChange).IsRequired();

            //Propriedades Específicas
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        }
    }
}
