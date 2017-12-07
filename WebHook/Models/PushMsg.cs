using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebHook.Models
{
    public class PushMsg
    {
        public string to { get; set; }
        public List<PushMsgTxt> messages { get; set; }
    }

    public class PushMsgTxt
    {
        public string type { get; set; }
        public string text { get; set; }
    }
}