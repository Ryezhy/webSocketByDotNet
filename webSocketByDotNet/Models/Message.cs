using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WxAppPlugin.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Time { get; set; }

        public Message(int id, string content, string sender, string receiver, string status, string type, string time)
        {
            Id = id;
            Content = content;
            Sender = sender;
            Receiver = receiver;
            Status = status;
            Type = type;
            Time = time;
        }


        public override string ToString()
        {
            return $"{Time} {Sender} {Receiver} {Type} {Content}";
        }
    }
}
