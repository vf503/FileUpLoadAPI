using FileUpLoadAPI.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;

namespace FileUpLoadAPI.Controllers
{
    public class FilesController : ApiController
    {
        private const string UploadFolder = "uploads";

        public class PutModel
        {
            public string ProjectId { get; set; }
            public string action { get; set; }
            public string source { get; set; }
            public string target { get; set; }
        }

        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2","valueX" };
        }
        
        public HttpResponseMessage Get(string fileName)
        {
            HttpResponseMessage result = null;
            var appSettings = ConfigurationManager.AppSettings;

            DirectoryInfo directoryInfo = new DirectoryInfo(HostingEnvironment.MapPath("~/App_Data/" + UploadFolder));
            FileInfo foundFileInfo = directoryInfo.GetFiles().Where(x => x.Name == fileName).FirstOrDefault();
            if (foundFileInfo != null)
            {
                FileStream fs = new FileStream(foundFileInfo.FullName, FileMode.Open);

                result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new StreamContent(fs);
                result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                result.Content.Headers.ContentDisposition.FileName = foundFileInfo.Name;
            }
            else
            {
                result = new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return result;
        }

        public HttpResponseMessage Post([FromBody]PutModel value)
        {
            HttpResponseMessage result = null;
            var appSettings = ConfigurationManager.AppSettings;
            string SourcePath = appSettings["CourseDirectory"]+ value.source;
            string TargetPath = appSettings["CourseDirectory"]+ value.target;
            if (value.action == "CourseVideoMove")
            {
                StringBuilder sb = new StringBuilder();
                StringWriter sw = new StringWriter(sb);
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartObject();
                    //video+pic
                    writer.WritePropertyName("video");
                    if (File.Exists(SourcePath + value.ProjectId + ".mp4") && File.Exists(SourcePath + value.ProjectId + ".jpg"))
                    {
                        //建目录
                        if (Directory.Exists(TargetPath))
                        {
                            if (File.Exists(TargetPath + @"raw.mp4"))
                            {
                                File.Delete(TargetPath + @"raw.mp4");
                            }
                            if (File.Exists(TargetPath + @"rip.mp4"))
                            {
                                File.Delete(TargetPath + @"rip.mp4");
                            }
                            if (File.Exists(TargetPath + @"raw.jpg"))
                            {
                                File.Delete(TargetPath + @"raw.jpg");
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(TargetPath);
                        }
                        File.Move(SourcePath + value.ProjectId + ".mp4", TargetPath + "raw.mp4");
                        writer.WriteValue("1");
                        //pic
                        writer.WritePropertyName("pic");
                        File.Move(SourcePath + value.ProjectId + ".jpg", TargetPath + "raw.jpg");
                        writer.WriteValue("1");
                        //doc
                        writer.WritePropertyName("doc");
                        if (File.Exists(SourcePath + value.ProjectId + ".zip"))
                        {
                            if (File.Exists(TargetPath + @"slide0.zip"))
                            {
                                File.Delete(TargetPath + @"slide0.zip");
                            }
                            File.Move(SourcePath + value.ProjectId + ".zip", TargetPath + "slide0.zip");
                            writer.WriteValue("1");
                        }
                        else { writer.WriteValue("0"); }
                    }
                    else { writer.WriteValue("0"); }
                    writer.WriteEnd();
                    //writer.WriteEndObject();
                }
                result = new HttpResponseMessage(HttpStatusCode.Accepted);
                result.Content = new StringContent(sb.ToString(), System.Text.Encoding.GetEncoding("UTF-8"), "application/json");
            }
            else if (value.action == "CourseDirectoryRename")
            {

            }
            else { }
            return result;
        }

        public Task<IQueryable<HDFile>> Post(string id)
        {
            var appSettings = ConfigurationManager.AppSettings;
            try
            {
            //uploadFolderPath variable determines where the files should be temporarily uploaded into server. 
            //Remember to give full control permission to IUSER so that IIS can write file to that folder.

            //string uploadFolderPath = HostingEnvironment.MapPath("~" + appSettings["CourseDirectory"] + "/" + id.Substring(0, 4) + "/" + id);
            string uploadFolderPath = appSettings["CourseDirectory"] + "\\" + id.Substring(0, 4) + "\\" + id;
            //如果路径不存在，创建路径
            if (!Directory.Exists(uploadFolderPath))
                    Directory.CreateDirectory(uploadFolderPath);

                //#region CleaningUpPreviousFiles.InDevelopmentOnly
                //DirectoryInfo directoryInfo = new DirectoryInfo(uploadFolderPath);
                //foreach (FileInfo fileInfo in directoryInfo.GetFiles())
                //	fileInfo.Delete();
                //#endregion

                if (Request.Content.IsMimeMultipartContent()) //If the request is correct, the binary data will be extracted from content and IIS stores files in specified location.
                {
                    var context = System.Web.HttpContext.Current.Request;
                    var streamProvider = new WithExtensionMultipartFormDataStreamProvider(uploadFolderPath, context["name"]);
                    var task = Request.Content.ReadAsMultipartAsync(streamProvider).ContinueWith<IQueryable<HDFile>>(t =>
                    {
                        if (t.IsFaulted || t.IsCanceled)
                        {
                            throw new HttpResponseException(HttpStatusCode.InternalServerError);
                        }

                        var fileInfo = streamProvider.FileData.Select(i =>
                        {
                            var info = new FileInfo(i.LocalFileName);
                            return new HDFile(info.Name, string.Format("{0}?filename={1}", Request.RequestUri.AbsoluteUri, info.Name), (info.Length / 1024).ToString());
                        });
                        return fileInfo.AsQueryable();
                    });

                    return task;
                }
                else
                {
                    throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotAcceptable, "This request is not properly formatted"));
                }
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }


        public class HDFile
        {
            public HDFile(string name, string url, string size)
            {
                Name = name;
                Url = url;
                Size = size;
            }

            public string Name { get; set; }

            public string Url { get; set; }

            public string Size { get; set; }
        }

    }

    
}
