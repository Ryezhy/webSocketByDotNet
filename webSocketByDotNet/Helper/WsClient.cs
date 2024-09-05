using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using TouchSocket.Core;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;
using Prism.Mvvm;
using System.Windows;

namespace WxAppPlugin.Helper
{
    public class WsClient: BindableBase
    {
        private static WebSocketClient _Client;

        public static WebSocketClient Instance
        {
            get {
                if (_Client == null)
                {
                    _Client = new WebSocketClient();
                }
                return _Client;
            }
        }

        public static string connectUrl = "ws://127.0.0.1:9080/";

        public static string ConnectTime = DateTime.Now.ToString();

        public static string ConnectStatus = "未连接";
        }
}
