using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebHook.Controllers
{
    public class LineChatController : ApiController
    {
        [HttpPost]
        public IHttpActionResult POST()
        {
            string ChannelAccessToken = "JZilAUe52oNviAO0pnqMs/Ukz+kumvYmQFExILnBdB/PfBtgITd8TRbbT1Zdakn9mCwQ4QdSHYz78QqCI8gpUWJ5Kv4E23iqlgWV0HmWecXGU/CO3S4lfbYFlb10m1sWi184xfuujnJ4y1WXvJFwMwdB04t89/1O/w1cDnyilFU=";
            int nMsgNumber = 10;
            string[] ResponseMessage = new string[nMsgNumber];
            Random random = new Random();
            int current_random = 0;

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

            try
            {
                // 取得隨機要回覆的訊息
                current_random = random.Next(0, 10);
                //取得 http Post RawData(should be JSON)
                string postData = Request.Content.ReadAsStringAsync().Result;
                //剖析JSON
                var ReceivedMessage = isRock.LineBot.Utility.Parsing(postData);

                if (ReceivedMessage.events.FirstOrDefault().type == "follow" || ReceivedMessage.events.FirstOrDefault().type == "join")
                {
                    //新朋友來了(或解除封鎖)
                    isRock.LineBot.Bot bot = new isRock.LineBot.Bot(ChannelAccessToken);
                    var userInfo = bot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.userId);
                    var groupInfo = bot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.groupId);
                    var roomInfo = bot.GetUserInfo(ReceivedMessage.events.FirstOrDefault().source.roomId);

                    //bot.ReplyMessage(ReceivedMessage.events.FirstOrDefault().replyToken, $"哈，'{userInfo.displayName}' 你來了...歡迎");

                    bot.ReplyMessage(ReceivedMessage.events.FirstOrDefault().replyToken, $"哈囉！'{userInfo.displayName}'，我回來了...GroupID='{groupInfo.userId}'");

                    isRock.LineBot.Utility.PushMessage("Ub7edd29f9ec12e41b1eae1c11baa733d", $"UserName='{userInfo.displayName}' , UserID='{userInfo.userId}' ", ChannelAccessToken);
                    isRock.LineBot.Utility.PushMessage("Ub7edd29f9ec12e41b1eae1c11baa733d", $"GroupName='{groupInfo.displayName}' , GroupID='{groupInfo.userId}' ", ChannelAccessToken);
                    isRock.LineBot.Utility.PushMessage("Ub7edd29f9ec12e41b1eae1c11baa733d", $"RoomName='{roomInfo.displayName}' , RoomID='{roomInfo.userId}' ", ChannelAccessToken);

                    return Ok();
                }

                string Message;
                if (ReceivedMessage.events[0].message.text.Contains("里長嬤"))
                {
                    //回覆訊息
                    Message = "里長嬤，" + ResponseMessage[current_random];   //"你說了:" + ReceivedMessage.events[0].message.text;                    
                    //回覆用戶
                    isRock.LineBot.Utility.ReplyMessage(ReceivedMessage.events[0].replyToken, Message, ChannelAccessToken);
                }               
                //回覆API OK
                return Ok();
            }
            catch (Exception ex)
            {
                return Ok();
            }
        }
    }
}
