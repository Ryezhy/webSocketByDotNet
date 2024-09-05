using Prism.Commands;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System;
using TouchSocket.Core;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;
using WxAppPlugin.Helper;
using WxAppPlugin.Models;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Net;

namespace WxAppPlugin.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public MainWindowViewModel()
        {
            _log = Log.Instance;
            _FunctionItems = new List<string>();
            WebSocketUrl = "ws://127.0.0.1:9080/ws";
            SendMessage = "Hello";
            PongMessage = "未连接";
            CanConnet = true;
            IsAutoConnect = true;
            SendMessageCommand = new DelegateCommand(async () =>
            {
                WebSocketClient client = WsClient.Instance;

                if (client.Client.Online)
                {
                    await client.SendAsync(SendMessage);
                    SendMessage = "";
                }
                else
                {
                    MessageBox.Show("当前连接离线");
                }
            });
            ClearLogCommand = new DelegateCommand(() =>
            {
                _log.Clear();
            });
            ConnectCommand = new DelegateCommand(Connect);
            CloseCommand = new DelegateCommand(Close);
            Connect();
        }
        private string _title = "webSocket客户端ByDotNet";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }
        /// <summary>
        /// 日志
        /// </summary>
        private readonly Log _log;
        public Log Log
        {
            get { return _log; }
        }

        private bool _isAutoConnect;
        public bool IsAutoConnect
        {
            get { return _isAutoConnect; }
            set
            {
                _isAutoConnect = value;
                RaisePropertyChanged(nameof(IsAutoConnect));
            }
        }
        //功能项
        private List<string> _FunctionItems;
        public List<string> FunctionItems
        {
            get
            {
                return _FunctionItems;
            }
        }
        /// <summary>
        /// webSocket连接地址
        /// </summary>
        private string _WebSocketUrl;
        public string WebSocketUrl
        {
            get
            {
                return _WebSocketUrl;
            }
            set
            {
                _WebSocketUrl = value;
                RaisePropertyChanged(nameof(WebSocketUrl));
            }
        }

        private bool _CanConnet;
        public bool CanConnet
        {
            get
            {
                return _CanConnet;
            }
            set
            {
                _CanConnet = value;
                RaisePropertyChanged(nameof(CanConnet));
            }
        }
        private string _PongMessage;
        /// <summary>
        /// 收到的消息
        /// </summary>
        public string PongMessage
        {
            get
            {
                return _PongMessage;
            }
            set
            {
                _PongMessage = value;
                RaisePropertyChanged(nameof(PongMessage));
            }
        }

        private string _SendMessage;
        /// <summary>
        /// 发送的消息
        /// </summary>
        public string SendMessage
        {
            get
            {
                return _SendMessage;
            }
            set
            {
                _SendMessage = value;
                RaisePropertyChanged(nameof(SendMessage));
            }
        }



        public async void Connect()
        {
            CanConnet = false;
            await Task.Delay(1000);
            WsClient.Instance.Client.SafeClose();
            WebSocketClient client = WsClient.Instance;// 获取实例
            TouchSocketConfig config = new TouchSocketConfig()
               .ConfigureContainer(a =>
               {
                   a.AddConsoleLogger();
               }).ConfigurePlugins(a =>
               {
                   a.UseWebSocketHeartbeat()//使用心跳插件
                   .SetTick(TimeSpan.FromSeconds(1));//每1秒ping一次。
               })
               .SetRemoteIPHost(WebSocketUrl);
            try
            {
               
                client.Setup(config);
                await client.ConnectAsync();
                WsClient.connectUrl = WebSocketUrl;
                WsClient.ConnectStatus = "已连接";
                WsClient.ConnectTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                client.Received += receiveMessage;
                CanConnet = true;    
            }
            #region
            catch (WebSocketConnectException ex)
            {
                WsClient.ConnectStatus = "连接失败";
                PongMessage = "连接服务器" + "异常:" + ex.Message;
                client.Logger.Error(ex.Message);
            }
            catch (AggregateException ae)
            {
                ae.Flatten().Handle(ex =>
                {
                    WsClient.ConnectStatus = "连接失败";
                    // 对每个异常进行处理  
                    Console.WriteLine(ex.Message);
                    PongMessage = "连接服务器" + "异常:" + ex.Message;
                    // 返回true表示该异常已处理，不需要再次抛出  
                    return true;
                });
            }
            catch (Exception ex)
            {
                WsClient.ConnectStatus = "连接失败";
                PongMessage = "连接服务器" + "异常:" + ex.Message;
            }
            #endregion
            CanConnet = true;
            await StartAutoReconnectAsync(WsClient.Instance);
            if (!client.Client.Online)
            {
                PongMessage = "未连接";
            }
            CanConnet = true;
        }

        public Task receiveMessage(WebSocketClient client, WSDataFrameEventArgs e)
        {
            {
                switch (e.DataFrame.Opcode)
                {
                    case WSDataType.Cont:
                        return MyAsyncMethod(e.DataFrame.ToText()); // 返回一个空值
                    case WSDataType.Text:
                        // 处理文本数据
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _log.Add(new Message(_log.Length + 1, "服务端", e.DataFrame.ToText(), "succes", client.Client.GetIPPort(), client.Client.IP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                        });
                        return MyAsyncMethod(e.DataFrame.ToText());
                    case WSDataType.Binary:
                        // 处理二进制数据
                        return MyAsyncMethod(e.DataFrame.ToText());
                    case WSDataType.Close:
                        PongMessage = "服务器已手动断开连接";
                        return MyAsyncMethod(e.DataFrame.ToText());
                    case WSDataType.Ping:
                        // 处理 Ping 事件
                        return MyAsyncMethod(e.DataFrame.ToText());
                    case WSDataType.Pong:
                        // 处理 Pong 事件
                        return PongTask();
                    default:
                        return MyAsyncMethod(e.DataFrame.ToText() + "默认参数"); // 处理未知事件类型
                }
            };
        }

        private async Task StartAutoReconnectAsync(WebSocketClient client)
        {
            
            await Task.Run(async () =>
            {
                while (IsAutoConnect&& CanConnet)
                {
                    int i = 1;
                    //CanConnet = false;
                    while (!client.Client.Online&& IsAutoConnect)
                    {
                        await Application.Current.Dispatcher.Invoke(async () =>
                        {
                            var errorMessage = "";
                            try
                            {
                                client.Client.SafeClose();      
                                await client.ConnectAsync();
                                PongMessage = "重新连接成功";
                                client.Received += receiveMessage;
                                if (client != WsClient.Instance)
                                {
                                    MessageBox.Show("警告实例不同");
                                }
                                WsClient.ConnectStatus = "已连接";
                                WsClient.ConnectTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                return;
                            }
                            catch (SocketException se)
                            {
                                errorMessage = se.Message;
                            }
                            catch (AggregateException ae)
                            {
                                errorMessage = ae.Message;
 
                            }catch(NotConnectedException ne)
                            {
                                errorMessage = ne.Message;
                            }
                            catch (OperationCanceledException oe)
                            {
                                errorMessage = oe.Message;
                            }
                            catch (Exception e)
                            {
                                errorMessage = e.Message;
                            }
                            WsClient.ConnectStatus = "连接失败";
                            if (i == 1)
                            {
                                PongMessage = "连接错误："+ errorMessage+"首次尝试重连";
                            }
                            else if (i <= 100)
                            {
                                PongMessage = "重连错误:" + errorMessage + "再次重连，" + "重连次数：" + i;
                            }
                            else
                            {
                                PongMessage = "重连次数大于100，停止重连";
                            }
                            i++;
                            await Task.Delay(4000);
                        });
                        if (i > 100)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                PongMessage = "重连次数大于100，停止重连";

                            });
                            break;
                        }
                    }
                    if (i > 100)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            PongMessage = "自动重连失败，请重新手动连接";
                            CanConnet = true;
                        });
                        break;
                    }
                }
            });
        }

        public async void Close()
        {
                CanConnet = false;
                await Task.Delay(1000);
                var wsClient = WsClient.Instance;
                wsClient.Client.SafeClose();
                MessageBox.Show("已关闭连接");
                PongMessage = "已关闭连接";
                CanConnet = true;

        }
        //清空日志
        public void ClearLog()
        {
            _log.Clear();
        }
        public async Task PongTask()
        {
            // 异步操作代码
            PongMessage = "心跳中"+DateTime.Now.ToString();
            await WsClient.Instance.SendAsync("{'id': 'stringst'");
            WsClient.ConnectTime = PongMessage;
        }
        public async Task<string> MyAsyncMethod(string Message)
        {
            return "Hello World!";
        }


        // 连接命令
        public DelegateCommand ConnectCommand { get; private set; }

        public DelegateCommand CloseCommand { get; private set; }

        //发送消息命令
        public DelegateCommand SendMessageCommand { get; private set; }

        public DelegateCommand ClearLogCommand { get ; private set;}


    }
}
