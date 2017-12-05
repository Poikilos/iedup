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
using System.ServiceProcess;
using System.Text;
using System.Configuration.Install;  // ManagedInstallerClass etc
using System.Reflection;  // Assembly etc
namespace iedu
{
	
	static class Program
	{
		/// <summary>
		/// This method starts the service.
		/// </summary>
		static void Main(string[] args)
		{
			if (System.Environment.UserInteractive)
            {
                //Though normally installed via iedusm, this case is here for convenience.
                if (args.Length > 0)
                {
                    switch (args[0])
                    {
                        case "-install":
                            {
                                ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                                	//starting it as per codemonkey from <https://stackoverflow.com/questions/1036713/automatically-start-a-windows-service-on-install>:
                                	//results in access denied (same if done manually, unless "Log on as" is changed from LocalService to Local System
									//serviceInstaller
                                	//using (ServiceController sc = new ServiceController(serviceInstaller.ServiceName))
                                	//using (ServiceController sc = new ServiceController("iedusm"))
								    //{
								    //     sc.Start();
								    //}
									break;
                            }
                        case "-uninstall":
                            {
                                ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                                break;
                            }
                        case "-delete_self":
                            {
                    			IEdu.delete_self(5);
                                break;
                            }
                    }
                }
            }
            else
            {
				//ServiceBase.Run(new ServiceBase[] { new iedup() });
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new IEduP() };
                ServiceBase.Run(ServicesToRun);
            }			
		}//end Main
	}
}
