using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shared.Messages
{
    public class VendaRealizadaMessage
    {
        public int ProdutoId{ get;set;}
        public int QuantidadeVendida {get; set;}
        public DateTime DataVenda {get; set;} = DateTime.UtcNow;
    }
}