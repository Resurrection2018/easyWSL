using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace easyWslLib
{

    public class DockerDownloader
    {
        private const long MAX_LAYER_SIZE = 5L * 1024 * 1024 * 1024; // 5 GB per layer
        private const long MAX_TOTAL_SIZE = 20L * 1024 * 1024 * 1024; // 20 GB total
        private const int MAX_LAYERS = 100; // Maximum number of layers
        
        private Helpers helpers = new();

        private List<string> layersPaths = new();

        private readonly string tmpDirectory;
        private readonly IPlatformHelpers platformHelpers;

        public DockerDownloader(string tmpDirectory, IPlatformHelpers platformHelpers)
        {
            this.tmpDirectory = tmpDirectory;
            this.platformHelpers = platformHelpers;
        }


        public class AuthorizationResponse
        {
            public required string Token { get; set; }
            public required string AccessToken { get; set; }

            public int ExpiresIn { get; set; }

            public required string IssuedAt { get; set; }

        }

        public class DockerException : Exception
        {
            public DockerException()
            {
            }
        }

        public async Task DownloadImage(string distroImage)
        {
            if (!Directory.Exists(tmpDirectory))
            {
                Directory.CreateDirectory(tmpDirectory);
            }

            DirectoryInfo tmpDirectoryInfo = new DirectoryInfo(tmpDirectory);
            foreach (FileInfo file in tmpDirectoryInfo.EnumerateFiles())
            {
                file.Delete();
            }


            string repository = "";
            string tag = "";
            string registry = "registry-1.docker.io";
            string authorizationUrl = "https://auth.docker.io/token";
            string registryUrl = "registry.docker.io";

            if (distroImage.Contains('/'))
            {
                string[] imageArray = distroImage.Split('/');
                if (imageArray.Length < 2)
                {
                    throw (new DockerException());
                }

                if (imageArray[1].Contains(':'))
                {
                    tag = imageArray[1].Split(':')[1];
                    repository = distroImage.Split(':')[0];
                }
                else
                {
                    tag = "latest";
                    repository = distroImage;
                }
            }
            else
            {
                string[] imageArray = distroImage.Split(':');
                if (imageArray.Length < 2)
                {
                    throw (new DockerException());
                }
                string image = imageArray[0];
                tag = imageArray[1];
                repository = $"library/{image}";
            }

            var authJson = helpers.GetRequest($"{authorizationUrl}?service={registryUrl}&scope=repository:{repository}:pull");
            var authorizationResponse = JsonSerializer.Deserialize<AuthorizationResponse>(authJson);

            if (authorizationResponse == null || string.IsNullOrEmpty(authorizationResponse.Token))
            {
                throw new DockerException();
            }

            string layersResponse;
            try
            {
                layersResponse = helpers.GetRequestWithHeader($"https://{registry}/v2/{repository}/manifests/{tag}", authorizationResponse.Token, "application/vnd.docker.distribution.manifest.v2+json");
                
                if (string.IsNullOrEmpty(layersResponse))
                {
                    throw new DockerException();
                }
            }
            catch (WebException)
            {
                throw new DockerException();
            }

            MatchCollection layersRegex = Regex.Matches(layersResponse, @"sha256:\w{64}");
            var layersList = layersRegex.Cast<Match>().Select(match => match.Value).ToList();
            layersList.RemoveAt(0);

            MatchCollection layersSizeRegex = Regex.Matches(layersResponse, @"""size"": \d*");
            var layersSizeList = layersSizeRegex.Cast<Match>().Select(match => Convert.ToInt64(match.Value.Remove(0, 8))).ToList();

            // Security: Validate layer count
            if (layersList.Count > MAX_LAYERS)
            {
                SecurityLogger.LogSecurityEvent(
                    "DOCKER_DOWNLOAD_BLOCKED",
                    $"Too many layers: {layersList.Count} (max {MAX_LAYERS})",
                    isSuccess: false
                );
                throw new DockerException();
            }
            
            // Security: Validate total download size
            long totalSize = layersSizeList.Sum();
            if (totalSize > MAX_TOTAL_SIZE)
            {
                long totalGB = totalSize / 1024 / 1024 / 1024;
                long maxGB = MAX_TOTAL_SIZE / 1024 / 1024 / 1024;
                SecurityLogger.LogSecurityEvent(
                    "DOCKER_DOWNLOAD_BLOCKED",
                    $"Image too large: {totalGB} GB (max {maxGB} GB)",
                    isSuccess: false
                );
                throw new DockerException();
            }
            
            // Security: Validate individual layer sizes
            for (int i = 0; i < layersSizeList.Count; i++)
            {
                if (layersSizeList[i] > MAX_LAYER_SIZE)
                {
                    long layerGB = layersSizeList[i] / 1024 / 1024 / 1024;
                    long maxGB = MAX_LAYER_SIZE / 1024 / 1024 / 1024;
                    SecurityLogger.LogSecurityEvent(
                        "DOCKER_DOWNLOAD_BLOCKED",
                        $"Layer {i+1} too large: {layerGB} GB (max {maxGB} GB)",
                        isSuccess: false
                    );
                    throw new DockerException();
                }
            }

            Trace.WriteLine(tmpDirectory);
            Directory.CreateDirectory(tmpDirectory);

            int layersCount = 0;
            foreach (string layer in layersList)
            {
                layersCount++;

                authJson = helpers.GetRequest($"{authorizationUrl}?service={registryUrl}&scope=repository:{repository}:pull");
                authorizationResponse = JsonSerializer.Deserialize<AuthorizationResponse>(authJson);
                
                if (authorizationResponse == null || string.IsNullOrEmpty(authorizationResponse.Token))
                {
                    throw new DockerException();
                }

                string layerName = $"layer{layersCount}.tar.bz";
                string layerPath = $"{tmpDirectory}\\{layerName}";

                layersPaths.Add(layerPath);

                var headers = new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>(HttpRequestHeader.Authorization.ToString(), $"Bearer {authorizationResponse.Token}"),
                    new KeyValuePair<string, string>(HttpRequestHeader.Accept.ToString(), "application/vnd.docker.distribution.manifest.v2+json"),
                };
                await platformHelpers.DownloadFileAsync(new Uri($"https://{registry}/v2/{repository}/blobs/{layer}"), headers, 
                    new FileInfo(Path.Combine(tmpDirectory, layerName)));
            }
        }

        public async Task CombineLayers()
        {
            var installTarPath = Path.Combine(tmpDirectory, "install.tar.bz");

            if (layersPaths.Count == 1)
            {
                await platformHelpers.CopyFileAsync(layersPaths[0], installTarPath);
            }
            else
            {
                string concatTarCommand = $" cf {installTarPath}";
                foreach (var layerPath in layersPaths)
                {
                    concatTarCommand += $" @{layerPath}";
                }
                Trace.WriteLine(concatTarCommand);
                await helpers.ExecuteProcessAsynch(platformHelpers.TarCommand, concatTarCommand);
                Trace.WriteLine("combining completed");
            }
        }
    }
}
