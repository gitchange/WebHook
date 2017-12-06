using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebHook.Models
{
    //public class AirQulity
    //{
    //    [Display(Name = "測站名稱")]
    //    public string SiteName { get; set; }
    //    [Display(Name = "縣市")]
    //    public string County { get; set; }
    //    [Display(Name = "空氣污染指標")]
    //    public string PSI { get; set; }
    //    [Display(Name = "指標污染物")]
    //    public string MajorPollutant { get; set; }
    //    [Display(Name = "狀態")]
    //    public string Status { get; set; }
    //    [Display(Name = "二氧化硫濃度")]
    //    public string SO2 { get; set; }
    //    [Display(Name = "一氧化碳濃度")]
    //    public string CO { get; set; }
    //    [Display(Name = "臭氧濃度")]
    //    public string O3 { get; set; }
    //    [Display(Name = "懸浮微粒濃度")]
    //    public string PM10 { get; set; }
    //    [Display(Name = "細懸浮微粒濃度")]
    //    public string PM25 { get; set; }
    //    [Display(Name = "二氧化氮濃度")]
    //    public string NO2 { get; set; }
    //    [Display(Name = "風速")]
    //    public string WindSpeed { get; set; }
    //    [Display(Name = "風向")]
    //    public string WindDirec { get; set; }
    //    [Display(Name = "FPMI")]
    //    public string FPMI { get; set; }
    //    [Display(Name = "NOX")]
    //    public string NOx { get; set; }
    //    [Display(Name = "NO")]
    //    public string NO { get; set; }
    //    [Display(Name = "發佈時間")]
    //    public string PublishTime { get; set; }
    //}

    public class AirQulity
    {
        [Display(Name = "空氣品質指標")]
        public string AQI { get; set; }
        [Display(Name = "一氧化氮(ppb)")]
        public string CO { get; set; }
        [Display(Name = "一氧化碳8小時行動平均(ppm)")]
        public string CO_8hr { get; set; }
        [Display(Name = "縣市")]
        public string County { get; set; }
        [Display(Name = "氮氧化物(ppb)")]
        public string NO { get; set; }
        [Display(Name = "二氧化氮(ppb)")]
        public string NO2 { get; set; }
        [Display(Name = "NOx")]
        public string NOx { get; set; }
        [Display(Name = "臭氧")]
        public string O3 { get; set; }
        [Display(Name = "臭氧8小時行動平均(ppb)")]
        public string O3_8hr { get; set; }
        [Display(Name = "細懸浮微粒(μg/m3)")]
        public string PM10 { get; set; }
        [Display(Name = "細懸浮微粒行動平均值(μg/m3)")]
        public string PM10_AVG { get; set; }
        [Display(Name = "懸浮微粒(μg/m3)")]
        public string PM25 { get; set; }
        [Display(Name = "懸浮微粒行動平均值(μg/m3)")]
        public string PM25_AVG { get; set; }
        [Display(Name = "空氣污染指標物")]
        public string Pollutant { get; set; }
        [Display(Name = "資料建置日期")]
        public string PublishTime { get; set; }
        [Display(Name = "測站名稱")]
        public string SiteName { get; set; }
        [Display(Name = "二氧化硫(ppb)")]
        public string SO2 { get; set; }
        [Display(Name = "狀態")]
        public string Status { get; set; }
        [Display(Name = "風向(degrees)")]
        public string WindDirec { get; set; }
        [Display(Name = "風速(m/sec)")]
        public string WindSpeed { get; set; }
    }
}