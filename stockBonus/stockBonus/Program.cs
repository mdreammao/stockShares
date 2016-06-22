using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stockBonus
{
    class Program
    {
        static void Main(string[] args)
        {
            GetStocks myList = new GetStocks(20160601,20161231,"000016.SH");
            GetBonus bonus = new GetBonus("000016.SH");
        }
    }
}
