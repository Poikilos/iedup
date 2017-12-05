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
		public static string err = null;
		public static string temp_d_path = null;
		public static DateTime last_process_list_dt = DateTime.MinValue;
		private static Dictionary<string,string> settings = null;
		private static bool first_run_out_enable = true;
		private const bool debug_enable = false;
		public const string MyServiceName = "iedup";
		private static System.Timers.Timer ss_timer = null;
		private static bool timers_enable = false;
		public static char[] badchars = new char[]{'\0'};
		public static string my_progdata_path = null;
		public static string settings_path = null;
		
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
			string loggedOnUserName = null;
			Dictionary<string, string> body = new Dictionary<string, string>();
			body["section"] = "track";
			body["mode"] = "create";
			body.Add("MachineName", Environment.MachineName);
			//string IP = Request.UserHostName;
			string HostName = IEdu.GetHostName();  // DetermineHostName(IP);
			body.Add("HostName", HostName);
			StreamWriter outs = null;
			if (debug_enable||first_run_out_enable) outs = new StreamWriter(text_path);
			if (outs!=null && err!=null) {
				outs.WriteLine(err);
				err = null;
			}
			try {
				//list logged on users as per Sameet from <https://stackoverflow.com/questions/1244000/find-out-the-current-users-username-when-multiple-users-are-logged-on>
				ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT UserName FROM Win32_ComputerSystem");
				
				int session_count = 0;
				ManagementObjectCollection queryObjs = searcher.Get();
				
				foreach (ManagementObject queryObj in queryObjs)
	            {
					string prefix = "session_i_"+session_count.ToString()+"_";
					//NOTE: iterating queryObj as a Dictionary results in "ManagementObjectSearcher does not contain a public definition for GetEnumerator"
					foreach(PropertyData pd in queryObj.Properties)
					{
						body.Add(prefix + pd.Name, pd.Value.ToString());
					}
					
	                loggedOnUserName = queryObj["UserName"].ToString();
	                loggedOnUserName = loggedOnUserName.Substring(loggedOnUserName.LastIndexOf('\\') + 1);
	                body["UserName"] = loggedOnUserName; //TODO: only add if known to be current active local session user
	            }
				if (!body.ContainsKey("UserName")) {
					//add all if active local session user was not found above:
					int user_count = 0;
					foreach (ManagementObject queryObj in queryObjs)
		            {
		                loggedOnUserName = queryObj["UserName"].ToString();
		                loggedOnUserName = loggedOnUserName.Substring(loggedOnUserName.LastIndexOf('\\') + 1);
		                body.Add("UserNames_i_"+user_count, loggedOnUserName);
		            }
				}
			}
			catch (Exception ex) {
				if (outs!=null) outs.WriteLine("Could not finish getting users: "+ex.ToString());
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
	                
	                Wlan.WlanBssEntryN[] wlanBssEntries = wlanIface.GetNetworkBssList();
	                foreach (Wlan.WlanBssEntryN network in wlanBssEntries)
	                {
	                    string prefix = "remotewifi_i_"+collected_count.ToString()+"_";
	                    int rss = network.BaseEntry.rssi;
	                    //TODO: check network.IEs too
	                    //     MessageBox.Show(rss.ToString());
	                    byte[] remote_mac_bytes = network.BaseEntry.dot11Bssid;
	
	                    string remote_mac_s = "";
	
	                    for (int i = 0; i < remote_mac_bytes.Length; i++) {
	                        remote_mac_s += remote_mac_bytes[i].ToString("x2").PadLeft(2, '0').ToUpper();
	                    }
	                    
						body.Add(prefix + "MAC", remote_mac_s);
	                    body.Add(prefix + "SSID", System.Text.ASCIIEncoding.ASCII.GetString(network.BaseEntry.dot11Ssid.SSID).ToString().Trim(badchars));
	                    body.Add(prefix + "signal_percent", network.BaseEntry.linkQuality.ToString());
	                    body.Add(prefix + "type", network.BaseEntry.dot11BssType.ToString());
	                    body.Add(prefix + "RSSID", rss.ToString());
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
	        
	        try{
        		if (outs!=null) outs.WriteLine("settings:");
        		foreach(KeyValuePair<string, string> entry in settings)
				{
					if (outs!=null) outs.WriteLine("  " + entry.Key + ": " + entry.Value);
					body["settings_k_"+entry.Key] = entry.Value;
				}
				if (debug_enable) Thread.Sleep(500);  // wait for file
	        }
	        catch (Exception ex) {
	        	if (outs!=null) {
	        		outs.WriteLine("error: could not finish appending settings to telemetry");
	        		outs.WriteLine("exception: >");
	        		outs.WriteLine(IEdu.foldable_yaml_value("  ",ex.ToString()));
	        	}
	        }
	        
	        try {
	        	if (outs!=null) outs.WriteLine("settings_path: "+settings_path);
		        if (settings.ContainsKey("ping_url") && settings["ping_url"].Length>0) {
	        		string form_method = null; //ok to be null--http_form will default to POST
	        		if (settings.ContainsKey("form_method")) form_method = settings["form_method"];
		        	string response = IEdu.http_send_as_form(settings["ping_url"], form_method, body);
		        	if (outs!=null) {
		        		outs.WriteLine("posted_to: "+settings["ping_url"]);
		        		outs.WriteLine("# empty response is ok below");
		        		if (response!=null) outs.WriteLine("response: '"+response+"'");
		        		else outs.WriteLine("response: ~");
		        	}
		        	if (response!=null) {
		        		string[] responses = null;
		        		if (response.Contains("\n")) {
		        			responses = response.Split(new char[]{'\n'});
		        		}
		        		else responses = new string[] {response};
		        		string this_section = null;
		        		for (int r_i=0; r_i<responses.Length; r_i++) {
		        			string this_response_trim = responses[r_i].Trim();
		        			if (!this_response_trim.StartsWith("#") && this_response_trim.Length>0) {
			        			if (this_response_trim.EndsWith(":")&&!responses[r_i].StartsWith(" ")) {
			        				this_section = this_response_trim.Substring(0,this_response_trim.Length-1);
			        				if (outs!=null) outs.WriteLine("notice: response contains object named "+this_section);
			        			}
			        			else {
			        				if (!responses[r_i].StartsWith(" ")) this_section = null;
			        				int name_start_i = 0;
			        				int name_ender_i = this_response_trim.IndexOf(":",name_start_i);
			        				if (name_ender_i>-1) {
			        					string this_name = this_response_trim.Substring(name_start_i,name_ender_i-name_start_i);
			        					int val_start_i = name_ender_i+1;
			        					string this_val = this_response_trim.Substring(val_start_i).Trim();
			        					if (this_section=="null") {
			        						if (this_name=="success") {
			        							if (outs!=null) outs.WriteLine(responses[r_i]);
			        						}
			        						else if (this_name=="error") {
			        							if (outs!=null) outs.WriteLine(responses[r_i]);
			        						}
			        						else if (this_name=="notice") {
			        							if (outs!=null) outs.WriteLine(responses[r_i]);
			        						}
			        						else {
			        							if (outs!=null) outs.WriteLine("warning: got unknown data \""+responses[r_i]+"\" in unknown section named "+((this_section!=null)?("\""+this_section+"\""):"null"));
			        						}
			        					}
			        					else if (this_section=="settings") {
				        					settings[this_name] = this_val;
				        					if (outs!=null) outs.WriteLine("changed setting "+this_name+" to '"+this_val+"'");
				        					save_settings();
				        				}
				        				else {
				        					if (outs!=null) outs.WriteLine("warning: found data \""+responses[r_i]+"\" in unknown section named "+((this_section!=null)?("\""+this_section+"\""):"null"));
				        				}
			        				}
			        				else if (outs!=null) outs.WriteLine("error: bad syntax in a response line--missing ':' in '"+this_response_trim+"'");
			        			}
		        			}
		        		}
		        	}
		        }
	        	else if (outs!=null) outs.WriteLine("error: missing ping_url in capture_data");
	        }
	        catch (Exception ex) {
				if (outs!=null) outs.WriteLine("error: Could not finish posting: "+ex.ToString());
	        }
    		if (outs!=null) {
	        	try {outs.Close();} catch {}; //don't care
    			outs = null;
    		}
	        
		}//end capture_data
		
		private void install_sibling() {
			string s_name_noext = "iedusm";  // install the OTHER program not this one (for update)
			string s_path = IEdu.get_software_destination_file_path(s_name_noext, false);
			
			bool install_as_service_enable = true;
			if (s_path==null) {
				//TODO: copy iedusm from somewhere
				s_path = IEdu.get_software_destination_file_path(s_name_noext, false);
			}
			if (s_path!=null) {
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
			    //TODO: finish this asdf
			}
		}
		
		private void uninstall_sibling() {
			string s_name_noext = "iedusm"; //uninstall the OTHER program not this one (for update)
			string s_path = IEdu.get_software_destination_file_path(s_name_noext, false);
			
			if (s_path!=null) ManagedInstallerClass.InstallHelper(new string[] { "/u", s_path });
			else Console.Error.WriteLine("iedup WARNING: "+s_path+" was already uninstalled.");
		}
		
		//private async Task ss_timer_Elapsed//would normally be a Task but ok not since is event --see https://stackoverflow.com/questions/39260486/is-it-okay-to-attach-async-event-handler-to-system-timers-timer
		//private async void ss_timer_ElapsedAsync(object sender, ElapsedEventArgs e) {
		//	ss_timer.Stop();
		//	await Task.Run(() => capture_data());
		//	if (timers_enable&&(ss_timer!=null)) ss_timer.Start();
		//}
		private void ss_timer_ElapsedSync(object sender, ElapsedEventArgs e) {
			ss_timer.Stop();
			ss_timer.Enabled = false;
			capture_data();
			if (timers_enable&&(ss_timer!=null)) {
				ss_timer.Enabled = true;
				ss_timer.Start();
			}
	        first_run_out_enable = false;
		}
		
		public static void save_settings() {
			StreamWriter outs = null;
			try {
				outs = new StreamWriter(settings_path);
				foreach(KeyValuePair<string, string> entry in settings)
				{
					outs.WriteLine(entry.Key + ": " + entry.Value);
				}
				outs.Close();
			}
			catch (Exception ex) {
				try {
					outs = new StreamWriter(text_path);
					outs.WriteLine("Could not finish saving settings: "+ex.ToString());
					outs.Close();
				}
				catch (Exception ex2) {
					Console.Error.WriteLine("Could not finish saving settings: "+ex.ToString());
					Console.Error.WriteLine("Could not finish writing save settings error: "+ex2.ToString());
				}
			}
		}
		
		/// <summary>
		/// Start this service.
		/// </summary>
		private static string image_name = "tmp.jpg";
		private static string text_name = "sm.log";
		private static string image_path = null;
		private static string text_path = null;
		protected override void OnStart(string[] args)
		{			
			settings = new Dictionary<string, string>();
			try {
				temp_d_path = "C:\\tmp"; // Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
				if (!Directory.Exists("C:\\")) {
					if (Directory.Exists("/tmp")) temp_d_path = "/tmp";
				}
				if (!Directory.Exists(temp_d_path)) Directory.CreateDirectory(temp_d_path);
			}
			catch (Exception ex) {
				Console.Write("Could not finish getting/creating temp folder: "+ex.ToString());
			}
			image_path = Path.Combine(temp_d_path, image_name);
			text_path = Path.Combine(temp_d_path, text_name);			
			try {
				my_progdata_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "iedup");
				settings_path = Path.Combine(my_progdata_path, "settings.yml");
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
			}
			catch (Exception exn) {
				if (err==null) err = "";
				err += "Could not finish getting settings: "+exn.ToString();
			}
			try {
				timers_enable = true;
				ss_timer = new System.Timers.Timer(debug_enable?5000:30000);  // 10000ms is 10s
				//ss_timer.AutoReset = true;  // loop (do not loop if starting manually anyway at end of event)
				//ss_timer.Elapsed += ss_timer_ElapsedAsync;  // ss_timer.Elapsed += async (sender, arguments) => await ss_timer_Elapsed(sender, arguments);
				ss_timer.Elapsed += ss_timer_ElapsedSync;
				
				ss_timer.Enabled = true;  // default is false
				ss_timer.Start();
			}
			catch (Exception ex) {
				StreamWriter outs = new StreamWriter(Path.Combine(temp_d_path, "sm.log"));
				outs.WriteLine("error: Could not finish starting timer(s): "+ex.ToString());
				outs.Close();
			}
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
