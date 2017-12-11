/*
 * Created by SharpDevelop.
 * User: Owner
 * Date: 12/5/2017
 * Time: 5:15 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Configuration.Install; //reference: System.Configuration.Install.dll; provides Installer
using System.ComponentModel; //provides RunInstaller, RunInstallerAttribute
using System.Collections; //provides IDictionary?
using System.ServiceProcess; //provides ServiceProcessInstaller, ServiceInstaller, ServiceController
//references:
//deprecated references:
//System.ServiceProcess.dll (didn't provide RunInstaller etc)
namespace iedu
{
	/// <summary>
	/// (moved to SharpDevelop-generated ProjectInstaller class which I renamed to IEduPInstaller)
	/// based on fre0n from <https://stackoverflow.com/questions/2253051/credentials-when-installing-windows-service>.
	/// </summary>
	[RunInstaller(true)]
	public class IEduPServiceInstaller : Installer
	{
		public static string my_name = "iedup";
	    public IEduPServiceInstaller()
	    {
	        ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
	        serviceProcessInstaller.Account = ServiceAccount.LocalSystem; // Or whatever account you want
	        ServiceInstaller si = new ServiceInstaller();
	        si.DelayedAutoStart = true;
	        si.DisplayName = my_name;
	        si.Description = my_name;
	        si.DisplayName = my_name;
	        //si.HelpText
	        //si.Installers
	        //si.Parent //do set since not child
	        si.ServiceName = my_name;
	        //si.ServicesDependedOn //do not set since not parent either
	        //si.Site // I dont' know what this does
	        si.StartType = ServiceStartMode.Automatic;
	        
	        //var serviceInstaller = new ServiceInstaller
	        //{
	        //    DisplayName = "Insert the display name here",
	        //    StartType = ServiceStartMode.Automatic, // Or whatever startup type you want
	        //    Description = "Insert a description for your service here",
	        //    ServiceName = "Insert the service name here"
	        //};
	        this.Installers.Add(serviceProcessInstaller); //why was this _serviceProcessInstaller fre0n?
	        this.Installers.Add(si);
	    }
	
	    public override void Commit(IDictionary savedState)
	    {
	        base.Commit(savedState);
	
	        // This will automatically start your service upon completion of the installation.
	        try
	        {
	            Console.Error.WriteLine("Attempting self-start during commit...");
	            var serviceController = new ServiceController(my_name);
	            serviceController.Start();
	        }
	        catch
	        {
	            Console.Error.WriteLine("ERROR: The service couldn't be started: you will have to do it manually using services.msc");
	        }
	    }
	}
}
