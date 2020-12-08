// Copyright (c) Microsoft. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.  

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureCognitiveSearch.PowerSkills.Vision.ImageStore
{
    public class BlobImageStore
    {
        private readonly BlobContainerClient _libraryContainer;

        public BlobImageStore(string blobConnectionString, string containerName)
        {
            var blobServiceClient = new BlobServiceClient(blobConnectionString);
            _libraryContainer = blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadImageToLibraryAsync(Stream stream, string name, string mimeType, bool overwrite = false)
        {
            var blockBlob = _libraryContainer.GetBlobClient(name);
            if (!await blockBlob.ExistsAsync())
            {
                await blockBlob.UploadAsync(
                    stream,
                    new BlobHttpHeaders
                    {
                        ContentType = mimeType
                    });
            }

            return blockBlob.Uri.ToString();
        }

        public async Task<string> UploadToBlobAsync(byte[] data, string name, string mimeType, bool overwrite = false)
            => await UploadImageToLibraryAsync(new MemoryStream(data), name, mimeType, overwrite);

        public async Task<string> UploadToBlobAsync(Image image, bool overwrite = false)
            => await UploadToBlobAsync(image.Data, image.Name, image.MimeType, overwrite);

        public async Task<string> UploadToBlobAsync(string data, string name, string mimeType, bool overwrite = false)
            => await UploadToBlobAsync(Convert.FromBase64String(data), name, mimeType, overwrite);

        public async Task<Image> DownloadFromBlobAsync(string imageUrl)
        {
            var imageName = new Uri(imageUrl).Segments.Last();
            var blockBlob = _libraryContainer.GetBlobClient(imageName);
            if (await blockBlob.ExistsAsync())
            {
                await using var stream = new MemoryStream();
                string mimeType = (await blockBlob.GetPropertiesAsync()).Value.ContentType;
                await blockBlob.DownloadToAsync(stream);
                byte[] data = stream.ToArray();
                return new Image(imageName, data, mimeType);
            }
            return null;
        }
    }
}
