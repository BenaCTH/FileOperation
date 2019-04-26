
namespace GuiCommon.Web.Controllers
{
   
    public class CommonController : ApiBaseController
    {
        [HttpPost("UploadSegmentFile")]
        [RequestSizeLimit(long.MaxValue)]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> UploadSegmentFile()
        {   //==Form file Info ==
            var formFile = Request.Form.Files["file"];
            string correlationId = Request.Form["correlationId"];
            var total = Convert.ToInt32(Request.Form["totalCount"]);
            var fileName = Request.Form["fileName"];
            var index = Convert.ToInt32(Request.Form["index"]);
            string scopePath = Request.Form["scopePath"];
            string category = Request.Form["category"];

            var currentUserId = HttpContext.CurrentUserId();
            var fileControlSetting = _commonQueryService.GetFileControl();
            var newFileInfos = new List<UploadFileResponseInfo>();
            FileResponseModel result = new FileResponseModel();

            logger.Info($"Begin to upload file. File count:{total}, current user:{HttpContext.CurrentUserName()}.");

            #region ==== Validate File Info =====
            if (index == 1)
            {
                UploadStatus validationResult = UploaderHelper.ValidationUploadFile(formFile.FileName, formFile.OpenReadStream(), fileControlSetting, true);
                if (validationResult != UploadStatus.ValidateSuccessful)
                {
                    logger.Warn($"Uploaded file is invalid. File name {formFile.Name}, size:{formFile.Length}, result:{validationResult}.");
                    return Request.OK(new { msg = I18NEntity.GetString("GC_Common_Uploader_Failed_Message"), status = validationResult });
                }
            }
            #endregion

            try
            {
                UploadFileRequestInfo info = new UploadFileRequestInfo()
                {
                    ScopePath = !string.IsNullOrEmpty(scopePath) ? scopePath : ScopePath,
                    Category = !string.IsNullOrEmpty(category) ? category : Category,
                    FileName = fileName,
                    TotalCount = total,
                    CorrelationId = !string.IsNullOrEmpty(correlationId) ? Guid.Parse(correlationId) : Guid.Empty,
                    Index = index
                };

                if (total > 1)
                {
                    logger.Info($"Uploading Segment file {formFile.Name} to lcms.TotalCount:{info.TotalCount},current index:{info.Index}, CorrelationId:{info.CorrelationId}.");
                }
                else
                {
                    logger.Info($"Uploading file {formFile.Name} to lcms.");
                }

                UploadFileResponseInfo o = await _uploaderService.UploadFile(info, new Refit.StreamPart(formFile.OpenReadStream(), formFile.FileName));
                result.Info = o;
                result.FileNumber = index;
                if (o.IsSucceed)
                {
                    //total file upload successfully
                    if (o.FileId != Guid.Empty)
                    {
                        logger.Info($"Uploading file {formFile.Name} to lcms succeed. File id:{o.FileId}.");
                        result.MergeResult = true;
                        o.Category = Category;
                        result.ResponseFiles.Add(o);
                    }
                    else
                    {
                        result.MergeResult = false;
                    }
                }
                else
                {
                    logger.Warn($"Uploading file {formFile.Name} to lcms failed. File id:{o.FileId}, message:{o.ErrorMsg}.");
                    result.Msg = o.ErrorMsg;
                }
            }
            catch (TaskCanceledException ex)
            {
                logger.Error("Connection timed out and file upload failed. ", ex);
                return Request.OK(new { msg = I18NEntity.GetString("GC_Common_Uploader_TimeOut_Message") });
            }
            catch (ValidationException ex)
            {
                logger.Error("Validation file upload failed. ", ex);
                return Request.OK(new { msg = I18NEntity.GetString("GC_Common_Uploader_Failed_Message"), ex.Status });
            }
            catch (Exception ex)
            {
                logger.Error("Upload file to lcms server failed. ", ex);
            }

            return Request.OK(result);
        }       
    
        [HttpGet("DownloadFile")]
        [AllowAnonymous]
        [RequestSizeLimit(long.MaxValue)]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<FileResult> DownloadFileAsync(Guid fileId)
        {
            FileResult fileResult = null;
            double maxFileSize = 1932735283;//1.8GB
            try
            {
                logger.Info($"Begin search file by id:{fileId.ToString()}]");
                QueryFileInfo queryInfo = new QueryFileInfo();
                queryInfo.FileIds = new List<Guid>();
                queryInfo.FileIds.Add(fileId);
                SearchResults result = await SearchFileInfoById(fileId);
                UploadFileResponseInfo fileInfo;
                if (result.Data.Count > 0)
                {
                    fileInfo = result.Data.FirstOrDefault();
                    logger.Info($"search file info: fileId{fileInfo.FileId}],filename:[{fileInfo.FileName}],filesize:[{fileInfo.FileSize}]");
                    if (fileInfo.FileSize < maxFileSize)
                    {
                        logger.Info($"Go to default download, fileId:{fileInfo.FileId}]");
                        fileResult = await DownloadFile(fileId);
                    }
                    else
                    {
                        logger.Info($"Go to HttpWebRequest download, fileId:{fileInfo.FileId}]");
                        fileResult = HttpWebDownFile(fileInfo);
                    }
                }
                else
                {
                    logger.Info($"No file was found, fileId:{fileId}]");
                }
            }
            catch (Exception e)
            {
                logger.Error(string.Format("An error occurred while download file with file id : '{0}'. error : {1}.", fileId, e.ToString()));
            }
            return fileResult;
        }

        #region ===== Download File Method =====

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileId">Download File ID</param>
        /// <returns></returns>
        private async Task<FileResult> DownloadFile(Guid fileId)
        {
            HttpResponseMessage response = await _uploaderService.DownloadFile(fileId);
            string contentType = "application/x-msdownload";
            if (!response.Content.Headers.ContentType.ToString().Equals("application/octet-stream"))
            {
                contentType = response.Content.Headers.ContentType.ToString();
            }
            return File(response.Content.ReadAsByteArrayAsync().Result, contentType, response.Content.Headers.ContentDisposition.FileNameStar);
        }

        /// <summary>
        ///  下载文件超过1.8GB时，通过HttpWebRequest的方式下载文件
        /// </summary>
        /// <param name="filename">Download File Name</param>
        /// <param name="fileId">Download File ID</param>
        /// <returns></returns>
        private FileResult HttpWebDownFile(UploadFileResponseInfo FileInfo)
        {
            FileResult result = null;
            string fileId = FileInfo.FileId.ToString();
            string fileName = FileInfo.FileName;
            logger.Info($"HttpWebDownFile: Bengin download file , file id:[{fileId}]");
            try
            {
                string LCMSHost = _configuration["LCMSHost"].TrimEnd('/');

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create($"{LCMSHost}/api/fileapi/Download?fileId={fileId}");
                request.Timeout = 30 * 60 * 1000;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                string contentType = "application/x-msdownload";
                var provider = new FileExtensionContentTypeProvider();
                if (provider.Mappings.ContainsKey(FileInfo.ExtensionName))
                {
                    contentType = provider.Mappings[FileInfo.ExtensionName];
                }
                Response.ContentLength = FileInfo.FileSize;
                result = File(response.GetResponseStream(), contentType, fileName);

                logger.Info($"HttpWebDownFile: End download stream , {DateTime.Now} {fileName}");
            }
            catch (Exception ex)
            {
                logger.Error($"HttpWebDownFile: Download file error:{ex.ToString()}");
            }
            return result;
        }

        /// <summary>
        /// Search File Info By File ID
        /// </summary>
        /// <param name="fileId">Search File ID</param>
        /// <returns></returns>
        private async Task<SearchResults> SearchFileInfoById(Guid fileId)
        {
            try
            {
                var fileIds = new Guid[1] { fileId };
                logger.Info($"Query UploadFilesByIds. FileIds:[{string.Join(',', fileIds)}].");
                SearchFileConditionModel model = new SearchFileConditionModel
                {
                    FileIds = fileIds
                };

                SearchResults result = await _uploaderService.SearchFileInfo(model);
                return result;
            }
            catch (Exception ex)
            {
                logger.Error("Query UploadFilesByIds failed. ", ex);
                return null;
            }

        }
        #endregion
	
	//Upload File to tempFile,then merge file 
	[HttpPost("UploadFileTemp")]
        [DisableFormValueModelBinding]
        [RequestSizeLimit(long.MaxValue)]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> UploadFileTemp()
        {
            var data = Request.Form.Files["file"];
            string lastModified = Request.Form["lastModified"].ToString();
            var total = Convert.ToInt32(Request.Form["total"]);
            var fileName = Request.Form["fileName"];
            var index = Convert.ToInt32(Request.Form["index"]);
            MergeFileModel result = new MergeFileModel();
            string temporary = Path.Combine(@"D:\tempFiles", lastModified);//临时保存分块的目录
            try
            {
                if (!Directory.Exists(temporary))
                    Directory.CreateDirectory(temporary);
                string filePath = Path.Combine(temporary, index.ToString());
                if (!Convert.IsDBNull(data))
                {
                    await Task.Run(() =>
                    {
                        using (var fs = new FileStream(filePath, FileMode.Create))
                        {
                            data.CopyTo(fs);
                        }
                    });
                }

                if (total == index)
                {
                    result = await FileMerge(lastModified, fileName);
                }
                result.FileNumber = index;
            }
            catch (TaskCanceledException ex)
            {
                logger.Error("Connection timed out and file upload failed. ", ex);
                return Request.OK(new { msg = I18NEntity.GetString("GC_Common_Uploader_TimeOut_Message") });
            }
            catch (ValidationException ex)
            {
                logger.Error("Validation file upload failed. ", ex);
                return Request.OK(new { msg = I18NEntity.GetString("GC_Common_Uploader_Failed_Message"), ex.Status });
            }
            catch (Exception ex)
            {
                logger.Error("Upload file to lcms server failed. ", ex);
            }

            return Request.OK(result);
        }

        public async Task<MergeFileModel> FileMerge(string lastModified, string fileName)
        {
            MergeFileModel result = new MergeFileModel();
            try
            {
                var temporary = Path.Combine(@"D:\tempFiles", lastModified);//临时文件夹
                string fileExt = Path.GetExtension(fileName);//获取文件后缀
                var files = Directory.GetFiles(temporary);//获得下面的所有文件
                var finalPath = Path.Combine(@"D:\tempFiles", lastModified, fileName);//最终的文件名
                string scopePath = Request.Form["scopePath"];
                string category = Request.Form["category"];

                UploadFileRequestInfo info = new UploadFileRequestInfo()
                {
                    ScopePath = !string.IsNullOrEmpty(scopePath) ? scopePath : ScopePath,
                    Category = !string.IsNullOrEmpty(category) ? category : Category
                };

                UploadFileResponseInfo o;
                using (var fs = new FileStream(finalPath, FileMode.Create))
                {
                    foreach (var part in files.OrderBy(x => x.Length).ThenBy(x => x))//排一下序，保证从0-N Write
                    {
                        var bytes = System.IO.File.ReadAllBytes(part);
                        await fs.WriteAsync(bytes, 0, bytes.Length);
                        bytes = null;
                        System.IO.File.Delete(part);//删除分块
                    }
                    fs.Position = 0;
                    logger.Info($"Merge file successfully, {DateTime.Now} {fileName}, size {fs.Length}");

                    logger.Info($"Bengin send stream to lcsm, {DateTime.Now} {fileName}, size {fs.Length}");

                    o = await _uploaderService.UploadFile(info, new Refit.StreamPart(fs, fileName));

                    logger.Info($"End send stream to lcsm, {DateTime.Now} {fileName}");

                    result.MergeResult = true;
                }
                try
                {
                    if (System.IO.File.Exists(finalPath))
                    {
                        System.IO.File.Delete(finalPath);
                        logger.Info($"delete temp file successfully , {finalPath}");
                    }
                    if (Directory.Exists(temporary))
                    {
                        Directory.Delete(temporary);//删除文件夹 
                        logger.Info($"delete temp folder successfully , {temporary}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"delete temp folder error : {ex.ToString()}");
                }

                if (o.IsSucceed)
                {
                    o.Category = Category;
                    result.ResponseFiles.Add(o);
                }
                else
                {
                    result.Msg = o.ErrorMsg;
                    result.Info = o;
                }

            }
            catch (Exception ex)
            {
                logger.Error($"merge file error : {ex.ToString()}");
            }
            return result;
        }



	}
    public class FileResponseModel
    {
        public bool MergeResult { get; set; }
        public int FileNumber { get; set; }
        public string Msg { get; set; }
        public List<UploadFileResponseInfo> ResponseFiles { get; set; }
        public UploadFileResponseInfo Info { get; set; }

        public FileResponseModel()
        {
            MergeResult = false;
            FileNumber = 0;
            ResponseFiles = new List<UploadFileResponseInfo>();
        }
    }
}
