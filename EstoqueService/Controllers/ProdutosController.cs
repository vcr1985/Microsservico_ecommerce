using EstoqueService.Data;
using EstoqueService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EstoqueService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProdutosController : ControllerBase
    {
        private readonly EstoqueDbContext _context;
        private readonly ILogger<ProdutosController> _logger;

        public ProdutosController(EstoqueDbContext context, ILogger<ProdutosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Produtos
        [HttpGet]
        public async Task<IActionResult> GetProdutos()
        {
            try
            {
                var produtos = await _context.Produtos.ToListAsync();
                return Ok(produtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produtos");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        // GET: api/Produtos/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduto(int id)
        {
            try
            {
                var produto = await _context.Produtos.FindAsync(id);
                if (produto == null)
                {
                    return NotFound($"Produto com ID {id} não encontrado");
                }
                return Ok(produto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produto {ProdutoId}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        // POST: api/Produtos
        [HttpPost]
        public async Task<IActionResult> CreateProduto([FromBody] CriarProdutoRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var produto = new Produto
                {
                    Nome = request.Nome,
                    Descricao = request.Descricao,
                    Preco = request.Preco,
                    Quantidade = request.Quantidade
                };

                _context.Produtos.Add(produto);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Produto {ProdutoId} criado com sucesso: {Nome}", produto.Id, produto.Nome);

                return CreatedAtAction(nameof(GetProduto), new { id = produto.Id }, produto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar produto {Nome}", request.Nome);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        // PUT: api/Produtos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduto(int id, [FromBody] AtualizarProdutoRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingProduto = await _context.Produtos.FindAsync(id);
                if (existingProduto == null)
                {
                    return NotFound($"Produto com ID {id} não encontrado");
                }

                existingProduto.Nome = request.Nome;
                existingProduto.Descricao = request.Descricao;
                existingProduto.Preco = request.Preco;
                existingProduto.Quantidade = request.Quantidade;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Produto {ProdutoId} atualizado com sucesso", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar produto {ProdutoId}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        // DELETE: api/Produtos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduto(int id)
        {
            try
            {
                var produto = await _context.Produtos.FindAsync(id);
                if (produto == null)
                {
                    return NotFound($"Produto com ID {id} não encontrado");
                }

                _context.Produtos.Remove(produto);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Produto {ProdutoId} removido com sucesso", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover produto {ProdutoId}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        // PATCH: api/Produtos/5/estoque
        [HttpPatch("{id}/estoque")]
        public async Task<IActionResult> AtualizarEstoque(int id, [FromBody] AtualizarEstoqueRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var produto = await _context.Produtos.FindAsync(id);
                if (produto == null)
                {
                    return NotFound($"Produto com ID {id} não encontrado");
                }

                if (request.Operacao == "reduzir")
                {
                    if (produto.Quantidade < request.Quantidade)
                    {
                        return BadRequest($"Estoque insuficiente. Disponível: {produto.Quantidade}, Solicitado: {request.Quantidade}");
                    }
                    produto.Quantidade -= request.Quantidade;
                }
                else if (request.Operacao == "adicionar")
                {
                    produto.Quantidade += request.Quantidade;
                }
                else
                {
                    return BadRequest("Operação deve ser 'adicionar' ou 'reduzir'");
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Estoque do produto {ProdutoId} atualizado. Nova quantidade: {Quantidade}", 
                    id, produto.Quantidade);

                return Ok(new { NovaQuantidade = produto.Quantidade });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar estoque do produto {ProdutoId}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }
    }

    public class CriarProdutoRequest
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Descrição deve ter no máximo 1000 caracteres")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Preço é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Preço deve ser maior que zero")]
        public decimal Preco { get; set; }

        [Required(ErrorMessage = "Quantidade é obrigatória")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantidade deve ser zero ou positiva")]
        public int Quantidade { get; set; }
    }

    public class AtualizarProdutoRequest
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Descrição deve ter no máximo 1000 caracteres")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Preço é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Preço deve ser maior que zero")]
        public decimal Preco { get; set; }

        [Required(ErrorMessage = "Quantidade é obrigatória")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantidade deve ser zero ou positiva")]
        public int Quantidade { get; set; }
    }

    public class AtualizarEstoqueRequest
    {
        [Required(ErrorMessage = "Operação é obrigatória")]
        [RegularExpression("^(adicionar|reduzir)$", ErrorMessage = "Operação deve ser 'adicionar' ou 'reduzir'")]
        public string Operacao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantidade é obrigatória")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero")]
        public int Quantidade { get; set; }
    }
}
