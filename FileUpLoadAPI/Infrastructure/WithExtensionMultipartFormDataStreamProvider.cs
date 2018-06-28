using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace FileUpLoadAPI.Infrastructure
{
    public class WithExtensionMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        public string FileName="";
        public WithExtensionMultipartFormDataStreamProvider(string rootPath,string fileName)
            : base(rootPath)
        {
            FileName = fileName;
        }

        public override string GetLocalFileName(System.Net.Http.Headers.HttpContentHeaders headers)
        {
            //string extension = !string.IsNullOrWhiteSpace(headers.ContentDisposition.FileName) ? Path.GetExtension(GetValidFileName(headers.ContentDisposition.FileName)) : "";
            //string FileName = !string.IsNullOrWhiteSpace(headers.ContentDisposition.FileName) ? Path.GetFileName(GetValidFileName(headers.ContentDisposition.FileName)) : "";
            //return Guid.NewGuid().ToString() + extension;
            return FileName;
        }

        private string GetValidFileName(string filePath)
        {
            char[] invalids = System.IO.Path.GetInvalidFileNameChars();
            return String.Join("_", filePath.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}