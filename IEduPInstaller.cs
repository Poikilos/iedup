/*
 * Created by SharpDevelop.
 * User: Owner
 * Date: 11/29/2017
 * Time: 1:25 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Collections;

namespace iedu
{
	/// <summary>
	/// Based on SharpDevelop-generated ProjectInstaller class (renamed)
	/// and on fre0n's answer from <https://stackoverflow.com/questions/2253051/credentials-when-installing-windows-service>.
	/// </summary>
	[RunInstaller(true)]
	public class IEduPInstaller : Installer
	{
		private ServiceProcessInstaller serviceProcessInstaller;
		private ServiceInstaller serviceInstaller;
		
		public IEduPInstaller()
		{
			serviceProcessInstaller = new ServiceProcessInstaller();
			serviceInstaller = new ServiceInstaller();
			// Here you can set properties on serviceProcessInstaller or register event handlers
			serviceProcessInstaller.Account = ServiceAccount.LocalSystem; //LocalService
			
			serviceInstaller.ServiceName = IEduP.MyServiceName;
			serviceInstaller.StartType = ServiceStartMode.Automatic;
			serviceInstaller.DelayedAutoStart = true;
			this.Installers.AddRange(new Installer[] { serviceProcessInstaller, serviceInstaller });
		}
		/*
		/// <summary>
		/// called by the install utility such as the ManagedInstallerClass.InstallHelper function
		/// </summary>
		/// <param name="savedState"></param>
	    public override void Commit(IDictionary savedState)
	    {
	        base.Commit(savedState);
	
	        // This will automatically start your service upon completion of the installation.
	        try
	        {
	            var serviceController = new ServiceController(IEduP.MyServiceName);
	            serviceController.Start();
	        }
	        catch
	        {
	            Console.Error.WriteLine("ERROR: The service couldn't be started: you will have to do it manually using services.msc");
	        }
	    }
	    */
	}
}
