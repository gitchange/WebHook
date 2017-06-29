using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WebHook.Models;
using System.IO;
using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;

namespace WebHook.Controllers
{
    public class LineChatController : ApiController
    {

        protected string ChannelAccessToken = "JZilAUe52oNviAO0pnqMs/Ukz+kumvYmQFExILnBdB/PfBtgITd8TRbbT1Zdakn9mCwQ4QdSHYz78QqCI8gpUWJ5Kv4E23iqlgWV0HmWecXGU/CO3S4lfbYFlb10m1sWi184xfuujnJ4y1WXvJFwMwdB04t89/1O/w1cDnyilFU=";
        protected string myLineID = "Ub7edd29f9ec12e41b1eae1c11baa733d";
        protected isRock.LineBot.ReceievedMessage ReceivedMessage;
        protected isRock.LineBot.Bot LintBot;

        [HttpPost]
        public async Task<IHttpActionResult> POSTAsync()
        {
            try
            {
                //取得 http Post RawData(should be JSON)
                string postData = Request.Content.ReadAsStringAsync().Result;
                //剖析JSON
                ReceivedMessage = isRock.LineBot.Utility.Parsing(postData);
                //建立 Line BOT
                LintBot = new isRock.LineBot.Bot(ChannelAccessToken);
                //取得 User 所 PO 的訊息
                string userMsg = ReceivedMessage.events[0].message.text;

                //新朋友來了(或解除封鎖)
                if (ReceivedMessage.events.FirstOrDefault().type == "follow" || ReceivedMessage.events.FirstOrDefault().type == "join")
                {
                    NewJoin();
                }

                //專門處理關鍵字 - "PM2.5"
                if (userMsg.ToUpper().Contains("PM2.5") || userMsg.Contains("空氣品質") || userMsg.Contains("空污"))
                {
                    await GetAirQulity(userMsg.ToUpper());
                }

                //專門處理關鍵字 - "股價 / 股票"
                if (userMsg.ToUpper().Contains("股價") || userMsg.Contains("股票"))
                {
                    GetStock(userMsg.ToUpper());
                }

                //專門處理關鍵字 - "股價 / 股票"
                if (userMsg.ToUpper().Contains("匯率"))
                {
                    GetExchange(userMsg.ToUpper());
                }

                //專門處理關鍵字 - "里長嬤"
                if (userMsg.Contains("里長嬤"))
                {
                    District();
                }
                //回覆API OK
                return Ok();
            }
            catch (Exception ex)
            {
                return Ok();
            }
        }

        #region 新朋友來了(或解除封鎖)
        /// <summary>
        /// 新朋友來了(或解除封鎖)
        /// </summary>
        private void NewJoin()
        {
            var userInfo = LintBot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.userId);
            var groupInfo = LintBot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.groupId);
            var roomInfo = LintBot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.roomId);

            //bot.ReplyMessage(ReceivedMessage.events.FirstOrDefault().replyToken, $"哈，'{userInfo.displayName}' 你來了...歡迎");

            LintBot.ReplyMessage(ReceivedMessage.events.FirstOrDefault().replyToken, $"哈囉！'{userInfo.displayName}'，我回來了...GroupID='{groupInfo.userId}'");

            LintBot.PushMessage(myLineID, $"UserName='{userInfo.displayName}' , UserID='{userInfo.userId}' ");
            LintBot.PushMessage(myLineID, $"GroupName='{groupInfo.displayName}' , GroupID='{groupInfo.userId}' ");
            LintBot.PushMessage(myLineID, $"RoomName='{roomInfo.displayName}' , RoomID='{roomInfo.userId}' ");
        }
        #endregion

        #region 專門處理關鍵字 - "里長嬤"
        /// <summary>
        /// 專門處理關鍵字 - "里長嬤"
        /// </summary>
        private void District()
        {
            int nMsgNumber = 10;
            string[] ResponseMessage = new string[nMsgNumber];
            string Message;
            Random random = new Random();
            int current_random = 0;

            // 取得隨機要回覆的訊息
            current_random = random.Next(0, nMsgNumber);
            ResponseMessage[0] = "你今天還好嗎？";
            ResponseMessage[1] = "你今天有運動嗎？";
            ResponseMessage[2] = "不要再吃了哦...";
            ResponseMessage[3] = "快出來面對！";
            ResponseMessage[4] = "你今天還好嗎？";
            ResponseMessage[5] = "別再假掰了~";
            ResponseMessage[6] = "你又肚子餓了嗎？";
            ResponseMessage[7] = "認真點工作";
            ResponseMessage[8] = "別再睡了！";
            ResponseMessage[9] = "又敗家了嗎？";

            //回覆訊息
            Message = "里長嬤，" + ResponseMessage[current_random];
            //回覆用戶
            isRock.LineBot.Utility.ReplyMessage(ReceivedMessage.events[0].replyToken, Message, ChannelAccessToken);

            // 暫時測試
            //isRock.LineBot.Bot bot = new isRock.LineBot.Bot(ChannelAccessToken);
            //var userInfo = bot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.userId);
            //bot.PushMessage(myLineID, $"UserName='{userInfo.displayName}' , UserID='{userInfo.userId}' ");
        }
        #endregion

        #region 專門處理關鍵字 - "PM2.5"
        /// <summary>
        /// 專門處理關鍵字 - "PM2.5"
        /// </summary>
        public async Task GetAirQulity(string msg)
        {
            const string targetURL = "http://opendata.epa.gov.tw/ws/Data/REWXQA/?%24orderby=SiteName&%24skip=0&%24top=1000&format=json";
            try
            {
                using (HttpClient client = new HttpClient())
                {

                    client.MaxResponseContentBufferSize = Int32.MaxValue;
                    var response = await client.GetStringAsync(targetURL);
                    var collection = JsonConvert.DeserializeObject<IEnumerable<AirQulity>>(response.Replace("PM2.5", "PM25"));
                    var result = (from c in collection
                                  where msg.Contains(c.SiteName)
                                  select c);
                    if (result.Any())
                    {
                        string remsg = string.Empty;
                        string recommend = string.Empty;
                        int nloop = 0;
                        foreach (var rr in result)
                        {
                            if (nloop > 0) remsg = remsg + "%0D%0A";
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
                        LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, remsg);
                    }
                    else
                    {
                        LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, "無法識別你所指定的地區，總之，快去買一台 Cado 回家就對了...");
                    }
                }
            }
            catch (Exception ex)
            {
                LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, ex.Message);
            }
            return;
        }
        #endregion

        #region 專門處理股價查詢
        private void GetStock(string msg)
        {
            // 先處理對話訊息的字眼,只留數字
            msg = Regex.Replace(msg, "[^0-9]", "");

            List<YahooStock> list = new List<YahooStock>();
            HtmlWeb htmlWeb = new HtmlWeb();
            htmlWeb.OverrideEncoding = Encoding.GetEncoding("big5");
            HtmlAgilityPack.HtmlDocument htmlDoc = htmlWeb.Load(string.Format(@"http://tw.stock.yahoo.com/q/q?s={0}", msg));

            //讀取 Yahoo Stock 網頁
            htmlDoc.DocumentNode.SelectNodes("/html[1]/body[1]/center[1]/table[2]/tr[1]/td[1]/table[1]").
                AsParallel().ToList().ForEach(ac =>
                {
                    HtmlNode node = ac.SelectSingleNode("./tr[1]/th");

                    list.Add(new YahooStock
                    {
                        StockID = ac.SelectSingleNode("./tr[2]/td[1]").InnerText,
                        DateTime = ac.SelectSingleNode("./tr[2]/td[2]").InnerText,
                        DealPrice = ac.SelectSingleNode("./tr[2]/td[3]").InnerText,
                        BuyPrice = ac.SelectSingleNode("./tr[2]/td[4]").InnerText,
                        SellPrice = ac.SelectSingleNode("./tr[2]/td[5]").InnerText,
                        UpDown = ac.SelectSingleNode("./tr[2]/td[6]").InnerText,
                        StockQty = ac.SelectSingleNode("./tr[2]/td[7]").InnerText,
                        YesterdayPrice = ac.SelectSingleNode("./tr[2]/td[8]").InnerText,
                        OpenPrice = ac.SelectSingleNode("./tr[2]/td[9]").InnerText,
                        Highest = ac.SelectSingleNode("./tr[2]/td[10]").InnerText,
                        Lowest = ac.SelectSingleNode("./tr[2]/td[11]").InnerText,
                        StockInfo = ac.SelectSingleNode("./tr[2]/td[12]").InnerText
                    });
                });
            string remsg = string.Empty;
            foreach (var st in list)
            {
                remsg += string.Format(@"股票代碼：{1}{0}捉取時間{2}{0}成交價：{3}{0}買進價：{4}{0}賣出價：{5}{0}漲跌：{6}{0}成交量：{7}{0}昨日收盤價：{8}{0}開盤價：{9}{0}最高價：{10}{0}最低價：{11}{0}",
                                                  System.Environment.NewLine, st.StockID.Replace("加到投資組合",""), st.DateTime, st.DealPrice,
                                                  st.BuyPrice, st.SellPrice, st.UpDown.Trim(), st.StockQty,
                                                  st.YesterdayPrice, st.OpenPrice, st.Highest, st.Lowest);
            }
            LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, remsg);
        }
        #endregion

        #region 專門處理匯率查詢
        private void GetExchange(string msg)
        {
            // 先處理對話訊息的字眼,只留數字
            msg = msg.Replace("的", "");
            msg = msg.Replace("匯率", "");

            List<ExchangeRate> list = new List<ExchangeRate>();
            HtmlWeb htmlWeb = new HtmlWeb();
            //強制在讀取網頁的時候，讓編碼是 big5 (若是大陸的網頁要改成 gbxxxx 試看看)
            htmlWeb.OverrideEncoding = Encoding.GetEncoding("big5");

            //讀取台灣銀行牌告匯率網頁
            HtmlAgilityPack.HtmlDocument htmlDoc = htmlWeb.Load("http://rate.bot.com.tw/xrt?Lang=zh-TW");
            htmlDoc.DocumentNode.SelectNodes("/html[1]/body[1]/div[1]/div[4]/table[1]/tbody[1]/tr[1]").
                AsParallel().ToList().ForEach(ac =>
                {                   
                    list.Add(new ExchangeRate
                    {
                        Currency = ac.SelectSingleNode("./td[1]/div[1]/div[3]").InnerText,
                        CashIn = ac.SelectSingleNode("./td[2]").InnerText,
                        CashOut = ac.SelectSingleNode("./td[3]").InnerText,
                        SpotIn = ac.SelectSingleNode("./td[4]").InnerText,
                        SpotOut = ac.SelectSingleNode("./td[5]").InnerText
                    });
                });
            List<ExchangeRate> showlist = new List<ExchangeRate>();
            if (msg.Contains("今日") || msg == "")
            {
                showlist = list;
            }
            else
            {
                showlist = (List<ExchangeRate>)(from l in list where msg.Contains(l.Currency) select l);
            }
            string remsg = string.Empty;
            foreach (var st in showlist)
            {
                remsg += string.Format(@"幣別：{1}{0}買入現金匯率{2}{0}賣出現金匯率：{3}{0}買入即期匯率：{4}{0}賣出即期匯率：{5}{0}{0}",
                                                  System.Environment.NewLine, st.Currency, st.CashIn, st.CashOut, st.SpotIn, st.SpotOut);
            }
            LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, remsg);
        }
        #endregion
    }
}