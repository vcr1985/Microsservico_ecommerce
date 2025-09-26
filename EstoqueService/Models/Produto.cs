using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EstoqueService.Models
{
    public class Produto
    {
        public int Id {get;set;}
        public string Nome {get; set;}= string.Empty;
        public string Descricao {get; set;}= string.Empty;
        public decimal Preco {get; set;}
        public int Quantidade {get; set;}
    }
}