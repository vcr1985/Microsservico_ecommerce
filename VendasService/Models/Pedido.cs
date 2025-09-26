using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VendasService.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        
        [Required]
        public string ClienteId { get; set; } = string.Empty;
        
        [Required]
        public string Status { get; set; } = "Pendente";
        
        public decimal ValorTotal { get; set; }
        
        public DateTime DataPedido { get; set; } = DateTime.UtcNow;
        
        public List<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
    }

    public class ItemPedido
    {
        public int Id { get; set; }
        
        public int PedidoId { get; set; }
        
        [Required]
        public int ProdutoId { get; set; }
        
        [Required]
        public string NomeProduto { get; set; } = string.Empty;
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero")]
        public int Quantidade { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Preço deve ser maior que zero")]
        public decimal PrecoUnitario { get; set; }
        
        public decimal SubTotal => Quantidade * PrecoUnitario;
        
        // Navigation property
        public Pedido? Pedido { get; set; }
    }
}