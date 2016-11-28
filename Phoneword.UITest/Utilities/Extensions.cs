using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;
using Xamarin.UITest;
using Xamarin.UITest.Configuration;
using Xamarin.UITest.Queries;

namespace Phoneword.UITest
{
	public static class Extensions
	{
		public const string DEFUALT_IOS_APP_PATH = "/Users/dominostest/Projects/Phoneword/Phoneword/Phoneword.iOS/bin/iPhoneSimulator/Debug/PhonewordiOS.app";
		public const string WINDOWS_WHERE = "where";
		public const string UNIX_WHICH = "/usr/bin/which";
		public const string ADB_COMMAND = "adb";
		public const string XCRUN_COMMAND = "xcrun";
		public const string ADB_SPECIFIED = "adbSpecified";
		public const string XCRUN_SPECIFIED = "xcrunSpecified";
		public const string ANDROID_APK_SPECIFIED = "apkSpecified";
		public const string IOS_APP_SPECIFIED = "appSpecified";
		public const string ANDROID_KEYS = "android_keys";
		public const string SCREEN_LOAD_TIMEOUT = "screenLoadTimeoutSeconds";
		public const string DEFAULT_ANDROID_FILTERS = "ro.serialno|version.release|version.sdk|brand|product.model";
        public const int MAX_TIMEOUT_INSECONDS = 120;

		public static string OSVersionPlatform = null;
		public static string adbCommand = null;
		public static string adbFilters = null;
		public static string xcrunCommand = null;
		public static string androidApkFilePath = null;
		public static string iosAppFilePath = null;

		public static Dictionary<string, string[]> androids = new Dictionary<string, string[]>();
		public static Dictionary<string, string[]> iPhones = new Dictionary<string, string[]>();

		public static string DefaultTimeoutMessageTemplate = "Element is not visible after {0}s.";
		public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);
		public static TimeSpan DefaultRetryFrequency = TimeSpan.FromSeconds(1);
		public static TimeSpan ScreenLoadTimeoutInSeconds = TimeSpan.FromSeconds(30);

		public static bool IsElementVisible(this Func<AppQuery, AppQuery> query, IApp app = null)
		{
			app = app ?? FeatureContext.Current.Get<IApp>();
			AppResult[] results = app.Query(query);
			if (results.Length == 0) return false;
			AppResult firstVisible = results.FirstOrDefault(r => r.Rect.Width > 0);
			return firstVisible != default(AppResult);
		}

		public static bool WaitElementVisible(this Func<AppQuery, AppQuery> query, IApp app = null, string timeoutMessage = null, TimeSpan? timeout = null, TimeSpan? retryFrequency = null)
		{
			try
			{
				app = app ?? FeatureContext.Current.Get<IApp>();
				TimeSpan theTimeout = timeout ?? DefaultTimeout;
				TimeSpan theRetry = retryFrequency ?? DefaultRetryFrequency;
				timeoutMessage = timeoutMessage ?? string.Format(DefaultTimeoutMessageTemplate, theTimeout.TotalSeconds);
				AppResult[] results = app.WaitForElement(query, timeoutMessage, theTimeout, theRetry);
				if (results.Length == 0) return false;
				AppRect rect = results.First().Rect;
				return rect.Width > 0 || rect.Height > 0;
			}
			catch (TimeoutException)
			{
				return false;
			}
			catch (Exception e)
			{
				return false;
			}
		}

		static Extensions()
		{
			//If it is running remotely, no need to check the attached devices
			if (TestPlatform.Local != TestEnvironment.Platform)
				return;

			OSVersionPlatform = System.Environment.OSVersion.Platform.ToString();

			string temp;

			var appSettings = ConfigurationManager.AppSettings;
			if (OSVersionPlatform.Contains("Unix"))
			{
				temp = appSettings[ADB_SPECIFIED];
				if (!string.IsNullOrEmpty(temp) && File.Exists(temp))
					adbCommand = temp;
				else
				{
					temp = RunProcess(UNIX_WHICH, ADB_COMMAND).Trim();
					adbCommand = (string.IsNullOrEmpty(temp) || !File.Exists(temp)) ? ADB_COMMAND : temp;
				}
				temp = appSettings[XCRUN_SPECIFIED];
				if (!string.IsNullOrEmpty(temp) && File.Exists(temp))
					xcrunCommand = temp;
				else
				{
					temp = RunProcess(UNIX_WHICH, XCRUN_COMMAND).Trim();
					xcrunCommand = (string.IsNullOrEmpty(temp) || !File.Exists(temp)) ? XCRUN_COMMAND : temp;
				}
			}
			else if (OSVersionPlatform.Contains("Win"))
			{
				temp = appSettings[ADB_SPECIFIED];
				if (!string.IsNullOrEmpty(temp) && File.Exists(temp))
					adbCommand = temp;
				else
				{
					temp = RunProcess(WINDOWS_WHERE, ADB_COMMAND).Trim();
					adbCommand = string.IsNullOrEmpty(temp) ? null : temp;
				}
			}

			adbFilters = appSettings[ANDROID_KEYS] ?? DEFAULT_ANDROID_FILTERS;
			temp = appSettings[ANDROID_APK_SPECIFIED];
			if (!string.IsNullOrEmpty(temp) && File.Exists(temp))
				androidApkFilePath = temp;
			temp = appSettings[IOS_APP_SPECIFIED];
			if (!string.IsNullOrEmpty(temp) && File.Exists(temp))
				iosAppFilePath = temp;
			
			temp = appSettings[SCREEN_LOAD_TIMEOUT];
			int seconds;
			if (!string.IsNullOrEmpty(temp) && int.TryParse(temp, out seconds) && seconds>0 && seconds<MAX_TIMEOUT_INSECONDS)
				ScreenLoadTimeoutInSeconds = TimeSpan.FromSeconds(seconds);

			androids = GetAndroidPhones();
			iPhones = GetiPhones();
		}

		private static Dictionary<string, string[]> GetAndroidPhones()
		{
			if (string.IsNullOrEmpty(adbCommand))
				return null;

			Dictionary<string, string[]> phones = new Dictionary<string, string[]>();

			string[] keys = adbFilters.Split(new char[] { '|' });
			string[] properties;
			string temp = RunProcess(adbCommand, "devices");
			string[] lines = temp.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 1; i < lines.Length; i++)
			{
				string serialno = lines[i].Split(new char[] { ' ', '\t' })[0];
				temp = RunProcess(adbCommand, string.Format("-s {0} shell getprop", serialno));
				properties = temp.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				var list = properties.Where(p => p.ContainsAny(keys)).Select(p =>
				{
					int index = p.LastIndexOf('[');
					string r = p.Substring(index + 1, p.LastIndexOf(']') - index - 1);
					return p.ContainsIgnoreCase("version.sdk") ? "API" + r : r;
				}).ToList();
				if (!list.Contains(serialno))
					list.Add(serialno);
				phones.Add(serialno, list.ToArray());
			}
			return phones;
		}

		private static Dictionary<string, string[]> GetiPhones()
		{
			if (string.IsNullOrEmpty(xcrunCommand))
				return null;

			Dictionary<string, string[]> phones = new Dictionary<string, string[]>();
			string[] properties;

			string temp = RunProcess(xcrunCommand, "instruments -s devices");
			string[] lines = temp.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
						  .Where(s => !s.Contains('+') && s.ContainsAny("iPad", "iPhone")).ToArray();
			foreach (var line in lines)
			{
				int index = line.IndexOf('[');
				string serialno = line.Substring(index + 1, line.IndexOf(']') - index - 1);
				index = line.IndexOf('(');
				if (line.Contains("Simulator"))
				{
					properties = new string[3];
					properties[2] = "Simulator";
				}
				else
					properties = new string[2];
				properties[0] = line.Substring(0, index - 1);
				properties[1] = "iOS" + line.Substring(index + 1, line.IndexOf(')') - index - 1);
				phones.Add(serialno, properties);
			}
			return phones;
		}

		public static string RunProcess(string command, string arguments = null)
		{
			if (string.IsNullOrEmpty(command))
				return null;

			ProcessStartInfo startInfo = string.IsNullOrEmpty(arguments) ? new ProcessStartInfo(command)
											   : new ProcessStartInfo(command, arguments);
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			startInfo.CreateNoWindow = true;

			Process process = Process.Start(startInfo);
			StringBuilder sb = new StringBuilder();
			while (!process.StandardOutput.EndOfStream)
			{
				sb.AppendLine(process.StandardOutput.ReadLine());
			}
			return sb.ToString();
		}

		public static bool ContainsIgnoreCase(this string content, object key)
		{
			if (key is DateTime)
			{
				DateTime date = (DateTime)key;
				return content.Contains(date.ToString("d-MMM-yyyy")) || content.Contains(date.ToShortDateString())
					|| content.Contains(date.ToShortDateString());
			}
			else
			{
				return content.IndexOf(key.ToString().TrimStart('0'), StringComparison.InvariantCultureIgnoreCase) >= 0;
			}
		}

		public static int IndexOfMissing(this string content, params object[] keys)
		{
			for (int i = 0; i < keys.Length; i++)
			{
				object k = keys[i];
				if (k is decimal)
				{
					decimal amount = (decimal)k;
					if (!content.Contains(amount.ToString("$#,##0.00")) && !content.Contains(amount.ToString("#,##0.00")) && !content.Contains(amount.ToString()))
						return i;
				}
				else if (k is DateTime)
				{
					DateTime date = (DateTime)k;
					if (!content.Contains(date.ToString("d/MM/yyyy")) && !content.Contains(date.ToString("d-MMM-yyyy")))
						return i;
				}
				else
				{
					if (content.IndexOf(k.ToString().TrimStart('0'), StringComparison.InvariantCultureIgnoreCase) < 0)
						return i;
				}
			}
			return -1;
		}

		public static bool ContainsAll(this string content, params object[] keys)
		{
			foreach (object k in keys)
			{
				if (k is decimal)
				{
					decimal amount = (decimal)k;
					if (!content.Contains(amount.ToString("$#,##0.00")) && !content.Contains(amount.ToString("#,##0.00")) && !content.Contains(amount.ToString()))
					{
						return false;
					}
				}
				else if (k is DateTime)
				{
					DateTime date = (DateTime)k;
					if (!content.Contains(date.ToString("d/MM/yyyy"))
						&& !content.Contains(date.ToString("d-MMM-yyyy")))
					{
						return false;
					}
				}
				else
				{
					if (content.IndexOf(k.ToString().TrimStart('0'), StringComparison.InvariantCultureIgnoreCase) < 0)
					{
						return false;
					}
				}
			}
			return true;
		}

		public static bool ContainsAny(this string content, params object[] keys)
		{
			foreach (object k in keys)
			{
				if (k is decimal)
				{
					decimal amount = (decimal)k;
					if (content.Contains(amount.ToString("$#,##0.00")) || content.Contains(amount.ToString("#,##0.00")) || content.Contains(amount.ToString()))
					{
						return true;
					}
				}
				else if (k is DateTime)
				{
					DateTime date = (DateTime)k;
					if (content.Contains(date.ToString("d/MM/yyyy")) || content.Contains(date.ToString("d-MMM-yyyy")))
					{
						return true;
					}
				}
				else
				{
					if (content.IndexOf(k.ToString().TrimStart('0'), StringComparison.InvariantCultureIgnoreCase) >= 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static int getCredit(string[] values, string[] keys)
		{
			int credit = 0;
			foreach (string k in keys)
			{
				if (string.IsNullOrEmpty(k)) continue;
				if (values.Contains(k))
					credit += 3;
				else if (values.Any(v => v.ToUpper() == k.ToUpper()))
					credit += 2;
				else if (values.Any(v => v.ContainsIgnoreCase(k)))
					credit += 1;
			}
			return credit;
		}

		private static string getIdentifier(Dictionary<string, string[]> devices, string[] keys)
		{
			if (devices == null || devices.Count == 0) return null;

			var maxKvp = devices.Aggregate((kvp1, kvp2) => getCredit(kvp1.Value, keys) >= getCredit(kvp2.Value, keys) ? kvp1 : kvp2);

			if (getCredit(maxKvp.Value, keys) == 0) return null;

			return maxKvp.Key;
		}

		public static string AndroidIdentifier(string deviceDesc)
		{
			if (string.IsNullOrEmpty(deviceDesc)) return null;
			string[] keys = deviceDesc.Split(new char[] { ',', '|', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries)
									  .Select(s => s.Trim()).ToArray();
			int api = -1;
			for (int i = 0; i < keys.Length; i++)
			{
				if (int.TryParse(keys[i], out api) && api > 0 && api < 100)
					keys[i] = "API" + keys[i];
			}

			return getIdentifier(androids, keys);
		}

		public static string iOSIdentifier(string deviceDesc)
		{
			if (string.IsNullOrEmpty(deviceDesc)) return null;
			string[] keys = deviceDesc.Split(new char[] { ',', '|', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries)
									  .Select(s => s.Trim()).ToArray();
			double ios = -1.0;
			for (int i = 0; i < keys.Length; i++)
			{
				if (double.TryParse(keys[i], out ios) && ios > 0 && ios < 20)
					keys[i] = "iOS" + keys[i];
			}

			var dict = iPhones.Where(kvp => deviceDesc.Contains("Simulator") ? kvp.Value.Contains("Simulator") : true)
				.ToDictionary(e => e.Key, e => e.Value);

			return getIdentifier(dict, keys);
		}

		public static IApp StartAndroidApp(string device, AppDataMode mode = AppDataMode.Auto)
		{
			AndroidAppConfigurator android = ConfigureApp.Android
				.Debug()
				.EnableLocalScreenshots()
				.PreferIdeSettings();

			if (TestEnvironment.Platform == TestPlatform.TestCloudAndroid)
				return android.StartApp(mode);
			else if (TestEnvironment.Platform == TestPlatform.TestCloudiOS)
				throw new InvalidOperationException("Android app cannot run in TestPlatform.TestCloudiOS");

			//The compiled APK can be specified by setting 'Android_APK_Path' appSettings of the App.config
			string apk = androidApkFilePath
				?? "../../../ZeroClick/ZeroClick.Droid/bin/Release/au.com.dominos.zeroclick.apk";

			if (File.Exists(apk))
				android = android.ApkFile(apk);

			if (device != null)
			{
				string deviceIdentifier = AndroidIdentifier(device);
				if (string.IsNullOrEmpty(deviceIdentifier))
					throw new InvalidCastException("Cannot cast '" + device + "' to an active Android device or emulator!");

				android = android.DeviceSerial(deviceIdentifier);
			}

			IApp a = android.StartApp(mode);
			//a.Repl();		//To view the layout only during debugging
			return a;
		}

		public static IApp StartiOSApp(string device, AppDataMode mode = AppDataMode.Auto)
		{
			iOSAppConfigurator iOS = ConfigureApp.iOS
                 .Debug()
                 .PreferIdeSettings()
                 .EnableLocalScreenshots();
			
			if (TestEnvironment.Platform == TestPlatform.TestCloudiOS)
				return iOS.StartApp(mode);
			else if (TestEnvironment.Platform == TestPlatform.TestCloudAndroid)
				throw new InvalidOperationException("iOS app cannot run in TestPlatform.TestCloudAndroid");

			if (device != null)
			{
				string deviceIdentifier = iOSIdentifier(device);
				if (string.IsNullOrEmpty(deviceIdentifier))
					throw new InvalidCastException("Cannot cast '" + device + "' to an active iOS device or Simulator!");

				iOS = iOS.DeviceIdentifier(deviceIdentifier);

				if (device.Contains("Simulator"))
				{
					if (mode == AppDataMode.Clear)
					{
						RunProcess(xcrunCommand, string.Format("simctl shutdown {0}", deviceIdentifier));
						RunProcess(xcrunCommand, string.Format("simctl erase {0}", deviceIdentifier));
					}
					//Xamarin Test Cloud Agent should only be included in Debug builds to enable UITest
					string appPath = iosAppFilePath ?? DEFUALT_IOS_APP_PATH;

					if (File.Exists(appPath) ||Directory.Exists(appPath))
						iOS = iOS.AppBundle(appPath);
				}
				else
				{
					iOS = iOS.InstalledApp("com.sample.Phoneword");
				}
			}

			//Note: iOS.StartApp() would close the initial System Notification with default Xamarin alert handling, 
			// but if it returns before the notification is displayed, then UITest would not access the app to run tests.
			IApp a = iOS.StartApp(mode);

			if (TestEnvironment.Platform == TestPlatform.TestCloudiOS)
			{
				//Returning iOS.ConnectToApp(0 after sleeping 1 second would be helpful to dismiss the System Notofication 
				System.Threading.Thread.Sleep(1000);
				a = iOS.ConnectToApp();
			}
			//a.Repl();		//To view the layout only during debugging
			return a;
		}

		public static IApp StartApp(Platform platform, string device, AppDataMode mode = AppDataMode.Auto)
		{
			if (platform == Platform.Android)
				return StartAndroidApp(device, mode);
			else if (platform == Platform.iOS)
				return StartiOSApp(device, mode);
			else
				throw new NotImplementedException(platform + " is not supported yet!");
		}
	}
}
