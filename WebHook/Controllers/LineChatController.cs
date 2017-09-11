﻿using Newtonsoft.Json;
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
using maduka_QnAMakerLibrary;
using maduka_QnAMakerLibrary.Interface;
using System.Web.Configuration;

namespace WebHook.Controllers
{
    public class LineChatController : ApiController
    {

        protected string ChannelAccessToken = "JZilAUe52oNviAO0pnqMs/Ukz+kumvYmQFExILnBdB/PfBtgITd8TRbbT1Zdakn9mCwQ4QdSHYz78QqCI8gpUWJ5Kv4E23iqlgWV0HmWecXGU/CO3S4lfbYFlb10m1sWi184xfuujnJ4y1WXvJFwMwdB04t89/1O/w1cDnyilFU=";
        protected string myLineID = "Ub7edd29f9ec12e41b1eae1c11baa733d";
        protected isRock.LineBot.ReceievedMessage ReceivedMessage;
        protected isRock.LineBot.Bot LintBot;
        protected isRock.LineBot.LineUserInfo userInfo;
        protected string username = string.Empty;
        /// <summary>
        /// Microsoft QnA Maker 訂閱的金鑰字串設定
        /// </summary>
        protected string SubscriptionKey = WebConfigurationManager.AppSettings["SubscriptionKey"].ToString();
        /// <summary>
        /// Microsoft QnA Maker knowledge bases ID
        /// </summary>
        protected string strKbId = WebConfigurationManager.AppSettings["kbId"].ToString();

        #region LineBOT 主程式 - 取得使用者、訊息資訊及判斷該如何處理回覆
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
                //取得 User 的資訊
                userInfo = LintBot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.userId);
                switch (userInfo.displayName.Trim())
                {
                    case "熊寶寶":
                        username = "熊寶";
                        break;
                    case "蔡福元":
                        username = "里長伯";
                        break;
                    case "Maggie":
                        username = "里長嬤";
                        break;
                    default:
                        break;
                }
                //取得 User 所 PO 的訊息
                string userMsg = ReceivedMessage.events[0].message.text;

                //新朋友來了(或解除封鎖)
                if (ReceivedMessage.events.FirstOrDefault().type == "follow" || ReceivedMessage.events.FirstOrDefault().type == "join")
                {
                    NewJoin();
                }

                //專門處理關鍵字 - "/ShowMyID"
                if (userMsg.ToUpper().Contains("/SHOWMYID"))
                {
                    ShowMyID();
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

                //專門處理 Q & A ：前置字元為"熊熊："
                if (userMsg.Contains("熊熊：") || userMsg.Contains("熊熊，"))
                {
                    QNAMaker(userMsg);
                }

                //專門處理關鍵字 - "里長嬤" or "里長伯"
                if (userMsg.Contains("里長嬤"))
                {
                    District("里長嬤");
                }
                if (userMsg.Contains("里長伯"))
                {
                    District("里長伯");
                }
                //回覆API OK
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error : " + ex.Message.ToString()));
            }
        }
        #endregion

        #region "處理 Q & A Maker https://qnamaker.ai/"

        #region 主程式判斷要進入哪一個子程序
        /// <summary>
        /// 主程式判斷要進入哪一個子程序
        /// </summary>
        /// <param name="pQuestion"></param>
        private void QNAMaker(string pQuestion)
        {
            string pquestion = pQuestion.Replace("熊熊：", "").Replace("熊熊，", "");

            if (pQuestion == "熊熊：" || pQuestion == "熊熊，")       //如果只是呼叫熊熊：，就回答什麼事
            {
                LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, string.Format("{0},{1}", userInfo.displayName, "什麼事？"));
            }
            else                            //如果熊熊：後面還有字串，就開始由微軟處理 Q & A 回應
            {
                if (pQuestion.ToUpper().Contains("ADDQ:") && pQuestion.ToUpper().Contains("ADDA:"))
                    QNAMakerAddQnA(pquestion);
                else
                {
                    if (pQuestion.ToUpper().Contains("(UPDATE)"))
                    {
                        QNAMakerUpdate();
                    }
                    else QNAMakerGenerateAnswer(pquestion);
                }
            }
        }
        #endregion

        #region 處理 Train & Publish
        /// <summary>
        /// 處理 Train & Publish
        /// </summary>
        private void QNAMakerUpdate()
        {
            string trainMsg, publishMsg;
            maduka_QnAMakerLibrary.API.QnAMaker QNAMaker = new maduka_QnAMakerLibrary.API.QnAMaker();
            QNAMaker.SubscriptionKey = SubscriptionKey;
            HttpStatusCode code = HttpStatusCode.OK;
            QNAMaker.TrainKB(strKbId, out code);
            if (code == HttpStatusCode.NoContent)
                trainMsg = "Train KB Success";
            else
                trainMsg = "Train KB Fail:" + code.ToString();

            QNAMaker.PublishKB(strKbId, out code);

            if (code == HttpStatusCode.NoContent)
                publishMsg = "Publish KB Success";
            else
                publishMsg = "Publish KB Fail:" + code.ToString();

            LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, string.Format("{0}，{1} ; {2}", username, trainMsg, publishMsg));
        }
        #endregion

        #region 處理新增 Knowledge Base Q & A 問題
        /// <summary>
        /// 處理新增 Knowledge Base Q & A 問題
        /// </summary>
        /// <param name="pquestion"></param>
        private void QNAMakerAddQnA(string pquestion)
        {
            List<KBModel.QnAQueryList> objQnA = new List<KBModel.QnAQueryList>();
            KBModel.UpdateKBModel objUpdate = new KBModel.UpdateKBModel()
            {
                add = new KBModel.UpdateKBModel.Add()
                {
                    qnaPairs = new List<KBModel.QnAList>(),
                    urls = new List<string>()
                },
                delete = new KBModel.UpdateKBModel.Delete()
                {
                    qnaPairs = new List<KBModel.QnAList>()
                },
            };

            int startQ = pquestion.IndexOf("AddQ:");
            int startA = pquestion.IndexOf("AddA:");
            string AddQ = pquestion.Substring(startQ + 5, startA - 5);
            string AddA = pquestion.Substring(startA + 5);
            Console.WriteLine(AddQ.Trim());
            Console.WriteLine(AddA.Trim());

            objQnA.Add(
                new KBModel.QnAQueryList()
                {
                    answer = AddA.Trim(),
                    question = AddQ.Trim(),
                    source = "add",
                }
            );

            for (int i = 0; i < objQnA.Count; i++)
            {
                if (objQnA[i].source == "delete")
                {
                    objUpdate.delete.qnaPairs.Add(new KBModel.QnAList()
                    {
                        answer = objQnA[i].answer,
                        question = objQnA[i].question,
                    }
                    );
                }
                else if (objQnA[i].source == "add")
                {
                    objUpdate.add.qnaPairs.Add(new KBModel.QnAList()
                    {
                        answer = objQnA[i].answer,
                        question = objQnA[i].question,
                    }
                    );
                }
            }

            maduka_QnAMakerLibrary.API.QnAMaker QNAMaker = new maduka_QnAMakerLibrary.API.QnAMaker();
            QNAMaker.SubscriptionKey = SubscriptionKey;
            HttpStatusCode code = HttpStatusCode.OK;
            string strMsg = QNAMaker.UpdateKB(strKbId, objUpdate, out code);

            if (code == HttpStatusCode.NoContent)
            {
                LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, string.Format("{0}，{1}", username, "Update KB Success"));
            }
            else
            {
                LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, "Update KB Fail:" + code.ToString());
            }
        }
        #endregion

        #region 處理送出 Question 取回 Answer
        /// <summary>
        /// 處理送出 Question 取回 Answer
        /// </summary>
        /// <param name="pquestion"></param>
        private void QNAMakerGenerateAnswer(string pquestion)
        {
            KBModel.GenerateAnswerModel objQuery = new KBModel.GenerateAnswerModel()
            {
                question = pquestion,
                top = 1,
            };
            maduka_QnAMakerLibrary.API.QnAMaker QNAMaker = new maduka_QnAMakerLibrary.API.QnAMaker();
            QNAMaker.SubscriptionKey = SubscriptionKey;
            HttpStatusCode code = HttpStatusCode.OK;
            KBModel.GenerateAnswerResultModel result = QNAMaker.GenerateAnswer(strKbId, objQuery, out code);
            string remsg = string.Empty;

            if (code == HttpStatusCode.OK)
            {
                // 取出最相似的回覆，並放在文字方塊中
                if (result.answers.Count > 0)
                {
                    remsg = string.Format("{0}，{1}", username, result.answers[0].answer);
                }
                else
                {
                    remsg = string.Format("{0}，{1}", username, "哩哄啥，哇聽某！");
                }
                LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, remsg);
            }
            else
            {
                LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, "Generate Answer Fail:" + code.ToString());
            }
        }
        #endregion

        #endregion

        #region 新朋友來了(或解除封鎖)
        /// <summary>
        /// 新朋友來了(或解除封鎖)
        /// </summary>
        private void NewJoin()
        {
            //var userInfo = LintBot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.userId);
            var groupInfo = LintBot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.groupId);
            var roomInfo = LintBot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.roomId);

            //bot.ReplyMessage(ReceivedMessage.events.FirstOrDefault().replyToken, $"哈，'{userInfo.displayName}' 你來了...歡迎");

            //LintBot.ReplyMessage(ReceivedMessage.events.FirstOrDefault().replyToken, $"哈囉！'{userInfo.displayName}'，我回來了...GroupID='{groupInfo.userId}'");

            LintBot.PushMessage(myLineID, string.Format("UserName={0} ; UserID={1}", userInfo.displayName, userInfo.userId));
            LintBot.PushMessage(myLineID, string.Format("GroupName={0} ; GropuID={1}", groupInfo.displayName, groupInfo.userId));
            LintBot.PushMessage(myLineID, string.Format("RoomName={0} ; RoomID={1}", roomInfo.displayName, roomInfo.userId));
        }
        #endregion

        #region 專門處理關鍵字 - "/showmyid"
        private void ShowMyID()
        {
            //var userInfo = LintBot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.userId);
            //var groupInfo = LintBot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.groupId);
            //var roomInfo = LintBot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.roomId);
            string Message;
            //回覆訊息
            Message = "哈囉！" + userInfo.displayName + "，你的 ID 是：" + userInfo.userId;
            //回覆用戶
            isRock.LineBot.Utility.ReplyMessage(ReceivedMessage.events[0].replyToken, Message, ChannelAccessToken);
            //LintBot.PushMessage(userInfo.userId, string.Format("UserName={0} ; UserID={1}", userInfo.displayName, userInfo.userId));
            //LintBot.PushMessage(userInfo.userId, string.Format("GroupName={0} ; GropuID={1}", groupInfo.displayName, groupInfo.userId));
            //LintBot.PushMessage(userInfo.userId, string.Format("RoomName={0} ; RoomID={1}", roomInfo.displayName, roomInfo.userId));
            LintBot.PushMessage(userInfo.userId, "哈囉！我是熊熊忘記了，現在主動PO訊息給你");
        }
        #endregion

        #region 專門處理關鍵字 - "里長嬤、里長伯"
        /// <summary>
        /// 專門處理關鍵字 - "里長嬤、里長伯"
        /// </summary>
        private void District(string pdistrict)
        {
            int nSex = 2;
            int nMsgNumber = 12;
            string[,] ResponseMessage = new string[nSex, nMsgNumber];

            if (pdistrict == "里長嬤") nSex = 0; else nSex = 1;

            string Message;
            Random random = new Random();
            int current_random = 0;

            // 取得隨機要回覆的訊息
            current_random = random.Next(0, nMsgNumber);
            ResponseMessage[0, 0] = "你今天還好嗎？";
            ResponseMessage[0, 1] = "鋰鎂銅鋰鋅";
            ResponseMessage[0, 2] = "不要再吃了哦...";
            ResponseMessage[0, 3] = "快出來面對！";
            ResponseMessage[0, 4] = "里長伯說你不要只會嘴砲，多說無益！";
            ResponseMessage[0, 5] = "鳥龜裝，貴森森";
            ResponseMessage[0, 6] = "別再假掰了~";
            ResponseMessage[0, 7] = "你又肚子餓了嗎？";
            ResponseMessage[0, 8] = "啊不就好棒棒";
            ResponseMessage[0, 9] = "別再睡了！";
            ResponseMessage[0, 10] = "三姑加六婆，沒人你對手";
            ResponseMessage[0, 11] = "又敗家了嗎？";
            ResponseMessage[1, 0] = "你今天還好嗎？";
            ResponseMessage[1, 1] = "麥擱滑手機啊!";
            ResponseMessage[1, 2] = "不能再吃了哦...";
            ResponseMessage[1, 3] = "快出來面對！";
            ResponseMessage[1, 4] = "振作一點...";
            ResponseMessage[1, 5] = "給你87分，不能再高了";
            ResponseMessage[1, 6] = "吃納豆有益身體健康";
            ResponseMessage[1, 7] = "94狂！";
            ResponseMessage[1, 8] = "嘉義福源肉粽讚讚讚！";
            ResponseMessage[1, 9] = "全家來買菜，福祭中元節";
            ResponseMessage[1, 10] = "很多運動不需要穿運動鞋哦！";
            ResponseMessage[1, 11] = "假的，哎呀我的眼睛業障重啊！";

            //回覆訊息
            Message = pdistrict + "，" + ResponseMessage[nSex, current_random];
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
                                                  System.Environment.NewLine, st.StockID.Replace("加到投資組合", ""), st.DateTime, st.DealPrice,
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
            if (msg.Contains("今日") || msg == "")
            {
                foreach (var st in list)
                {
                    remsg += string.Format(@"幣別：{1}{0}買入現金匯率：{2}{0}賣出現金匯率：{3}{0}買入即期匯率：{4}{0}賣出即期匯率：{5}{0}{0}",
                                                      System.Environment.NewLine, st.Currency, st.CashIn, st.CashOut, st.SpotIn, st.SpotOut);
                }
            }
            else
            {
                var showlist = (from l in list where l.Currency.Contains(msg) select l);
                if (showlist.Any())
                {
                    foreach (var st in showlist)
                    {
                        remsg += string.Format(@"幣別：{1}{0}買入現金匯率：{2}{0}賣出現金匯率：{3}{0}買入即期匯率：{4}{0}賣出即期匯率：{5}{0}{0}",
                                                          System.Environment.NewLine, st.Currency, st.CashIn, st.CashOut, st.SpotIn, st.SpotOut);
                    }
                }
            }
            LintBot.ReplyMessage(ReceivedMessage.events[0].replyToken, remsg);
        }
        #endregion
    }
}