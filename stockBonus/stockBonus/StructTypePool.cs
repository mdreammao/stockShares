using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stockBonus
{
    /// <summary>
    /// 存储股票基本信息结构体。
    /// </summary>
    struct stockFormat
    {
        public string name;
        public int code;
        public string market;
        //记录股票加入指数的时间，和退出的时间。。
        public List<int> existsDate;
    }


    /// <summary>
    /// 记录成分股变动的结构
    /// </summary>
    struct stockModify
    {
        public string name;
        public int code;
        public string market;
        public int date;
        public string direction;
    }
}
