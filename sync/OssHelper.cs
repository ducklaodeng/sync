using Aliyun.OSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sync
{
    internal class OssHelper
    {

        private readonly OssClient _ossClient;
        private readonly string _bucketName;

        public OssHelper(string endpoint, string accessKeyId, string accessKeySecret, string bucketName)
        {
            _ossClient = new OssClient(endpoint, accessKeyId, accessKeySecret);
            _bucketName = bucketName;
        }

        public string UploadFile(string localFilePath, string ossFilePath)
        {
            if (DoesObjectExist(ossFilePath))
            {
                return "file exist";
            }

            using (var fs = File.Open(localFilePath, FileMode.Open))
            {
                var result = _ossClient.PutObject(_bucketName, ossFilePath, fs);
                return result.ETag;
            }

        }
        public bool DoesObjectExist(string ossFilePath)
        {

            return _ossClient.DoesObjectExist(_bucketName, ossFilePath);

        }
    }
}
