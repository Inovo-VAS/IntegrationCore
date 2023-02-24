using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace Lnksnk.Core.Servicing
{
    public  class Services
    {
        private static Services services = new Services();
        public static Services SERVICES()
        {
            return services;
        }

        private static ServiceController[] emptyServices = new ServiceController[] { };
        public ServiceController[] ServiceControllers(params string[] serviceNames)
        {
            List<ServiceController> servicesfound = null;
            try
            {
                foreach (var srvc in ServiceController.GetServices())
                {
                    foreach (var srvcnme in serviceNames)
                    {
                        if (srvc.ServiceName.ToLower().Equals(srvcnme.ToLower())||srvc.DisplayName.ToLower().Equals(srvcnme.ToLower()))
                        ((servicesfound == null ? (servicesfound = new List<ServiceController>()) : servicesfound)).Add(srvc);
                    }
                }
            } catch
            {
                //..
            }
            return servicesfound == null ? emptyServices : servicesfound.ToArray(); 
        }

        public void StartService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = ServiceByName(serviceName);
            try
            {
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }
            }
            catch
            {
                // ...
            }
        }

        public void StopService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                if (service.Status == ServiceControllerStatus.Running)
                {
                    TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
    
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                }
            }
            catch
            {
                // ...
            }
        }


        public ServiceController ServiceByName(string serviceName)
        {
            foreach (var svr in ServiceControllers(serviceName))
            {
                return svr;
            }
            return null;
        }

        public void RestartService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = ServiceByName(serviceName);
            try
            {
                int millisec1 = System.Environment.TickCount;
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                }
                // count the rest of the timeout
                int millisec2 = System.Environment.TickCount;
                timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }
            }
            catch
            {
                // ...
            }
        }
    }
}
