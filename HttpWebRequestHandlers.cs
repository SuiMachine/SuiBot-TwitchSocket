using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_TwitchSocket
{
	public static class HttpWebRequestHandlers
	{
		/// <summary>
		/// Does a request and gets a Json response.
		/// </summary>
		/// <param name="address">Url to perform a request on</param>
		/// <param name="result">JSON received as response (or empty if failed)</param>
		/// <returns>True if response was successful, false if failed.</returns>
		public static bool GrabJson(Uri address, out string result)
		{
			try
			{
				HttpWebRequest wRequest = (HttpWebRequest)HttpWebRequest.Create(address);
				wRequest.Credentials = CredentialCache.DefaultCredentials;

				dynamic wResponse = wRequest.GetResponse().GetResponseStream();
				StreamReader reader = new StreamReader(wResponse);
				result = reader.ReadToEnd();
				reader.Close();
				wResponse.Close();
				return true;
			}
			catch (Exception)
			{
				result = "";
				return false;
			}
		}

		public static string PerformGetSync(string baseUrl, string scope, string parameters, Dictionary<string, string> requestHeaders)
		{
			using HttpClient client = new();
			using HttpRequestMessage content = new(HttpMethod.Get, new Uri(baseUrl + scope + parameters));
			foreach (var header in requestHeaders)
			{
				if (string.IsNullOrEmpty(header.Key) || string.IsNullOrEmpty(header.Value))
					continue;

				if (header.Key == "Authorization")
				{
					var split = header.Value.Split([' '], 2, StringSplitOptions.TrimEntries);
					if (split.Length == 2)
					{
						client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(split[0], split[1]);
					}
					else
						client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(header.Value);
				}
				else
					content.Headers.Add(header.Key, [header.Value]);
			}

			try
			{
				var result = client.Send(content);
				if (result.IsSuccessStatusCode)
				{
					return result.Content.ReadAsStringAsync().Result;
				}

				return "";
			}
			catch (Exception e)
			{
				ErrorLoggingSocket.WriteLine($"Failed to perform get: {e}");
				ErrorLoggingSocket.WriteLine($"Url and scope were: to perform get: {baseUrl + scope}");
				return "";
			}
		}

		public static async Task<string> PerformGetAsync(string baseUrl, string scope, string parameters, Dictionary<string, string> requestHeaders, int timeout = 5000)
		{
			using HttpClient client = new()
			{
				Timeout = TimeSpan.FromSeconds(timeout)
			};
			using HttpRequestMessage content = new(HttpMethod.Get, new Uri(baseUrl + scope + parameters));
			foreach (var header in requestHeaders)
			{
				if (string.IsNullOrEmpty(header.Key) || string.IsNullOrEmpty(header.Value))
					continue;

				if (header.Key == "Authorization")
				{
					var split = header.Value.Split([' '], 2, StringSplitOptions.TrimEntries);
					if (split.Length == 2)
					{
						client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(split[0], split[1]);
					}
					else
						client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(header.Value);
				}
				else
					content.Headers.Add(header.Key, [header.Value]);
			}

			try
			{
				var result = await client.SendAsync(content);
				if (result.IsSuccessStatusCode)
				{
					return await result.Content.ReadAsStringAsync();
				}

				return "";
			}
			catch (Exception e)
			{
				ErrorLoggingSocket.WriteLine($"Failed to perform get: {e}");
				ErrorLoggingSocket.WriteLine($"Url and scope were: {baseUrl + scope}");
				return "";
			}
		}

		public static async Task<string> PerformDeleteAsync(string baseUrl, string scope, string parameters, Dictionary<string, string> headers, string contentType = "application/json", int timeout = 8000)
		{
			using HttpClient client = new()
			{
				Timeout = TimeSpan.FromSeconds(timeout)
			};
			using HttpRequestMessage content = new(HttpMethod.Delete, new Uri(baseUrl + scope + parameters));


			foreach (var header in headers)
			{
				if (string.IsNullOrEmpty(header.Key) || string.IsNullOrEmpty(header.Value))
					continue;

				if (header.Key == "Authorization")
				{
					var split = header.Value.Split([' '], 2, StringSplitOptions.TrimEntries);
					if (split.Length == 2)
					{
						client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(split[0], split[1]);
					}
					else
						client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(header.Value);
				}
				else
					content.Headers.Add(header.Key, [header.Value]);
			}


			try
			{
				var result = await client.SendAsync(content);
				if (result.IsSuccessStatusCode)
				{
					return await result.Content.ReadAsStringAsync();
				}

				return "";
			}
			catch (Exception ex)
			{
				ErrorLoggingSocket.WriteLine($"Failed to perform delete: {ex}");
				ErrorLoggingSocket.WriteLine($"Url and scope were: {baseUrl + scope + parameters}");
				return "";
			}
		}

		public static async Task<string> PerformPostAsync(string baseUrl, string scope, string parameters, string postData, Dictionary<string, string> headers, string contentType = "application/json", int timeout = 8000)
		{
			using HttpClient client = new()
			{
				Timeout = TimeSpan.FromSeconds(timeout)
			};


			using StringContent content = new(postData, Encoding.UTF8, contentType);
			foreach (var header in headers)
			{
				if (string.IsNullOrEmpty(header.Key) || string.IsNullOrEmpty(header.Value))
					continue;

				if (header.Key == "Authorization")
				{
					var split = header.Value.Split([' '], 2, StringSplitOptions.TrimEntries);
					if (split.Length == 2)
					{
						client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(split[0], split[1]);
					}
					else
						client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(header.Value);
				}
				else
					content.Headers.Add(header.Key, [header.Value]);
			}

			try
			{
				var result = await client.PostAsync(new Uri(baseUrl + scope + parameters), content);
				if (result.IsSuccessStatusCode)
				{
					return await result.Content.ReadAsStringAsync();
				}

				return "";
			}
			catch (Exception e)
			{
				ErrorLoggingSocket.WriteLine($"Failed to perform post: {e}");
				ErrorLoggingSocket.WriteLine($"Url and scope were: {baseUrl + scope + parameters}");
				ErrorLoggingSocket.WriteLine($"Content was: {postData}");
				return "";
			}
		}

		public static async Task<string> PerformPatchAsync(string baseUrl, string scope, string parameters, string patchData, Dictionary<string, string> headers, string contentType = "application/json", int timeout = 5000)
		{
			using HttpClient client = new()
			{
				Timeout = TimeSpan.FromSeconds(timeout)
			};
			using StringContent content = new(patchData, Encoding.UTF8, contentType);
			foreach (var header in headers)
			{
				if (string.IsNullOrEmpty(header.Key) || string.IsNullOrEmpty(header.Value))
					continue;

				if (header.Key == "Authorization")
				{
					var split = header.Value.Split([' '], 2, StringSplitOptions.TrimEntries);
					if (split.Length == 2)
					{
						client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(split[0], split[1]);
					}
					else
						client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(header.Value);
				}
				else
					content.Headers.Add(header.Key, [header.Value]);
			}


			try
			{
				var result = await client.PatchAsync(new Uri(baseUrl + scope + parameters), content);
				if (result.IsSuccessStatusCode)
				{
					return await result.Content.ReadAsStringAsync();
				}

				return "";
			}
			catch (Exception e)
			{
				ErrorLoggingSocket.WriteLine($"Failed to perform patch: {e}");
				ErrorLoggingSocket.WriteLine($"Url and scope were: {baseUrl + scope + parameters}");
				ErrorLoggingSocket.WriteLine($"Content was: {patchData}");
				return "";
			}
		}
	}
}
