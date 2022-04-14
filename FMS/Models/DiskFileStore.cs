using System;
using System.Web;
using AsyncUploaderDemo.Models;
using System.IO;
using System.Web.Hosting;

namespace AsyncUploaderDemo.Models
{
    internal class DiskFileStore : IFileStore
    {
        private string _uploadsFolder = HostingEnvironment.MapPath("~/content/uploadimages");

        public string  SaveUploadedFile(HttpPostedFileBase fileBase)
        {
            //var identifier = Guid.NewGuid();
            fileBase.SaveAs(GetDiskLocation(fileBase.FileName));
            return fileBase.FileName;
        }

        private string GetDiskLocation(string  identifier)
        {
            return Path.Combine(_uploadsFolder, identifier.ToString());
        }
    }
}