using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WxAppPlugin.Models;

namespace WxAppPlugin.Helper
{
    public class Log : BindableBase
    {
        private static Log log;
        public static Log Instance
        {
            get
            {
                if (log == null)
                {
                    log = new Log();
                }
                return log;
            }
        }

        private static ObservableCollection<Message> _messages;

        public ObservableCollection<Message> Messages
        {
            get
            {
                if (_messages == null)
                {
                    _messages = new ObservableCollection<Message>();
                }
                return _messages;
            }
            set
            {
                SetProperty(ref _messages, value);
            }
        }

        public int Length
        {
            get
            {
                return Messages.Count;
            }
        }
        public void Add(Message message)
        {
            Messages.Add(message);
        }

        public void Clear()
        {
            Messages.Clear();
        }
    }
}
