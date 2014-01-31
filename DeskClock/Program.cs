using System;
using TheResistorNetwork.Drivers.VfdDriver;
using System.Text;
using System.Threading;
using System.Timers;
using System.Configuration;
using ActiveUp.Net.Mail;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;

namespace DeskClock
{
	class MainClass
	{
		static VdfDisplay display;

		static int timeCounter = 0;

		static int frameCount = 0;
		static byte[,][] animation = Animations.Globe;
		static byte[,][] nextAnimation = null;

		static Imap4Client client;
		static string currentSubject = string.Empty;
		static int subjectCharPos = 0;
		static int subjectCounter = 0;
		static List<string> emailSubjects = new List<string> ();
		static List<string> displayedSubjects = new List<string> ();
		static System.Timers.Timer emailUpdateTimer = new System.Timers.Timer();

		static System.Timers.Timer weatherUpdateTimer =
			new System.Timers.Timer();

		public static void Main (string[] args)
		{
			Console.WriteLine ("VFD Desk Clock");

			display = new VdfDisplay ("/dev/ttyUSB0");

			display.Reset ();
			display.Brightness = VfdBrightness.Percent50;
			display.CursorMode = VfdCursorMode.Off;
			display.CustomCharacterMode = VfdCustomCharacterMode.Enabled;
			display.Clear ();

			clearCustomCharacters (241, 255);

			var timeTimer = new System.Timers.Timer ();
			timeTimer.Interval = 250;
			timeTimer.Elapsed += (object sender, ElapsedEventArgs e) => {
				updateClock();
			};

			var animationTimer = new System.Timers.Timer ();
			animationTimer.Interval = 120;
			animationTimer.Elapsed += (object sender, ElapsedEventArgs e) => {
				updateAnimation();
			};

			var emailTimer = new System.Timers.Timer ();
			emailTimer.Interval = 200;
			emailTimer.Elapsed += (object sender, ElapsedEventArgs e) => {
				updateEmails();
			};

			emailUpdateTimer.Interval = 10000;
			emailUpdateTimer.Elapsed += (object sender, ElapsedEventArgs e) => {
				updateUnreadEmails();
			};

			weatherUpdateTimer = new System.Timers.Timer ();
			weatherUpdateTimer.Interval = 1200000;
			weatherUpdateTimer.Elapsed +=
				(object sender, ElapsedEventArgs e) => {
				updateWeatherConditions ();
			};

			updateWeatherConditions ();
			updateUnreadEmails ();

			updateClock ();
			updateAnimation ();
			updateEmails();

			timeTimer.Start ();
			animationTimer.Start ();
			emailTimer.Start ();

			Thread.Sleep (Timeout.Infinite);
		}

		private static void updateClock()
		{
			lock(display) {
				var time = string.Empty;

				if(timeCounter++ < 70)
				{
					time = DateTime.Now.ToString("HH:mm:ss");
				}
				else
				{
					time = DateTime.Now.ToString("dd/MM/yy");
				}

				timeCounter %= 100;

				display.SetCursorPosition (16, 0);
				display.Write(Encoding.ASCII.GetBytes(time));
			}
		}

		private static void updateAnimation()
		{
			lock(display) {
				for (int i = 241; i <= 255; i++) {
					display.DefineCustomCharacters ((byte)i, (byte)i,
						animation [frameCount, i - 241]);
				}
			}

			frameCount++;
			frameCount %= animation.GetLength(0);

			if (frameCount == 0
				&& nextAnimation != null) {
				animation = nextAnimation;
				nextAnimation = null;
			}
		}

		private static void updateUnreadEmails()
		{
			emailUpdateTimer.Stop ();

			try {
				nextAnimation = Animations.Loader;

				if(client == null || !client.IsConnected)
				{
					client = new Imap4Client();
					client.ConnectSsl("imap.gmail.com", 993);

					var username =
						ConfigurationManager.AppSettings ["gmailUsername"];
					var password =
						ConfigurationManager.AppSettings ["gmailPassword"];

					client.Login(username, password);
				}

				var inbox = client.SelectMailbox("inbox");
				var ids = inbox.Search("UNSEEN");

				var subjects = new List<string>();

				foreach (var id in ids) {
					var message = inbox.Fetch.MessageObject (id);
						subjects.Add (message.Subject);

					var flags = new FlagCollection();
					flags.Add("Seen");
					inbox.RemoveFlags(id, flags);
				}

				if(subjects.Count > 0) {
					nextAnimation = Animations.Email;
				}

				lock(emailSubjects) {
					emailSubjects = subjects;
				}
			} catch(Exception e) {
				Console.WriteLine (e.Message);
			}

			emailUpdateTimer.Start ();
		}

		private static void updateWeatherConditions()
		{
			weatherUpdateTimer.Stop ();

			try {
				var appId = ConfigurationManager.AppSettings ["weatherAppId"];
				var city = ConfigurationManager.AppSettings ["weatherCity"];

				var url = string.Format ("http://api.openweathermap.org/" +
					"data/2.5/weather?q={0}&APPID={1}", city, appId);

				var request = (HttpWebRequest)WebRequest.Create(url);
				request.ContentType = "text/json";
				request.Method = "GET";

				var response = (HttpWebResponse)request.GetResponse ();

				var reader = new StreamReader (response.GetResponseStream());
				var weatherJson = reader.ReadToEnd ();
				reader.Dispose();

				var serializer = new JavaScriptSerializer ();
				var weather = serializer.Deserialize<WeatherData> (weatherJson);

				var highTemp = string.Format ("High {0}",
					(weather.main.temp_max - 273).ToString("F0"));
				var lowTemp = string.Format ("Low {0}",
					(weather.main.temp_min - 273).ToString("F0"));
				var conditions = new string (' ',
					24 - lowTemp.Length - weather.weather[0].description.Length) +
				                 weather.weather[0].description;

				conditions = conditions.ToLower ();

				lock (display) {
					display.SetCursorPosition (0, 0);
					display.Write (Encoding.ASCII.GetBytes (highTemp));
					display.SetCursorPosition (0, 1);
					display.Write (Encoding.ASCII.GetBytes (lowTemp));
					display.Write (Encoding.ASCII.GetBytes (conditions));
				}
			}
			catch(Exception e)
			{
				Console.WriteLine(e);
			}

			weatherUpdateTimer.Start ();
		}

		private static void updateEmails()
		{
			var unreadEmail = string.Format ("{0} Unread Email{1}",
				emailSubjects.Count,
				emailSubjects.Count == 1 ? string.Empty : "s");

			unreadEmail += new string (' ', 17 - unreadEmail.Length);

			lock (emailSubjects) {
				if (currentSubject == string.Empty
					&& emailSubjects.Count > 0) {

					foreach (var subject in emailSubjects) {
						if (!displayedSubjects.Contains (subject)) {
							currentSubject = subject;
							break;
						}
					}

					if (currentSubject == string.Empty) {
						displayedSubjects.Clear ();
						currentSubject = emailSubjects [0];
					}
				}
			}

			string displaySubject;

			if (currentSubject.Length > 17) {
				if ((currentSubject.Length - subjectCharPos) > 17) {
					displaySubject =
						currentSubject.Substring (subjectCharPos++, 17);
				} else {
					displaySubject =
						currentSubject.Substring (subjectCharPos, 17);
				}
			} else {
				displaySubject = currentSubject +
                	new string (' ', 17 - currentSubject.Length);
			}

			if (emailSubjects.Count == 0 && animation != Animations.Globe) {
				nextAnimation = Animations.Globe;
			}

			if (subjectCounter <= 1) {
				lock (display) {
					display.SetCursorPosition (6, 3);
					display.Write (Encoding.ASCII.GetBytes (unreadEmail));

					display.SetCursorPosition (6, 4);
					display.Write (Encoding.ASCII.GetBytes (displaySubject));
				}
			}

			if ((currentSubject.Length - subjectCharPos) <= 17) {
				if (subjectCounter++ > 10) {
					displayedSubjects.Add (currentSubject);
					subjectCharPos = 0;
					currentSubject = string.Empty;
					subjectCounter = 0;
				}
			}
		}

		private static void clearCustomCharacters(byte start, byte end)
		{
			var emptyChar = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 };

			for (int i = start; i <= end; i++) {
				display.DefineCustomCharacters ((byte)i, (byte)i, emptyChar);
				Thread.Sleep (1);
			}

			display.SetCursorPosition (0, 3);
			display.Write (new byte[] { 241, 242, 243, 244, 245 });
			display.SetCursorPosition (0, 4);
			display.Write (new byte[] { 246, 247, 248, 249, 250 });
			display.SetCursorPosition (0, 5);
			display.Write (new byte[] { 251, 252, 253, 254, 255 });
		}
	}
}
