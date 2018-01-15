using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebHook.Models;

namespace WebHook.Controllers
{
    public class JobsController : ApiController
    {
        // GET: api/Jobs
        public async void Get()
        {
            DateTime timeUtc = DateTime.UtcNow;
            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, cstZone);            
            int hourly = cstTime.Hour;            

            switch (hourly)
            {
                case 7:
                    await Jobs.job_pm25_0700();
                    break;
                case 17:
                    await Jobs.job_pm25_1700();
                    break;
                case 12:
                    Jobs.job_exchange_rate_1200();
                    break;
                case 16:
                    Jobs.job_exchange_rate_1630();
                    break;
                default:
                    break;
            }            
        }
    }
}
