using System;
using System.Web;

namespace AsyncUploaderDemo.Models
{
    public interface IFileStore
    {
        string  SaveUploadedFile(HttpPostedFileBase fileBase);
    }
}