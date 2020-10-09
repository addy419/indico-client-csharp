﻿using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using Indico.Exception;
using System.Threading.Tasks;

namespace Indico.Storage
{
    public class UploadFile : RestRequest<JArray>
    {
        IndicoClient _client;
        List<string> _files = new List<string>();

        /// <summary>
        /// List of files to upload
        /// </summary>
        public List<string> Files
        {
            get => this._files;
            set => CheckFiles(value);
        }

        public UploadFile(IndicoClient client)
        {
            this._client = client;
        }

        void CheckFiles(List<string> files)
        {
            foreach (string path in files)
            {
                string filepath = path;
                char seperator = Path.DirectorySeparatorChar;
                char alt = Path.AltDirectorySeparatorChar;
                if (seperator != alt)
                {
                    filepath = filepath.Replace(alt, seperator);
                }

                if (File.Exists(filepath))
                {
                    this._files.Add(filepath);
                }
                else
                {
                    throw new RuntimeException($"File ({path}) does not exist");
                }
            }
        }

        /// <summary>
        /// Upload files and return metadata
        /// </summary>
        /// <returns>JArray</returns>
        async public Task<JArray> Call()
        {
            List<FileParameter> fileParameters = new List<FileParameter>();

            foreach (string filepath in this.Files)
            {
                string filename = Path.GetFileName(filepath);
                FileStream file = File.OpenRead(filepath);
                FileParameter param = new FileParameter
                {
                    File = file,
                    FilePath = filepath,
                    FileName = filename,
                    ContentType = "application/octet-stream"
                };
                fileParameters.Add(param);
            }

            MultipartFormUpload formUpload = new MultipartFormUpload(this._client)
            {
                FileParameters = fileParameters
            };
            JArray uploadResult = await formUpload.Call();

            // Dispose FileStreams
            fileParameters.ForEach(param => param.File.Dispose());

            foreach (JObject uploadMeta in uploadResult)
            {
                string error = (string)uploadMeta.GetValue("error");
                if (error != null)
                {
                    string fname = (string)uploadMeta.GetValue("name");
                    string ferror = (string)uploadMeta.GetValue("error");
                    throw new FileUploadException($"File upload failed on {fname} with status {ferror}");
                }
            }

            return uploadResult;
        }
    }
}
