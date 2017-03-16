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
            GetStocks myList = new GetStocks(20170301,20171229,"000016.SH");
            GetBonus bonus = new GetBonus("000016.SH");
        }
    }
}
