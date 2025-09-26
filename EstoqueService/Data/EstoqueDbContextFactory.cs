using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace EstoqueService.Data
{
    public class EstoqueDbContextFactory : IDesignTimeDbContextFactory<EstoqueDbContext>
    {
        public EstoqueDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EstoqueDbContext>();
            optionsBuilder.UseMySql(
                "server=localhost;port=3306;database=EstoqueDb;user=root;password=root8790;",
                new MySqlServerVersion(new Version(8, 0, 33))
            );

            return new EstoqueDbContext(optionsBuilder.Options);
        }
    }
}
