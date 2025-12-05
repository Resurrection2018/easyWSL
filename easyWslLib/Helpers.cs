using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace easyWslLib
{
    public class Helpers
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static Helpers()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "easyWSL/2.0");
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task<string> GetRequestAsync(string url)
        {
            try
            {
                return await _httpClient.GetStringAsync(url);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"HTTP request failed: {ex.Message}", ex);
            }
        }

        // Synchronous wrapper for backward compatibility
        public string GetRequest(string url)
        {
            return GetRequestAsync(url).GetAwaiter().GetResult();
        }

        public async Task<string> GetRequestWithHeaderAsync(string url, string token, string acceptType)
        {
            try
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptType));
                
                var response = await _httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"HTTP request failed: {ex.Message}", ex);
            }
        }

        // Synchronous wrapper for backward compatibility
        public string GetRequestWithHeader(string url, string token, string type)
        {
            return GetRequestWithHeaderAsync(url, token, type).GetAwaiter().GetResult();
        }

        public string ComputeSha256Hash(byte[] rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(rawData);

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public async Task ExecuteProcessAsynch(string exe, string arguments)
        {
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.FileName = exe;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.Arguments = arguments;
            proc.Start();
            await proc.WaitForExitAsync().ConfigureAwait(false);
        }

        public void StartWSLDistro(string distroName)
        {
            Process proc = new Process();
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.FileName = "wsl.exe";
            proc.StartInfo.Arguments = $"-d {distroName}";
            proc.Start();
        }
        public async Task ExecuteCommandInWSLAsync(string distroName, string command)
        {
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.FileName = "wsl.exe";
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.Arguments = $"-d {distroName} -- {command}";
            proc.Start();
            await proc.WaitForExitAsync().ConfigureAwait(false);
        }

        public string ExecuteProcessAndGetOutputAsynch(string exe, string arguments)
        {
            using Process process = Process.Start(new ProcessStartInfo()
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                FileName = exe,
                Arguments = arguments,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            StringBuilder outputBuilder = new();
            process.OutputDataReceived += (_, eventArgs) =>
            {
                if (!string.IsNullOrEmpty(eventArgs.Data))
                    outputBuilder.Append(eventArgs.Data);
            };
            process.BeginOutputReadLine();
            process.WaitForExit();
            return outputBuilder.ToString();
        }
        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;

            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }

            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }

    }
}
