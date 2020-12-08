// Copyright (c) Microsoft. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.  

using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Security.KeyVault.Keys.Cryptography;
using AzureCognitiveSearch.PowerSkills.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace AzureCognitiveSearch.PowerSkills.Utils.DecryptBlobFile
{
    public static class DecryptBlobFile
    {
        [FunctionName("decrypt-blob-file")]
        public static IActionResult RunDecryptBlobFile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext executionContext)
        {
            log.LogInformation("DecryptBlobFile Custom Skill: C# HTTP trigger function processed a request.");

            string skillName = executionContext.FunctionName;
            IEnumerable<WebApiRequestRecord> requestRecords = WebApiSkillHelpers.GetRequestRecords(req);
            if (requestRecords == null)
            {
                return new BadRequestObjectResult($"{skillName} - Invalid request record array.");
            }

            // Set up access to keyvault to retrieve the key to decrypt the document with
            // Requires that this Azure Function has access via managed identity to the Keyvault where the key is stored.
            var credential = new DefaultAzureCredential();
            var cloudResolver = new KeyResolver(credential);
            var encryptionOptions = new ClientSideEncryptionOptions(ClientSideEncryptionVersion.V1_0)
            {
                KeyResolver = cloudResolver
            };

            WebApiSkillResponse response = WebApiSkillHelpers.ProcessRequestRecords(skillName, requestRecords,
                (inRecord, outRecord) =>
                {
                    string blobUrl = (inRecord.Data.TryGetValue("blobUrl", out object blobUrlObject) ? blobUrlObject : null) as string;
                    string sasToken = (inRecord.Data.TryGetValue("sasToken", out object sasTokenObject) ? sasTokenObject : null) as string;

                    if (string.IsNullOrWhiteSpace(blobUrl))
                    {
                        outRecord.Errors.Add(new WebApiErrorWarningContract() { Message = $"Parameter '{nameof(blobUrl)}' is required to be present and a valid uri." });
                        return outRecord;
                    }

                    var clientOptions = new SpecializedBlobClientOptions
                    {
                        ClientSideEncryption = encryptionOptions
                    };
                    var blobUriBuilder = new BlobUriBuilder(new Uri((blobUrl)));
                    var blob = new BlobServiceClient(new Uri(WebApiSkillHelpers.CombineSasTokenWithUri(blobUrl, sasToken)), clientOptions)
                        .GetBlobContainerClient(blobUriBuilder.BlobContainerName)
                        .GetBlobClient(blobUriBuilder.BlobName);
                    byte[] decryptedFileData;
                    using (var np = new MemoryStream())
                    {
                        blob.DownloadTo(np);
                        decryptedFileData = np.ToArray();
                    }
                    FileReference decryptedFileReference = new FileReference()
                    {
                        data = Convert.ToBase64String(decryptedFileData)
                    };
                    JObject jObject = JObject.FromObject(decryptedFileReference);
                    jObject["$type"] = "file";
                    outRecord.Data["decrypted_file_data"] = jObject;
                    return outRecord;
                });
            return new OkObjectResult(response);
        }
    }
}
