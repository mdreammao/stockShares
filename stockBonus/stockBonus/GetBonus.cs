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

        private int yesterday = Convert.ToInt32(DateTime.Now.AddDays(-1).ToString("yyyyMMdd"));

        static public SortedDictionary<string, stockBonus> bonusList;

        static public SortedDictionary<string, stockBonus> evaluateBonusList=new SortedDictionary<string, stockBonus>();




        public GetBonus()
        {
            if (bonusList==null)
            {
                GetBonusPlan();
                EvaluateBonus();
            }
            printBonus();

        }
        private void printBonus()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("代码", System.Type.GetType("System.String"));
            dt.Columns.Add("股票", System.Type.GetType("System.String"));
            dt.Columns.Add("分红时间", System.Type.GetType("System.String"));
            dt.Columns.Add("分红利息", System.Type.GetType("System.String"));
            dt.Columns.Add("备注", System.Type.GetType("System.String"));
            dt.Columns.Add("分红时间2", System.Type.GetType("System.String"));
            dt.Columns.Add("分红利息2", System.Type.GetType("System.String"));
            dt.Columns.Add("备注2", System.Type.GetType("System.String"));

            foreach (var item in evaluateBonusList)
            {
                stockBonus bonus = item.Value;
                DataRow row = dt.NewRow();
                row[0] = bonus.code;
                row[1] = bonus.name;
                row[2] = bonus.firstDate.ToString();
                row[3] = bonus.firstBonus.ToString("f4");
                row[4] = bonus.firstStatus;
                row[5] = bonus.secondDate.ToString();
                row[6] = bonus.secondBonus.ToString("f4");
                row[7] = bonus.secondStatus;
                dt.Rows.Add(row);
            }
            CsvApplication.SaveCSV(dt, "bonus.csv");
        }

        private void EvaluateBonus()
        {
            foreach (var item in GetStocks.stockList)
            {
                stockBonus bonus =bonusList[item.Key];
                List<int> dateList = new List<int>();
                List<double> historicalBonusList = new List<double>();
                WindData wd = w.wset("corporationaction", "startdate=2014-01-01;enddate=2020-06-20;windcode=" + bonus.code + ";field=ex_dividend_date,wind_code,sec_name,cash_payout_ratio,ex_dividend_note");
                object[] stockList = wd.data as object[];
                int num = (stockList==null?0:stockList.Length / 5);
                for (int i = 0; i < num; i++)
                {
                    string[] dateStr = Convert.ToString(stockList[i * 5]).Split(new char[] { '/', ' ' });
                    int date= Convert.ToInt32(dateStr[0]) * 10000 + Convert.ToInt32(dateStr[1]) * 100 + Convert.ToInt32(dateStr[2]);
                    double planBonus= (stockList[i * 5 + 3] == null ? 0 : (double)stockList[i * 5 + 3]);
                    dateList.Add(date);
                    historicalBonusList.Add(planBonus);
                }
                //判断去年是否有2次分红
                int frequency = 0;
                foreach (var date in dateList)
                {
                    if (date/10000+1==yesterday/10000)
                    {
                        frequency += 1;
                    }
                }
                if (frequency==1)//去年仅有1次分红，一般来说今年也是1次分红
                {
                    if (dateList[dateList.Count()-1]/10000==yesterday/10000) //今年分红已经明确
                    {
                        bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 1];
                        bonus.firstDate = dateList[dateList.Count() - 1];
                        if (TradeDays.GetNextTradeDay(yesterday)>=bonus.firstDate)
                        {
                            bonus.firstStatus = "分过了";
                        }
                        else
                        {
                            bonus.firstStatus = "明确";
                        }
                    }
                    if (dateList[dateList.Count() - 1] / 10000 +1 == yesterday / 10000) //今年分红未明确
                    {
                        bonus.firstBonus = bonus.planBonus;
                        bonus.firstDate = dateList[dateList.Count() - 1] + 10000;
                        bonus.firstStatus = "日期未明确";
                    }
                }
                if (frequency==0) //两种情况去年未分红，或者最近刚上市
                {
                    if (num>0) //以前有分红，判断是否亏损而没有分红预案
                    {
                        string str = (yesterday / 10000).ToString() + "-01-10";
                        WindData eps = w.wsd(bonus.code, "eps_ttm", "ED-5TD", str, "");
                        double [] epsList =eps.data as double[];
                        double myEps = epsList[0];
                        if (myEps<0)
                        {
                            bonus.firstStatus = "去年亏损";
                        }
                    }
                }
                if (frequency==2)  //年内分红具有两次的
                {
                    int frequencyThisYear = 0;
                    foreach (var date in dateList)
                    {
                        if (date / 10000  == yesterday / 10000)
                        {
                            frequencyThisYear += 1;
                        }
                    }
                    if (frequencyThisYear==2)  //今年第二次分红已明确
                    {
                        if (dateList[dateList.Count()-2]<=yesterday && dateList[dateList.Count() - 1]>yesterday)  //年内第一次分红已经分过但是第二次分红未分
                        {
                            bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 1];
                            bonus.firstDate = dateList[dateList.Count() - 1];
                            bonus.firstStatus = "明确";
                        }
                        if (dateList[dateList.Count() - 1] <= yesterday) //年内两次分红都分过了
                        {
                            bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 1];
                            bonus.firstDate = dateList[dateList.Count() - 1];
                            bonus.firstStatus = "分过了";
                        }
                        if (dateList[dateList.Count() - 2] > yesterday) //年内两次分红都没有分
                        {
                            bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 2];
                            bonus.firstDate = dateList[dateList.Count() - 2];
                            bonus.firstStatus = "明确";
                            bonus.secondBonus = historicalBonusList[historicalBonusList.Count() - 1];
                            bonus.secondDate = dateList[dateList.Count() - 1];
                            bonus.secondStatus = "明确";
                        }
                    }
                    else if(frequencyThisYear == 1) //第一次分红已明确，但第二次分红未明确
                    {
                        bonus.firstBonus = historicalBonusList[historicalBonusList.Count() - 1];
                        bonus.firstDate = dateList[dateList.Count() - 1];
                        bonus.firstStatus = "明确";
                        bonus.secondBonus = bonus.planBonus;
                        bonus.secondDate = dateList[dateList.Count() - 2] + 10000;
                        bonus.secondStatus = "日期未明确";
                    }
                }
                if (bonus.firstStatus != null && bonus.firstBonus == 0 && num > 0)  //预测年内第一次分红
                {
                    string str = (yesterday / 10000).ToString() + "-01-10";
                    WindData eps = w.wsd(bonus.code, "eps_ttm", "ED-5TD", str, "");
                    double[] epsList = eps.data as double[];
                    double myEpsThisYear = epsList[0];
                    str = (yesterday / 10000-1).ToString() + "-01-10";
                    eps = w.wsd(bonus.code, "eps_ttm", "ED-5TD", str, "");
                    epsList = eps.data as double[];
                    double myEpsLastYear = epsList[0];
                    for (int i = 0; i < dateList.Count - 1; i++)  //去年第一次分红
                    {
                        if (dateList[i] / 10000 + 1 == yesterday / 10000)
                        {
                            if (bonus.firstDate == 0)
                            {
                                bonus.firstDate = dateList[i] + 10000;
                            }
                            bonus.firstBonus = historicalBonusList[i] / myEpsLastYear * myEpsThisYear;
                            bonus.firstStatus = "预测";
                            break;
                        }
                    }
                }
                if (bonus.secondStatus != null && bonus.secondBonus == 0 && num > 0)  //预测年内第二次分红
                {
                    string str = (yesterday / 10000).ToString() + "-01-10";
                    WindData eps = w.wsd(bonus.code, "eps_ttm", "ED-5TD", str, "");
                    double[] epsList = eps.data as double[];
                    double myEpsThisYear = epsList[0];
                    str = (yesterday / 10000 - 1).ToString() + "-01-10";
                    eps = w.wsd(bonus.code, "eps_ttm", "ED-5TD", str, "");
                    epsList = eps.data as double[];
                    double myEpsLastYear = (epsList==null?0:epsList[0]);
                    int times = 0;
                    if (myEpsLastYear==0) //没有去年数据，去年刚上市
                    {
                        for (int i = 0; i < dateList.Count - 1; i++)  //去年第一次分红
                        {
                            if (dateList[i] / 10000 + 1 == yesterday / 10000 && times == 0)
                            {
                                times += 1;
                                continue;
                            }
                            if (dateList[i] / 10000 + 1 == yesterday / 10000 && times == 1)
                            {
                                if (bonus.secondDate == 0)
                                {
                                    bonus.secondDate = dateList[i] + 10000;
                                }
                                bonus.secondBonus = historicalBonusList[i] / historicalBonusList[i-1]*historicalBonusList[i+1];
                                bonus.secondStatus = "预测";
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < dateList.Count - 1; i++)  //去年第一次分红
                        {
                            if (dateList[i] / 10000 + 1 == yesterday / 10000 && times == 0)
                            {
                                times += 1;
                                continue;
                            }
                            if (dateList[i] / 10000 + 1 == yesterday / 10000 && times == 1)
                            {
                                if (bonus.secondDate == 0)
                                {
                                    bonus.secondDate = dateList[i] + 10000;
                                }
                                bonus.secondBonus = historicalBonusList[i] / myEpsLastYear * myEpsThisYear;
                                bonus.secondStatus = "预测";
                                break;
                            }
                        }
                    }
                }
                evaluateBonusList.Add(bonus.code, bonus);
            }
        }

        private void GetBonusPlan()
        {
            bonusList = new SortedDictionary<string, stockBonus>();
            w.start();
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
