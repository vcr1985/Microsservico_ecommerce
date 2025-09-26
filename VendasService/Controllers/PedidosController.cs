using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendasService.Data;
using VendasService.Models;
using VendasService.Services;
using Shared.Messages;
using System.ComponentModel.DataAnnotations;

namespace VendasService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PedidosController : ControllerBase
    {
        private readonly VendasContext _context;
        private readonly IEstoqueService _estoqueService;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;
        private readonly ILogger<PedidosController> _logger;

        public PedidosController(
            VendasContext context,
            IEstoqueService estoqueService,
            IRabbitMQPublisher rabbitMQPublisher,
            ILogger<PedidosController> logger)
        {
            _context = context;
            _estoqueService = estoqueService;
            _rabbitMQPublisher = rabbitMQPublisher;
            _logger = logger;
        }

        // GET: api/pedidos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidos()
        {
            try
            {
                var pedidos = await _context.Pedidos
                    .Include(p => p.Itens)
                    .OrderByDescending(p => p.DataPedido)
                    .ToListAsync();

                return Ok(pedidos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar pedidos");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        // GET: api/pedidos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Pedido>> GetPedido(int id)
        {
            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Itens)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (pedido == null)
                {
                    return NotFound($"Pedido com ID {id} não encontrado");
                }

                return Ok(pedido);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar pedido {PedidoId}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        // POST: api/pedidos
        [HttpPost]
        public async Task<ActionResult<Pedido>> CreatePedido([FromBody] CriarPedidoRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Validar estoque de todos os produtos
                var pedido = new Pedido
                {
                    ClienteId = request.ClienteId,
                    Status = "Processando",
                    DataPedido = DateTime.UtcNow,
                    Itens = new List<ItemPedido>()
                };

                decimal valorTotal = 0;

                foreach (var item in request.Itens)
                {
                    // Buscar produto no serviço de estoque
                    var produto = await _estoqueService.GetProdutoAsync(item.ProdutoId);
                    
                    if (produto == null)
                    {
                        return BadRequest($"Produto com ID {item.ProdutoId} não encontrado");
                    }

                    // Validar estoque
                    if (produto.Quantidade < item.Quantidade)
                    {
                        return BadRequest($"Estoque insuficiente para o produto {produto.Nome}. Disponível: {produto.Quantidade}, Solicitado: {item.Quantidade}");
                    }

                    var itemPedido = new ItemPedido
                    {
                        ProdutoId = item.ProdutoId,
                        NomeProduto = produto.Nome,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = produto.Preco
                    };

                    pedido.Itens.Add(itemPedido);
                    valorTotal += itemPedido.SubTotal;
                }

                pedido.ValorTotal = valorTotal;

                // Salvar pedido
                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // Confirmar pedido e atualizar estoque
                pedido.Status = "Confirmado";
                await _context.SaveChangesAsync();

                // Publicar mensagem para atualizar estoque
                foreach (var item in pedido.Itens)
                {
                    var message = new VendaRealizadaMessage
                    {
                        ProdutoId = item.ProdutoId,
                        QuantidadeVendida = item.Quantidade,
                        DataVenda = pedido.DataPedido
                    };

                    await _rabbitMQPublisher.PublishAsync(message, "venda-realizada");
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Pedido {PedidoId} criado com sucesso para cliente {ClienteId}", 
                    pedido.Id, pedido.ClienteId);

                return CreatedAtAction(nameof(GetPedido), new { id = pedido.Id }, pedido);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao criar pedido para cliente {ClienteId}", request.ClienteId);
                return StatusCode(500, "Erro ao processar pedido");
            }
        }

        // PUT: api/pedidos/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdatePedidoStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var pedido = await _context.Pedidos.FindAsync(id);
                
                if (pedido == null)
                {
                    return NotFound($"Pedido com ID {id} não encontrado");
                }

                pedido.Status = request.NovoStatus;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Status do pedido {PedidoId} alterado para {NovoStatus}", id, request.NovoStatus);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status do pedido {PedidoId}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        // GET: api/pedidos/cliente/{clienteId}
        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidosByCliente(string clienteId)
        {
            try
            {
                var pedidos = await _context.Pedidos
                    .Include(p => p.Itens)
                    .Where(p => p.ClienteId == clienteId)
                    .OrderByDescending(p => p.DataPedido)
                    .ToListAsync();

                return Ok(pedidos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar pedidos do cliente {ClienteId}", clienteId);
                return StatusCode(500, "Erro interno do servidor");
            }
        }
    }

    public class CriarPedidoRequest
    {
        [Required]
        public string ClienteId { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "Pedido deve conter ao menos um item")]
        public List<ItemPedidoRequest> Itens { get; set; } = new();
    }

    public class ItemPedidoRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ID do produto é obrigatório")]
        public int ProdutoId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero")]
        public int Quantidade { get; set; }
    }

    public class UpdateStatusRequest
    {
        [Required]
        public string NovoStatus { get; set; } = string.Empty;
    }
}