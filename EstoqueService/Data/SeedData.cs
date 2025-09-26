using EstoqueService.Data;
using EstoqueService.Models;

public static class SeedData
{
    public static void Run(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();

        if (!context.Produtos.Any())
        {
            context.Produtos.AddRange(
                new Produto { Nome = "Café Arábica", Preco = 25.5M, Quantidade = 0 },
                new Produto { Nome = "Café Robusta", Preco = 18.9M, Quantidade = 0 },
                new Produto { Nome = "Café Especial", Preco = 32M, Quantidade = 0 }
            );
            context.SaveChanges();
        }
    }
}
