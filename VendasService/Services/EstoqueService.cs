using System.Text.Json;

namespace VendasService.Services
{
    public interface IEstoqueService
    {
        Task<ProdutoDto?> GetProdutoAsync(int produtoId);
        Task<bool> ValidarEstoqueAsync(int produtoId, int quantidade);
    }

    public class EstoqueService : IEstoqueService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EstoqueService> _logger;

        public EstoqueService(HttpClient httpClient, ILogger<EstoqueService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ProdutoDto?> GetProdutoAsync(int produtoId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/produtos/{produtoId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ProdutoDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                
                _logger.LogWarning("Falha ao buscar produto {ProdutoId}. Status: {StatusCode}", 
                    produtoId, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produto {ProdutoId}", produtoId);
                return null;
            }
        }

        public async Task<bool> ValidarEstoqueAsync(int produtoId, int quantidade)
        {
            var produto = await GetProdutoAsync(produtoId);
            return produto != null && produto.Quantidade >= quantidade;
        }
    }

    public class ProdutoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public int Quantidade { get; set; }
    }
}