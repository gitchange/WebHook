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
    public class DrivingPushController : ApiController
    {
        protected string ChannelAccessToken = "JZilAUe52oNviAO0pnqMs/Ukz+kumvYmQFExILnBdB/PfBtgITd8TRbbT1Zdakn9mCwQ4QdSHYz78QqCI8gpUWJ5Kv4E23iqlgWV0HmWecXGU/CO3S4lfbYFlb10m1sWi184xfuujnJ4y1WXvJFwMwdB04t89/1O/w1cDnyilFU=";
        protected string myLineID = "Ub7edd29f9ec12e41b1eae1c11baa733d";

        protected isRock.LineBot.ReceievedMessage ReceivedMessage;
        protected isRock.LineBot.Bot LintBot;
        // POST: api/DrivingPush
        //public void Post([FromBody]string value)
        [HttpPost]
        public IHttpActionResult POST([FromBody]string puid, string pmsg)
        {
            //建立 Line BOT
            LintBot = new isRock.LineBot.Bot(ChannelAccessToken);
            string Message = string.Empty;

            if (puid.ToUpper() == "MYID" || puid == null) puid = myLineID;

            //回覆訊息
            if (pmsg == null) Message = "哈囉！我是熊熊忘記了，現在主動PO訊息給你"; else Message = pmsg;

            try
            {
                //主動送訊息給用戶                            
                LintBot.PushMessage(puid, Message);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error : " + ex.Message.ToString()));
            }

        }
    }
}
