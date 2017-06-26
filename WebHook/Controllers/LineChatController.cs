using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WebHook.Models;

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
            current_random = random.Next(0, 10);
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
    }
}