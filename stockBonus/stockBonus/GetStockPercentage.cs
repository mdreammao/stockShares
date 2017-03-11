using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WAPIWrapperCSharp;

namespace stockBonus
{
    class GetStockPercentage
    {
        WindAPI w = new WindAPI();
        public List<stockFormat> nowStockList = new List<stockFormat>();
        public List<stockFormat> tomorrowStockList = new List<stockFormat>();
        static public SortedDictionary<string, stockEquity> equityList;
        private string indexName;
        private int yesterday = TradeDays.GetPreviousTradeDay(Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd")));
        public GetStockPercentage(string indexName)
        {
            this.indexName = indexName;
            if (equityList==null)
            {
                equityList = GetStockData();
            }
        }

        private SortedDictionary<string, stockEquity> GetStockData()
        {
           SortedDictionary<string, stockEquity> myData = new SortedDictionary<string, stockEquity>();
            string yesterdayStr = DateTime.ParseExact(yesterday.ToString(), "yyyyMMdd", null).ToString("yyyy-MM-dd");
            string firstDate = "2016-01-01";
            foreach (var item in GetStocks.stockList)
            {
                string code = item.Key;
                WindData wd = w.wsd(code, "close,free_float_shares,float_a_shares", "ED-0D", firstDate, "Days=Alldays");
                double[] stockList = wd.data as double[];
                int num = (stockList == null ? 0 : stockList.Length / 3);
                if (num==1)
                {
                    stockEquity myEquity = new stockEquity();
                    myEquity.code = item.Value.code;
                    myEquity.name = item.Value.name;
                    myEquity.date = yesterday;
                    myEquity.closePrice = (double)stockList[0];
                    myEquity.freeEquity = (double)stockList[1];
                    myEquity.equity = (double)stockList[2];
                    double percentage = myEquity.freeEquity / myEquity.equity;
                    if (percentage <= 0.15)
                    {
                        myEquity.percentage = Math.Ceiling(percentage * 100) / 100;
                    }
                    else if (percentage <= 0.2)
                    {
                        myEquity.percentage = 0.2;
                    }
                    else if (percentage <= 0.8)
                    {
                        myEquity.percentage = Math.Ceiling(percentage * 10) / 10;
                    }
                    else
                    {
                        myEquity.percentage = 1;
                    }
                    myData.Add(code, myEquity);
                }
                else
                {
                    Console.WriteLine("There is something wrong with {0}", code);
                }
            }

            WindData wd2 = w.wsd(indexName, "close,free_float_shares,float_a_shares", "ED-0D", firstDate, "Days=Alldays");
            double[] stockList2 = wd2.data as double[];
             double num2 = (stockList2 == null ? 0 : stockList2.Length / 3);
            if (num2 == 1)
            {
                stockEquity myEquity = new stockEquity();
                myEquity.code = indexName;
                myEquity.name = indexName;
                myEquity.date = yesterday;
                myEquity.closePrice = (double)stockList2[0];
                myEquity.freeEquity = (double)stockList2[1];
                myEquity.equity = (double)stockList2[2];
                double percentage = myEquity.freeEquity / myEquity.equity;
                if (percentage <= 0.15)
                {
                    myEquity.percentage = Math.Ceiling(percentage * 100) / 100;
                }
                else if (percentage <= 0.2)
                {
                    myEquity.percentage = 0.2;
                }
                else if (percentage <= 0.8)
                {
                    myEquity.percentage = Math.Ceiling(percentage * 10) / 10;
                }
                else
                {
                    myEquity.percentage = 1;
                }
                myData.Add(indexName, myEquity);
            }
            else
            {
                Console.WriteLine("There is something wrong with {0}", indexName);
            }
            return  myData;
        }
    }
}
