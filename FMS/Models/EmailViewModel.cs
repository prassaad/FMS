using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FMS.Models
{
    [Serializable]
    public class WebEmail
    {
        public WebEmail()
        {
            this.FileAttachments = new List<FileAttachments>();
        }
        public int MessageNumber { get; set; }
        public string From { get; set; }
        public string SenderId { get; set; }
        public string Subject { get; set; }
        public string To { get; set; }
        public string CC { get; set; }
        public string BCC { get; set; }
        public string Body { get; set; }
        public DateTime DateSent { get; set; }
        public List<FileAttachments> FileAttachments { get; set; }
    }
    [Serializable]
    public class FileAttachments
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
    }
}