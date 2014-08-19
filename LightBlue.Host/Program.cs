﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using LightBlue.Host.Stub;
using LightBlue.Infrastructure;

namespace LightBlue.Host
{
    public static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowText(IntPtr hWnd, string text);

        public static void Main(string[] args)
        {
            var hostArgs = HostArgs.ParseArgs(args);

            var handle = Process.GetCurrentProcess().MainWindowHandle;

            SetWindowText(handle, hostArgs.Title);

            var appDomainSetup = new AppDomainSetup
            {
                ApplicationBase = hostArgs.ApplicationBase
            };

            var configurationFile = hostArgs.Assembly + ".config";
            if (File.Exists(configurationFile))
            {
                ConfigurationManipulation.RemoveAzureTraceListenerFromConfiguration(configurationFile);
                appDomainSetup.ConfigurationFile = configurationFile;
            }

            var destinationHostStubPath = Path.Combine(hostArgs.ApplicationBase, Path.GetFileName(typeof(HostStub).Assembly.Location));
            if (!File.Exists(destinationHostStubPath))
            {
                try
                {
                    File.Copy(typeof(HostStub).Assembly.Location, destinationHostStubPath, true);
                }
                catch (IOException)
                {
                    Console.WriteLine("Could not copy Host Stub. Assuming this is because it already exists and continuing.");
                }
            }

            var appDomain = AppDomain.CreateDomain(
                "LightBlue",
                null,
                appDomainSetup);

            Trace.Listeners.Add(new ConsoleTraceListener());

            var stub = (HostStub) appDomain.CreateInstanceAndUnwrap(
                typeof(HostStub).Assembly.FullName,
                typeof(HostStub).FullName);

            stub.ConfigureTracing(new TraceShipper());

            stub.Run(workerRoleAssembly: hostArgs.Assembly,
                configurationPath: hostArgs.ConfigurationPath,
                serviceDefinitionPath: hostArgs.ServiceDefinitionPath,
                roleName: hostArgs.RoleName);
        }
    }
}