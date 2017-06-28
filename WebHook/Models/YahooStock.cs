using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebHook.Models
{
    public class YahooStock
    {
        [Display(Name = "股票代號")]
        public string StockID { get; set; }

        [Display(Name = "時間")]
        public string DateTime { get; set; }

        [Display(Name = "成交價")]
        public string DealPrice { get; set; }

        [Display(Name = "買進價")]
        public string BuyPrice { get; set; }

        [Display(Name = "賣出價")]
        public string SellPrice { get; set; }

        [Display(Name = "漲跌")]
        public string UpDown { get; set; }

        [Display(Name = "張數")]
        public string StockQty { get; set; }

        [Display(Name = "昨收")]
        public string YesterdayPrice { get; set; }

        [Display(Name = "開盤")]
        public string OpenPrice { get; set; }

        [Display(Name = "最高")]
        public string Highest { get; set; }

        [Display(Name = "最低")]
        public string Lowest { get; set; }

        [Display(Name = "個股資料")]
        public string StockInfo { get; set; }
    }
}