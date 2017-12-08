using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace WebHook.Models
{
    public static class Jobs
    {
        #region Call Line API to Push Message
        public static async Task<string> CallLineAPIPushMessage(string pmsg)
        {
            string uri = "https://api.line.me/v2/bot/message/push";
            string ChannelAccessToken = WebConfigurationManager.AppSettings["ChannelAccessToken"].ToString();
            string DistrictGroup = WebConfigurationManager.AppSettings["DistrictGroup"].ToString();
            string result = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ChannelAccessToken);
                PushMsg pm = new PushMsg();
                pm.to = DistrictGroup;
                pm.messages = new List<PushMsgTxt>();
                pm.messages.Add(new PushMsgTxt { type = "text", text = pmsg });
                string json = JsonConvert.SerializeObject(pm);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(uri, content);
                //result = response.EnsureSuccessStatusCode().ToString();
            }
            return result;
        }
        #endregion

        /// <summary>
        /// job_pm25_gm : 顯示空污叫早安
        /// </summary>
        /// <returns></returns>
        public static async Task job_pm25_0700()
        {
            string get_msg = await CallFunction.GetAirQulity("善化 嘉義 新營 的空污");
            string post_msg = "大家早！以下是三處的空污狀況，祝各位出門順心，一路平安！" + Environment.NewLine + get_msg;

            await CallLineAPIPushMessage(post_msg);
        }

        /// <summary>
        /// job_pm25_ga : 顯示空污叫下班
        /// </summary>
        /// <returns></returns>
        public static async Task job_pm25_1700()
        {
            string get_msg = await CallFunction.GetAirQulity("善化 嘉義 新營 的空污");
            string post_msg = "啾咪！以下是三處的空污狀況，祝各位下班順心，一路平安！" + Environment.NewLine + get_msg;
            await CallLineAPIPushMessage(post_msg);
        }

        /// <summary>
        /// job_exchange_rate() : 顯示匯率 (中午 12:00)
        /// </summary>
        /// <returns></returns>
        public static void job_exchange_rate_1200()
        {
            string get_msg = CallFunction.GetExchange("匯率");
            string post_msg = "午安！吃飯時間到了哦！今日目前的匯率是：" + Environment.NewLine + get_msg;
            CallLineAPIPushMessage(post_msg);
        }

        /// <summary>
        /// job_exchange_rate() : 顯示匯率 (下午 16:30)
        /// </summary>
        /// <returns></returns>
        public static void job_exchange_rate_1630()
        {
            string get_msg = CallFunction.GetExchange("匯率");
            string post_msg = "安安！吃過下午茶了嗎？今日收盤的匯率是：" + Environment.NewLine + get_msg;
            CallLineAPIPushMessage(post_msg);
        }
    }
}