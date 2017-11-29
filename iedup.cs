/*
 * Created by SharpDevelop.
 * User: Owner
 * Date: 11/29/2017
 * Time: 1:25 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
//SOURCE for IEdu.cs is at https://github.com/expertmm/iedusm
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Timers; //ElapsedEventArgs etc
using System.Management; //ManagementObjectSearcher etc
using System.IO;
//using System.Threading.Tasks; //async etc
//using Microsoft.Bcl.Async;  // doesn't exist 
//using System.ComponentModel.EventBasedAsync;  // doesn't exist
using NativeWifi;
using System.Net.NetworkInformation;
using System.Configuration.Install;  // ManagedInstallerClass etc
using System.Web; //HttpContext.Request etc

namespace iedu
{
	public class iedup : ServiceBase
	{
		private static Dictionary<string,string> settings = null;
		private const bool debug_enable = true;
		public const string MyServiceName = "iedup";
		private static System.Timers.Timer ss_timer = null;
		private static bool timers_enable = false;
		public static char[] badchars = new char[]{'\0'};
		
		public iedup()
		{
			InitializeComponent();
		}
		
		private void InitializeComponent()
		{
			this.ServiceName = MyServiceName;
		}
		
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (ss_timer!=null) {
				ss_timer.Stop();
				ss_timer = null;
			}
			base.Dispose(disposing);
		}

		//private async void capture_data() {
		private void capture_data() {
			string temp = "C:\\tmp"; // Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
			if (!Directory.Exists(temp)) Directory.CreateDirectory(temp);
			string image_name = "last.jpg";
			string text_name = "users.txt";
			string image_path = Path.Combine(temp, image_name);
			string text_path = Path.Combine(temp, text_name);
			string loggedOnUserName = null;
			Dictionary<string, string> body = new Dictionary<string, string>();
			body.Add("MachineName", Environment.MachineName);
			//string IP = Request.UserHostName;
			string HostName = IEdu.GetHostName();  // DetermineHostName(IP);
			body.Add("HostName", HostName);
			StreamWriter outs = null;
			if (debug_enable) outs = new StreamWriter(text_path);
			
			//list logged on users as per Sameet from <https://stackoverflow.com/questions/1244000/find-out-the-current-users-username-when-multiple-users-are-logged-on>
			ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT UserName FROM Win32_ComputerSystem");
			
			foreach (ManagementObject queryObj in searcher.Get())
            {
                loggedOnUserName = queryObj["UserName"].ToString();
                loggedOnUserName = loggedOnUserName.Substring(loggedOnUserName.LastIndexOf('\\') + 1);
                //body.Add("UserName", loggedOnUserName);
            }
			if (!body.ContainsKey("UserName")) {
				int user_count = 0;
				foreach (ManagementObject queryObj in searcher.Get())
	            {
	                loggedOnUserName = queryObj["UserName"].ToString();
	                loggedOnUserName = loggedOnUserName.Substring(loggedOnUserName.LastIndexOf('\\') + 1);
	                body.Add("UserNames_i_"+user_count, loggedOnUserName);
	            }
			}
			
			WlanClient client = new WlanClient();
        	// Wlan = new WlanClient();
        	try
	        {
        		int collected_count = 0;
	            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
	            {
	                string local_mac = "";
	                //System.Net.NetworkInformation.PhysicalAddress
	                PhysicalAddress pa = wlanIface.NetworkInterface.GetPhysicalAddress();
	                local_mac = wlanIface.NetworkInterface.GetPhysicalAddress().ToString(); //wlanIface.InterfaceGuid.ToString();
	                if (!body.ContainsKey("MAC")) body.Add("MAC", local_mac);
	                
	                Wlan.WlanBssEntry[] wlanBssEntries = wlanIface.GetNetworkBssList();
	                foreach (Wlan.WlanBssEntry network in wlanBssEntries)
	                {
	                    string object_prefix = "remotewifi_i_"+collected_count.ToString()+"_";
	                    int rss = network.rssi;
	                    //     MessageBox.Show(rss.ToString());
	                    byte[] remote_mac_bytes = network.dot11Bssid;
	
	                    string remote_mac_s = "";
	
	                    for (int i = 0; i < remote_mac_bytes.Length; i++) {
	                        remote_mac_s += remote_mac_bytes[i].ToString("x2").PadLeft(2, '0').ToUpper();
	                    }
	                    
						body.Add(object_prefix+"MAC", remote_mac_s);
	                    body.Add(object_prefix+"SSID", System.Text.ASCIIEncoding.ASCII.GetString(network.dot11Ssid.SSID).ToString().Trim(badchars));
	                    body.Add(object_prefix+"signal_percent", network.linkQuality.ToString());
	                    body.Add(object_prefix+"type", network.dot11BssType.ToString());
	                    body.Add(object_prefix+"RSSID", rss.ToString());
	                    collected_count++;
	                }//end for remote nodes
	                if (collected_count>0) break; //no need to check other Wireless adapters
	            }//end for wireless adapters
	            if (outs!=null) {
					foreach(KeyValuePair<string, string> entry in body)
					{
						outs.WriteLine(entry.Key + ": " + entry.Value);
					}
	            }
	        }
	        catch (Exception ex)
	        {
	            if (outs!=null) outs.WriteLine(ex.Message);
	        }
	        if (settings.ContainsKey("ping_url") && settings["ping_url"].Length>0) {
	        	string response = IEdu.html_post(settings["ping_url"], body);
	        	if (outs!=null) {
	        		outs.WriteLine("posted to "+settings["ping_url"]);
	        		outs.WriteLine("no response needed: '"+response+"'");
	        	}
	        }
	        else if (outs!=null) outs.WriteLine("missing ping_url in capture_data");
	        try{
				if (outs!=null) outs.Close();
				if (debug_enable) Thread.Sleep(500);  // wait for file
	        }
	        catch {}
		}//end capture_data
		
		private void install_software() {
			string s_name_noext = "iedusm";
			string s_path = IEdu.get_service_path(s_name_noext);
			
			//see <https://stackoverflow.com/questions/2072288/installing-windows-service-programmatically>:
            ManagedInstallerClass.InstallHelper(new string[] { s_path });
        	//starting it as per codemonkey from <https://stackoverflow.com/questions/1036713/automatically-start-a-windows-service-on-install>:
        	//results in access denied (same if done manually, unless "Log on as" is changed from LocalService to Local System
			//serviceInstaller
        	//using (ServiceController sc = new ServiceController(serviceInstaller.ServiceName))
        	//using (ServiceController sc = new ServiceController("iedusm"))
		    //{
		    //     sc.Start();
		    //}
		}
		
		private void uninstall_software() {
			string s_name_noext = "iedusm";
			string s_path = IEdu.get_service_path(s_name_noext);
			
			ManagedInstallerClass.InstallHelper(new string[] { "/u", s_path });
		}
		
		//private async Task ss_timer_Elapsed//would normally be a Task but ok not since is event --see https://stackoverflow.com/questions/39260486/is-it-okay-to-attach-async-event-handler-to-system-timers-timer
		//private async void ss_timer_ElapsedAsync(object sender, ElapsedEventArgs e) {
		//	ss_timer.Stop();
		//	await Task.Run(() => capture_data());
		//	if (timers_enable&&(ss_timer!=null)) ss_timer.Start();
		//}
		private void ss_timer_ElapsedSync(object sender, ElapsedEventArgs e) {
			ss_timer.Stop();
			capture_data();
			if (timers_enable&&(ss_timer!=null)) ss_timer.Start();
		}
		
		/// <summary>
		/// Start this service.
		/// </summary>
		protected override void OnStart(string[] args)
		{
			settings = new Dictionary<string, string>();
			string settings_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "iedup");
			if (File.Exists(settings_path)) {
				StreamReader ins = new StreamReader(settings_path);
				string line;
				//defaults:
				while ( (line=ins.ReadLine()) != null ) {
					string line_trim = line.Trim();
					if (line_trim.Length>0) {
						if (!line_trim.StartsWith("#")) {
							int ao_i = line_trim.IndexOf(":");
							if (ao_i>-1) {
								string name = line.Substring(0,ao_i).Trim();
								string val = line.Substring(ao_i+1).Trim();
								if (val.Length>1 && val.StartsWith("\"") && val.EndsWith("\"")) {
									val = val.Substring(1,val.Length-2);
								}
								else if (val.Length>1 && val.StartsWith("'") && val.EndsWith("'")) {
									val = val.Substring(1,val.Length-2);
								}
								if (name!="") {
									if (settings.ContainsKey(name)) settings[name] = val;
									else settings.Add(name, val);
								}
							}
						}
					}
				}
				ins.Close();
			}
			timers_enable = true;
			ss_timer = new System.Timers.Timer(debug_enable?5000:30000);  // 10000ms is 10s
			ss_timer.AutoReset = true;  // loop
			//ss_timer.Elapsed += ss_timer_ElapsedAsync;  // ss_timer.Elapsed += async (sender, arguments) => await ss_timer_Elapsed(sender, arguments);
			ss_timer.Elapsed += ss_timer_ElapsedSync;
			
			ss_timer.Enabled = true;  // default is false
			ss_timer.Start();
		}
		
		/// <summary>
		/// Stop this service.
		/// </summary>
		protected override void OnStop()
		{
			timers_enable = false;
			ss_timer.Stop();
			ss_timer = null;
		}
	}
}
