using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WAPIWrapperCSharp;
using System.Data.SqlClient;
using System.Data;

namespace stockBonus
{
    /// <summary>
    /// 根据给定的时间段和股票，给出分红的日期和股息(包括预测的和已公告的）
    /// </summary>
    class GetBonus
    {
        /// <summary>
        /// 万德接口类实例。
        /// </summary>
        static private WindAPI w = new WindAPI();

        static private TradeDays myTradeDays = new TradeDays(20150101, 20171231);

        private int yesterday =TradeDays.GetPreviousTradeDay(Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd")));
        //private int yesterday = 20170103;
        static public SortedDictionary<string, stockBonus> bonusList;

        static public SortedDictionary<string, stockBonus> evaluateBonusList=new SortedDictionary<string, stockBonus>();

        private SortedDictionary<string, stockBonus> evaluatePointList = new SortedDictionary<string, stockBonus>();

        private string indexName;


        public GetBonus(string indexName)
        {
            this.indexName = indexName;
            if (bonusList==null)
            {
                GetBonusPlan();
                EvaluateBonus();
            }
            GetStockPercentage equity = new GetStockPercentage(indexName);
            EvaluatePoint();
            PrintBonus();

        }
        private void EvaluatePoint()
        {
            SortedDictionary<string, stockEquity> equityList = GetStockPercentage.equityList;
            List<string> myKeys = new List<string>();
            foreach (var key in evaluateBonusList)
            {
                myKeys.Add(key.Key);
            }
            foreach (var item in myKeys)
            {
                stockBonus bonus = evaluateBonusList[item];
                bool influence = false;
                if ((bonus.firstDate>yesterday || bonus.firstDate==0) && bonus.firstBonus>0 && GetStocks.stockList[item].existsDate[GetStocks.stockList[item].existsDate.Count() - 1] > yesterday)
                {
                    List<stockFormat> list = GetStocks.getConstituentStock(bonus.firstDate);
                    if (bonus.firstDate == 0)
                    {
                        list = GetStocks.getConstituentStock(GetStocks.stockList[item].existsDate[GetStocks.stockList[item].existsDate.Count() - 1]);
                    }
                    bonus.firstPoint = GetStockPoint(list, equityList, bonus.code,bonus.firstBonus);
                    influence = true;
                }
                if ((bonus.secondDate>yesterday|| bonus.secondDate==0) && bonus.secondBonus>0 && GetStocks.stockList[item].existsDate[GetStocks.stockList[item].existsDate.Count() - 1] > yesterday)
                {
                    List<stockFormat> list = GetStocks.getConstituentStock(bonus.secondDate);
                    if (bonus.secondDate == 0)
                    {
                        list = GetStocks.getConstituentStock(GetStocks.stockList[item].existsDate[GetStocks.stockList[item].existsDate.Count() - 1]);
                    }
                    bonus.secondPoint = GetStockPoint(list, equityList,bonus.code, bonus.secondBonus);
                    influence = true;
                }
                if (influence==true)
                {
                    evaluatePointList.Add(bonus.code, bonus);
                }
            }

        }

        private double GetStockPoint(List<stockFormat> list, SortedDictionary<string, stockEquity> equityList,string code,double bonus)
        {
            double total = 0;
            foreach (var item in list)
            {
                total += equityList[item.code].percentage * equityList[item.code].equity * equityList[item.code].closePrice;
            }
            double divisor = total/equityList[indexName].closePrice;
            double point = equityList[code].percentage * equityList[code].equity*bonus/divisor;
            return point;
        }

        private void PrintBonus()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("代码", System.Type.GetType("System.String"));
            dt.Columns.Add("股票", System.Type.GetType("System.String"));
            dt.Columns.Add("股权登记日", System.Type.GetType("System.String"));
            dt.Columns.Add("分红时间", System.Type.GetType("System.String"));
            dt.Columns.Add("分红利息", System.Type.GetType("System.String"));
            dt.Columns.Add("分红点数", System.Type.GetType("System.String"));
            dt.Columns.Add("备注", System.Type.GetType("System.String"));
         

            foreach (var item in evaluatePointList)
            {
                stockBonus bonus = item.Value;
                if ((bonus.firstDate > yesterday || bonus.firstDate == 0) && bonus.firstBonus > 0 && GetStocks.stockList[item.Key].existsDate[GetStocks.stockList[item.Key].existsDate.Count()-1]>yesterday)
                {
                    DataRow row = dt.NewRow();
                    row[0] = bonus.code;
                    row[1] = bonus.name;
                    row[2] = bonus.firstRegisterDate.ToString();
                    row[3] = bonus.firstDate.ToString();
                    row[4] = bonus.firstBonus.ToString("f4");
                    row[5] = bonus.firstPoint.ToString("f4");
                    row[6] = bonus.firstStatus;
                    dt.Rows.Add(row);
                }
                if ((bonus.secondDate > yesterday || bonus.secondDate == 0) && bonus.secondBonus > 0 && GetStocks.stockList[item.Key].existsDate[GetStocks.stockList[item.Key].existsDate.Count() - 1] > yesterday)
                {
                    DataRow row = dt.NewRow();
                    row[0] = bonus.code;
                    row[1] = bonus.name;
                    row[2] = bonus.SecondRegisterDate.ToString();
                    row[3] = bonus.secondDate.ToString();
                    row[4] = bonus.secondBonus.ToString("f4");
                    row[5] = bonus.secondPoint.ToString("f4");
                    row[6] = bonus.secondStatus;
                    dt.Rows.Add(row);
                }
            }
            CsvApplication.SaveCSV(dt, "bonus"+yesterday.ToString()+".csv");
        }

        private void EvaluateBonus()
        {
            //利用万德wset的“分红送转”抓取数据
            foreach (var item in GetStocks.stockList)
            {
                stockBonus bonus = bonusList[item.Key];
                List<int> dateList = new List<int>();
                List<double> historicalBonusList = new List<double>();
                WindData wd = w.wset("corporationaction", "startdate=2014-01-01;enddate=2020-06-20;windcode=" + bonus.code + ";field=ex_dividend_date,wind_code,sec_name,cash_payout_ratio,ex_dividend_note");
                object[] stockList = wd.data as object[];
                int num = (stockList == null ? 0 : stockList.Length / 5);
                for (int i = 0; i < num; i++)
                {
                    string[] dateStr = Convert.ToString(stockList[i * 5]).Split(new char[] { '/', ' ' });
                    int date = Convert.ToInt32(dateStr[0]) * 10000 + Convert.ToInt32(dateStr[1]) * 100 + Convert.ToInt32(dateStr[2]);
                    double planBonus = (stockList[i * 5 + 3] == null ? 0 : (double)stockList[i * 5 + 3]);
                    dateList.Add(date);
                    historicalBonusList.Add(planBonus);
                }
                //判断去年是否有2次分红
                int thisYear=0,lastYear = 0;
                for (int i = 0; i < dateList.Count; i++)
                {
                    int date = dateList[i];
                    double myBonus = historicalBonusList[i];
                    if (date / 10000 + 1 == yesterday / 10000  && myBonus>0)
                    {
                        lastYear += 1;
                    }
                    if (date / 10000 == yesterday / 10000 && myBonus > 0)
                    {
                        thisYear += 1;
                    }
                }
                if (lastYear==1 &&　thisYear==1)
                {
                    bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 1];
                    bonus.firstDate = dateList[dateList.Count() - 1];
                    bonus.firstStatus = "明确";
                }
                if (lastYear==1 && thisYear==0)
                {
                    if (bonus.planBonus!=0)
                    {
                        bonus.firstBonus = bonus.planBonus;
                        bonus.firstDate =TradeDays.GetRecentTradeDay(dateList[dateList.Count() - 1] + 10000);
                        bonus.firstStatus = "有预案但日期未明确";
                    }
                    else
                    {
                        
                        string str = DateTime.ParseExact(yesterday.ToString(), "yyyyMMdd", null).ToString("yyyy-MM-dd");
                        WindData eps = w.wsd(bonus.code, "eps_ttm", "ED-0TD", str, "Days=Alldays");//利用EPS来估算分红
                        double[] epsList = eps.data as double[];
                        double thisEps = epsList[0];
                        if (thisEps < 0)
                        {
                            bonus.firstStatus = "去年亏损无分红";
                            bonus.firstBonus = 0;
                            bonus.firstDate = 0;
                        }
                        else
                        {
                            str = DateTime.ParseExact((yesterday -10000).ToString(), "yyyyMMdd", null).ToString("yyyy-MM-dd");
                            eps = w.wsd(bonus.code, "eps_ttm", "ED-0TD", str, "Days=Alldays");
                            epsList = eps.data as double[];
                            double lastEps = (epsList==null?0:epsList[0]);
                            if (lastEps==0)
                            {
                                bonus.firstStatus = "去年无EPS数据无法预测";
                                bonus.firstBonus = 0;
                                bonus.firstDate = 0;
                            }
                            else
                            {
                                bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 1] / lastEps * thisEps;
                                bonus.firstDate =TradeDays.GetRecentTradeDay(dateList[dateList.Count() - 1] + 10000);
                                bonus.firstStatus = "无预案按上次分红预测";
                            }
                        }
                    }
                }
                if (lastYear==2 && thisYear==2)
                {
                    bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 2];
                    bonus.firstDate = dateList[dateList.Count() - 2];
                    bonus.firstStatus = "明确";
                    bonus.secondBonus = historicalBonusList[historicalBonusList.Count() - 1];
                    bonus.secondDate = dateList[dateList.Count() - 1];
                    bonus.secondStatus = "明确";
                }
                if (lastYear==2 && thisYear==1)
                {
                    bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 1];
                    bonus.firstDate = dateList[dateList.Count() - 1];
                    bonus.firstStatus = "明确";
                    if (bonus.planBonus != 0)
                    {
                        bonus.secondBonus = bonus.planBonus;
                        bonus.secondDate =TradeDays.GetRecentTradeDay(dateList[dateList.Count() - 2] + 10000);
                        bonus.secondStatus = "有预案但日期未明确";
                    }
                    else
                    {
                        bonus.secondDate =TradeDays.GetRecentTradeDay(dateList[dateList.Count() - 2] + 10000);
                        bonus.secondStatus = "无预案按上次分红预测";
                        bonus.secondBonus = historicalBonusList[historicalBonusList.Count() - 2] / historicalBonusList[historicalBonusList.Count() - 3] * historicalBonusList[historicalBonusList.Count() - 1];
                    }
                }
                if (lastYear==2 && thisYear==0)
                {
                    if (bonus.planBonus != 0)
                    {
                        bonus.firstBonus = bonus.planBonus;
                        bonus.firstDate =TradeDays.GetRecentTradeDay(dateList[dateList.Count() - 2] + 10000);
                        bonus.firstStatus = "有预案但日期未明确";
                        bonus.secondDate =TradeDays.GetRecentTradeDay(dateList[dateList.Count() - 1] + 10000);
                        bonus.secondStatus = "无预案按上次分红预测";
                        bonus.secondBonus = historicalBonusList[historicalBonusList.Count() - 1] / historicalBonusList[historicalBonusList.Count() - 2] * bonus.planBonus;
                    }
                    else
                    {
                        string str = DateTime.ParseExact(yesterday.ToString(), "yyyyMMdd", null).ToString("yyyy-MM-dd");
                        WindData eps = w.wsd(bonus.code, "eps_ttm", "ED-0TD", str, "Days=Alldays");
                        double[] epsList = eps.data as double[];
                        double thisEps = epsList[0];
                        if (thisEps < 0)
                        {
                            bonus.firstStatus = "去年亏损无分红";
                            bonus.firstBonus = 0;
                            bonus.firstDate = 0;
                            bonus.secondStatus = "去年亏损无分红";
                            bonus.secondBonus = 0;
                            bonus.secondDate = 0;
                        }
                        else
                        {
                            str = DateTime.ParseExact((yesterday - 10000).ToString(), "yyyyMMdd", null).ToString("yyyy-MM-dd");
                            eps = w.wsd(bonus.code, "eps_ttm", "ED-0TD", str, "Days=Alldays");
                            epsList = eps.data as double[];
                            double lastEps = (epsList == null ? 0 : epsList[0]);
                            if (lastEps == 0)
                            {
                                bonus.firstStatus = "去年无EPS数据无法预测";
                                bonus.firstBonus = 0;
                                bonus.firstDate = 0;
                                bonus.secondStatus = "去年无EPS数据无法预测";
                                bonus.secondBonus = 0;
                                bonus.secondDate = 0;
                            }
                            else
                            {
                                bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 2] / lastEps * thisEps;
                                bonus.firstDate =TradeDays.GetRecentTradeDay(dateList[dateList.Count() - 2] + 10000);
                                bonus.firstStatus = "无预案按上次分红预测";
                                bonus.secondBonus = historicalBonusList[historicalBonusList.Count() - 1] / lastEps * thisEps;
                                bonus.secondDate =TradeDays.GetRecentTradeDay(dateList[dateList.Count() - 1] + 10000);
                                bonus.secondStatus = "无预案按上次分红预测";
                            }
                        }
                    }
                }
                if (lastYear==0&& thisYear==0)
                {
                    if (bonus.planBonus > 0)
                    {
                        bonus.firstBonus = bonus.planBonus;
                        bonus.firstDate = 0;
                        bonus.firstStatus = "有预案但日期未明确";
                    }
                    else
                    {
                        bonus.firstStatus = "无预案无分红数据";
                    }
                }
                if (lastYear==0 && thisYear==1)
                {
                    bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 1];
                    bonus.firstDate = dateList[dateList.Count() - 1];
                    bonus.firstStatus = "明确";
                    if (bonus.planBonus>0)
                    {
                        bonus.secondBonus = bonus.planBonus;
                        bonus.secondDate = 0;
                        bonus.secondStatus = "有预案但日期未明确";
                    }
                }
                if (lastYear==0 &&　thisYear==2)
                {
                    bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 2];
                    bonus.firstDate = dateList[dateList.Count() - 2];
                    bonus.firstStatus = "明确";
                    bonus.secondBonus = historicalBonusList[historicalBonusList.Count() - 1];
                    bonus.secondDate = dateList[dateList.Count() - 1];
                    bonus.secondStatus = "明确";
                }
                if (bonus.firstStatus== "有预案但日期未明确" || bonus.firstStatus == "无预案按上次分红预测")
                {
                    if (bonus.firstDate<=yesterday)
                    {
                        bonus.firstDate = 0;
                        bonus.firstStatus += "预测日期已过";
                    }
                }
                if (bonus.secondStatus == "有预案但日期未明确" || bonus.secondStatus == "无预案按上次分红预测")
                {
                    if (bonus.secondDate <= yesterday)
                    {         
                        bonus.secondDate = 0;
                        bonus.secondStatus += "预测日期已过";
                    }
                }
                evaluateBonusList.Add(bonus.code, bonus);
            }

            //预处理，默认除息除权日是分红的前一天
            foreach (var item in GetStocks.stockList)
            {
                stockBonus bonus = evaluateBonusList[item.Key];
                
                if (bonus.firstDate > 0)
                {
                    bonus.firstRegisterDate = TradeDays.GetPreviousTradeDay(bonus.firstDate);
                }
                if (bonus.secondDate > 0)
                {
                    bonus.SecondRegisterDate = TradeDays.GetPreviousTradeDay(bonus.secondDate);
                }
                evaluateBonusList[item.Key] = bonus;
            }


                //利用万德接口wset的“分红实施”来获取股权登记日
             string lastYearStr = (yesterday / 10000 - 1).ToString();
            WindData register = w.wset("bonus", "orderby=报告期;year="+lastYearStr+";period=y1;sectorid=a001010100000000;field=wind_code,sec_name,shareregister_date,dividend_payment_date");
            object[] stockList2 = register.data as object[];
            int num2 = (stockList2==null?0:stockList2.Length / 4);
            for (int i = 0; i < num2; i++)
            {
                string code= Convert.ToString(stockList2[i * 4]);
                if (evaluateBonusList.ContainsKey(code))
                {
                    string[] date = Convert.ToString(stockList2[i * 4 + 3]).Split(new char[] { '/', ' ' });
                    int planDate = Convert.ToInt32(date[0]) * 10000 + Convert.ToInt32(date[1]) * 100 + Convert.ToInt32(date[2]);
                    stockBonus bonus = evaluateBonusList[code];
                    if (planDate==bonus.firstDate)
                    {
                        date = Convert.ToString(stockList2[i * 4 + 2]).Split(new char[] { '/', ' ' });
                        bonus.firstRegisterDate= Convert.ToInt32(date[0]) * 10000 + Convert.ToInt32(date[1]) * 100 + Convert.ToInt32(date[2]);
                    }
                    if(planDate == bonus.secondDate)
                    {
                        date = Convert.ToString(stockList2[i * 4 + 2]).Split(new char[] { '/', ' ' });
                        bonus.SecondRegisterDate = Convert.ToInt32(date[0]) * 10000 + Convert.ToInt32(date[1]) * 100 + Convert.ToInt32(date[2]);
                    }
                    evaluateBonusList[code] = bonus;
                }
            }
        }


        private void GetBonusPlan()
        {
            bonusList = new SortedDictionary<string, stockBonus>();
            w.start();
            //利用万德wset的“分红预案”抓取数据
            WindData wd = w.wset("dividendproposal", "ordertype=1;startdate=2015-06-30;enddate=2020-12-31;sectorid=a001010100000000;field=wind_code,sec_name,progress,cash_dividend,fellow_preplandate");
            object[] stockList = wd.data as object[];
            int num = stockList.Length / 5;
            for (int i = 0; i < num; i++)
            {
                stockBonus myBonus = new stockBonus();
                myBonus.code = Convert.ToString(stockList[i * 5 ]);
                myBonus.name = (string)stockList[i * 5 + 1];
                myBonus.progress= (string)stockList[i * 5 + 2];
                myBonus.planBonus=(stockList[i * 5 + 3]==null? 0:(double)stockList[i * 5 + 3]);
                string[] date= Convert.ToString(stockList[i * 5 + 4]).Split(new char[] { '/',' ' });
                myBonus.planDate = Convert.ToInt32(date[0]) * 10000 + Convert.ToInt32(date[1]) * 100 + Convert.ToInt32(date[2]);
                if (TradeDays.GetTimeSpan(myBonus.planDate, yesterday) <= 125 && myBonus.code.Substring(7,2)=="SH")
                {
                    if (bonusList.ContainsKey(myBonus.code) == false)
                    {
                        bonusList.Add(myBonus.code, myBonus);
                    }
                    else if (TradeDays.GetTimeSpan(myBonus.planDate, yesterday)<TradeDays.GetTimeSpan(bonusList[myBonus.code].planDate,yesterday))
                    {
                        bonusList[myBonus.code] = myBonus;
                    }
                }
            }
            foreach (var item in GetStocks.stockList)
            {
                string code = item.Key;
                if (bonusList.ContainsKey(code)==false)
                {
                    stockBonus myBonus = new stockBonus();
                    myBonus.code = item.Value.code;
                    myBonus.name = item.Value.name;
                    myBonus.progress = "没有分红预案";
                    bonusList.Add(myBonus.code,myBonus);
                }
            }
        }
    }
}
