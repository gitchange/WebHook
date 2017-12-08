using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebHook.Models
{
    public static class CallFunction
    {
        #region 專門處理關鍵字 - "PM2.5"
        /// <summary>
        /// 專門處理關鍵字 - "PM2.5"
        /// </summary>
        public static async Task<string> GetAirQulity(string pmsg)
        //public static string GetAirQulity(string pmsg)
        {
            // 政府資料開放平台 - 空氣品質指標(AQI) : https://data.gov.tw/dataset/40448
            const string targetURL = "http://opendata2.epa.gov.tw/AQI.json";
            string remsg = string.Empty;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.MaxResponseContentBufferSize = Int32.MaxValue;
                    var response = await client.GetStringAsync(targetURL);
                    var collection = JsonConvert.DeserializeObject<IEnumerable<AirQulity>>(response.Replace("PM2.5", "PM25"));
                    var result = (from c in collection
                                  where pmsg.Contains(c.SiteName)
                                  select c);
                    if (result.Any())
                    {
                        string recommend = string.Empty;
                        int nloop = 0;
                        foreach (var rr in result)
                        {
                            if (nloop > 0) remsg = remsg + System.Environment.NewLine;
                            int intPM25 = int.Parse(rr.PM25);
                            if (intPM25 >= 0 || intPM25 <= 35)
                                recommend = "(舒適，可從事戶外活動)";
                            else
                                if (intPM25 >= 36 || intPM25 <= 53)
                                recommend = "(舒適尚可，可從事戶外活動，但應考慮減少體力消耗)";
                            else
                                if (intPM25 >= 54 || intPM25 <= 70)
                                recommend = "(任何人如果有不適，如眼痛，咳嗽或喉嚨痛等，應該考慮減少戶外活動)";
                            else
                                if (intPM25 >= 71) recommend = "(任何人如果有不適，如眼痛，咳嗽或喉嚨痛等，應減少體力消耗，特別是減少戶外活動)";

                            remsg = remsg + string.Format("{0}的 PM2.5數值為{1} {2}", rr.SiteName, rr.PM25, recommend);
                            nloop++;
                        }
                    }
                    else
                    {
                        remsg = "無法識別你所指定的地區，總之，快去買一台 Cado 回家就對了...";
                    }
                }
            }
            catch (Exception ex)
            {
                remsg = ex.Message;
            }
            return remsg;
        }
        #endregion

        #region 專門處理匯率查詢
        public static string GetExchange(string pmsg)
        {
            // 先處理對話訊息的字眼,只留數字
            pmsg = pmsg.Replace("的", "");
            pmsg = pmsg.Replace("匯率", "");

            List<ExchangeRate> list = new List<ExchangeRate>();
            HtmlWeb htmlWeb = new HtmlWeb();
            //因為台銀的網頁非 big5 編碼，所以不強制轉碼
            //htmlWeb.OverrideEncoding = Encoding.GetEncoding("big5");

            //讀取台灣銀行牌告匯率網頁
            HtmlAgilityPack.HtmlDocument htmlDoc = htmlWeb.Load("http://rate.bot.com.tw/xrt?Lang=zh-TW");
            ExchangeRate er = new ExchangeRate();
            for (int row = 1; row <= 19; row++)
            {
                list.Add(new ExchangeRate
                {
                    Currency = htmlDoc.DocumentNode.SelectSingleNode(string.Format(@"/html[1]/body[1]/div[1]/main[1]/div[4]/table[1]/tbody[1]/tr[{0}]/td[1]/div[1]/div[3]", row)).InnerText.Trim(),
                    CashIn = htmlDoc.DocumentNode.SelectSingleNode(string.Format(@"/html[1]/body[1]/div[1]/main[1]/div[4]/table[1]/tbody[1]/tr[{0}]/td[2]", row)).InnerText.Trim(),
                    CashOut = htmlDoc.DocumentNode.SelectSingleNode(string.Format(@"/html[1]/body[1]/div[1]/main[1]/div[4]/table[1]/tbody[1]/tr[{0}]/td[3]", row)).InnerText.Trim(),
                    SpotIn = htmlDoc.DocumentNode.SelectSingleNode(string.Format(@"/html[1]/body[1]/div[1]/main[1]/div[4]/table[1]/tbody[1]/tr[{0}]/td[4]", row)).InnerText.Trim(),
                    SpotOut = htmlDoc.DocumentNode.SelectSingleNode(string.Format(@"/html[1]/body[1]/div[1]/main[1]/div[4]/table[1]/tbody[1]/tr[{0}]/td[5]", row)).InnerText.Trim()
                });
            }

            string remsg = string.Empty;
            if (pmsg.Contains("今日") || pmsg == "")
            {
                foreach (var st in list)
                {
                    remsg += string.Format(@"幣別：{1}{0}買入現金匯率：{2}{0}賣出現金匯率：{3}{0}買入即期匯率：{4}{0}賣出即期匯率：{5}{0}{0}",
                                                      System.Environment.NewLine, st.Currency, st.CashIn, st.CashOut, st.SpotIn, st.SpotOut);
                }
            }
            else
            {
                var showlist = (from l in list where l.Currency.Contains(pmsg) select l);
                if (showlist.Any())
                {
                    foreach (var st in showlist)
                    {
                        remsg += string.Format(@"幣別：{1}{0}買入現金匯率：{2}{0}賣出現金匯率：{3}{0}買入即期匯率：{4}{0}賣出即期匯率：{5}{0}{0}",
                                                          System.Environment.NewLine, st.Currency, st.CashIn, st.CashOut, st.SpotIn, st.SpotOut);
                    }
                }
            }
            //LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, remsg);
            return remsg;
        }
        #endregion
    }
}