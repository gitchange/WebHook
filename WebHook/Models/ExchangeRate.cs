using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebHook.Models
{
    public class ExchangeRate
    {
        [Display(Name = "幣別")]
        public string Currency { get; set; }
        [Display(Name = "買入現金匯率")]
        public string CashIn { get; set; }
        [Display(Name = "賣出現金匯率")]
        public string CashOut { get; set; }
        [Display(Name = "買入即期匯率")]
        public string SpotIn { get; set; }
        [Display(Name = "賣出即期匯率")]
        public string SpotOut { get; set; }
    }
}