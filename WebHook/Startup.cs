using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Hangfire;
using Hangfire.MemoryStorage;
using WebHook.Models;
using System.Web.Http;

[assembly: OwinStartup(typeof(WebHook.Startup))]
namespace WebHook
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 如需如何設定應用程式的詳細資訊，請瀏覽 https://go.microsoft.com/fwlink/?LinkID=316888

            // Wire Web API
            var httpConfiguration = new HttpConfiguration();
            httpConfiguration.MapHttpAttributeRoutes();
            app.UseWebApi(httpConfiguration);

            // 指定Hangfire使用記憶體儲存任務
            Hangfire.GlobalConfiguration.Configuration.UseMemoryStorage();

            app.UseHangfireDashboard("/watchjobs", new DashboardOptions
            {
                Authorization = new[] { new MyAuthorizationFilter() }
            });
            // 啟用HanfireServer
            app.UseHangfireServer();
            // 啟用Hangfire的Dashboard
            //app.UseHangfireDashboard();            
            app.UseHangfireDashboard("/watchjobs");            
        }
    }
}
