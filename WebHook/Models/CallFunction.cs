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
    public static class CallFunction
    {
        #region
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
    }
}