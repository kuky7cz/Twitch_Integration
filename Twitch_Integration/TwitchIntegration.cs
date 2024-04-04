using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;



namespace Twitch_Integration {

	/// <summary>
	/// Nezávisla třída pužitelná i do jiných projektu
	/// </summary>
	public class TwitchIntegration {

		public string chanel = "KUKY7cz";
		public string clientId = "";
		public string password = ""; // !!PRIVATE!! pssw or OAuthToken
									 // Asi netřeba
		public string accessToken = ""; // !!PRIVATE!! OAuthToken
		public string username = "kuky7cz"; // NICK

		public List<HlasovaciEvent> hlasovaciEvents = new List<HlasovaciEvent>();
		private List<string> hlasovali = new List<string>();
		private bool isVoting = false;
		private DateTime votingStart = new DateTime();
		public TimeSpan delkaHlasovasni = new TimeSpan(0, 1, 0);
		public TimeSpan rozmeziHlasovasni = new TimeSpan(0, 10, 0);
		//public TimeSpan rozmeziHlasovasniMin = new TimeSpan(0, 10, 0);
		//public TimeSpan rozmeziHlasovasniMax = new TimeSpan(0, 30, 0);

		public bool isConnected { get; private set; } = false;
		private TcpClient tcpClient;
		private StreamReader sr;
		private StreamWriter sw;
		private Thread receiveThread;


		public TwitchIntegration() {

		}


		public void Connect() {
			// v1  using System.IO, System.Net.Sockets;

			//tcpClient = new TcpClient("irc.twitch.tv", 6667); // ???
			tcpClient = new TcpClient("irc.chat.twitch.tv", 6667); // Tohle by měl být chat
																   // https://id.twitch.tv/oauth2/authorize // AUtorizace. Mělo by se zde sehnat token
			sr = new StreamReader(tcpClient.GetStream());
			sw = new StreamWriter(tcpClient.GetStream());
			sw.WriteLine("PASS " + password);
			sw.WriteLine("NICK " + username.ToLower());
			sw.WriteLine("JOIN #" + chanel.ToLower());
			sw.Flush();

			isConnected = true;

			//receiveThread = new Thread(Update);
			//receiveThread.Start();

			//StartCoroutine("ReadStream");

			// v2 - using System.Net.WebSockets, System.Threading;
			/*
			ClientWebSocket clientWebSocket = new ClientWebSocket();
			CancellationToken cancellationToken = new CancellationToken();
			*/

		}

		public void Update(float dt) { // V1
			/*
			if(tcpClient != null && tcpClient.Connected && tcpClient.Available > 0 && !sr.EndOfStream) {
				string message = sr.ReadLine();
				Debug.Log("reader = " + message);
			}
			*/

			//while(true) {
				//if(!isConnected) { break; }
				//if(tcpClient == null || !tcpClient.Connected || tcpClient.Available <= 0 || sr.EndOfStream ) { break; }
				if(tcpClient != null && tcpClient.Connected && tcpClient.Available > 0 && !sr.EndOfStream && isConnected) {
					string message = sr.ReadLine();
					//Debug.Log(message);
					if(message.Split('!', 3).Length >= 3) { // OPTIMALIZACE RYCHLOSTI -  Přeskoč vše co není příkaz.

						TwitchChatMessage msg = new TwitchChatMessage();
						msg.Parse(message);

						foreach(HlasovaciEvent e1 in hlasovaciEvents) {
							if(msg.message.Contains(e1.name)) {
								HlasovaniStart();
								Vote(msg.nick, e1.name);
							}
						}
						Debug.Log(msg.ToJSON());
					}
				//}
				//Thread.Sleep(200);
			}
		}


		private IEnumerable ReadStream() { // V2

			return null;
		}


		public void SendTestMessage() {
			TwitchChatMessage msg = new TwitchChatMessage();
			msg.message = "< LUL >";
			msg.chanel = "kuky7cz";
			sw.WriteLine(msg.ToString());
			sw.Flush();
		}


		public void Disconnect() {
			sw.Close();
			sr.Close();
			tcpClient.Close();
			isConnected = false;
		}


		public class TwitchChatMessage {
			private static string tt1 = "PRIVMSG";

			public string nick = "";
			public string chanel = "";
			public string message = "";

			public int Parse(string message) {
				if(message.Contains(tt1)) { // PRIVMSG

					string[] parts = message.Split(':', 3); // EMPTY, DATA, MESSAGE

					//parts[0] // EMPTY

					string[] parts1 = parts[1].Split(' ', 4); // ADRESS, PRIVMSG, #CHANEL, EMPTY
					nick = parts1[0].Split('!', 4)[0];
					//part1s[1]
					chanel = parts1[2].Substring(1);
					//part1s[3] // EMPTY

					this.message = parts[2];

				} else {
					return 1;
				}
				return 0;
			}

			public override string ToString() {
				// ":kuky7cz!kuky7cz@kuky7cz.tmi.twitch.tv PRIVMSG #chanel :message;
				return ":kuky7cz!kuky7cz@kuky7cz.tmi.twitch.tv " + tt1 + " #" + chanel + " :" + message;
			}

			public string ToJSON() {
				// ":kuky7cz!kuky7cz@kuky7cz.tmi.twitch.tv PRIVMSG #chanel :message;
				//return "{ \"" + name + "\", \"" + chanel + "\", \"" + message + "\" }";
				return JsonUtility.ToJson(this);
			}

		}


		public void HlasovaniStart() {
			Debug.Log("HlasovaniStart()");
			if(IsPossibleVotingStart()) {
				HlasovaniReset();
				votingStart = DateTime.Now;
				isVoting = true;

				System.Timers.Timer timer = new System.Timers.Timer(delkaHlasovasni.TotalMilliseconds); // 60000 ms = 1 minuta
				timer.Elapsed += delegate (object sender, ElapsedEventArgs e) { HlasovaniStop(); };
				timer.AutoReset = false; // Zabraňuje opakovanému spouštění po každém intervalu
				timer.Start();

				/*
				TimerCallback callback = HlasovaniStopAsinc;
				System.Threading.Timer timer = new System.Threading.Timer(callback, null, 60000, Timeout.Infinite); // 60000 ms = 1 minuta
				*/
			}
		}


		public void HlasovaniStop() {
			Debug.Log("HlasovaniStop()");
			HlasovaniVyhodnoceni();
			HlasovaniReset();
			isVoting = false;
		}


		public bool IsPossibleVotingStart() {
			if(isVoting) { return false; }
			return (DateTime.Now - votingStart) > rozmeziHlasovasni;
		}


		private void HlasovaniVyhodnoceni() {
			Debug.Log("HlasovaniVyhodnoceni()");
			int max = 0;
			HlasovaciEvent viteznyEvent = null;

			foreach(var e1 in hlasovaciEvents) {
				if(max < e1.hlasy) {
					max = e1.hlasy;
					viteznyEvent = e1;
				}
			}

			viteznyEvent.events();
			Debug.Log("HlasovaniVyhodnoceni() -> " + viteznyEvent.name + "|" + viteznyEvent.hlasy);
		}


		public void HlasovaniReset() {
			Debug.Log("HlasovaniReset()");
			foreach(var e1 in hlasovaciEvents) {
				e1.Reset();
			}
			hlasovali = new List<string>();
		}


		public void Vote(string aVoterName, string aName) {
			Debug.Log("Vote()");
			if(isVoting) {
				foreach(string e1 in hlasovali) {
					if(e1 == aVoterName) {
						Debug.Log("Vote() -> Chce znovu hlasovat. " + aVoterName + "|" + aName);
						return;
					}
				}
				foreach(HlasovaciEvent e1 in hlasovaciEvents) {
					if(e1.name == aName) {
						e1.hlasy++;
						hlasovali.Add(aVoterName);
					}
				}
			}
		}


		public void ForceVotingStart() {
			votingStart = new DateTime();
			HlasovaniStart();
		}



		public class HlasovaciEvent {
			public string name;
			public Action events;
			public int hlasy = 0;
			//public List<string> hlasovali = new List<string>(); // Action, ..., Func<>

			public HlasovaciEvent() {

			}

			public HlasovaciEvent(string aName, Action aEvent) {
				name = aName;
				events = aEvent;
			}

			public void Reset() {
				hlasy = 0;
			}

		}


	}
}

/*
EN: Exclusive ownership of this file cannot be claimed by persons other than the authors of the file.
CZ: Na tento soubor nelze uplatni výhradní vlastnicví, jiným osobám než autorm souboru.

LICENSE - MIT

Copyright (c) 2023 Hotárek Lukáš

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/