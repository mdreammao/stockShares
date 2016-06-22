using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stockBonus
{
    struct stockEquity
    {
        public string code, name;
        public int date;
        public double freeEquity,equity,closePrice,percentage;
    }

    struct stockBonus
    {
        public string code,name;
        public int firstDate,secondDate;
        public double firstBonus,secondBonus;
        public double firstPoint, secondPoint;
        public string progress,firstStatus,secondStatus;
        public int planDate;
        public double planBonus;

    }
    
    /// <summary>
    /// 存储股票基本信息结构体。
    /// </summary>
    struct stockFormat
    {
        public string name;
        public string code;
        //记录股票加入指数的时间，和退出的时间。。
        public List<int> existsDate;
    }


    /// <summary>
    /// 记录成分股变动的结构
    /// </summary>
    struct stockModify
    {
        public string name;
        public string code;
        public int date;
        public string direction;
    }
}
