using Microsoft.EntityFrameworkCore;
using EstoqueService.Models;
using System.Collections.Generic;

namespace EstoqueService.Data
{
    public class EstoqueDbContext : DbContext
    {
        public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : base(options)
        {
        }

        public DbSet<Produto> Produtos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da tabela Produto
            modelBuilder.Entity<Produto>(entity =>
            {
                entity.ToTable("Produtos");

                entity.HasKey(p => p.Id);

                entity.Property(p => p.Nome)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.Preco)
                    .HasColumnType("decimal(18,2)");

                entity.Property(p => p.Quantidade)
                    .IsRequired();
            });
        }
    }
}
