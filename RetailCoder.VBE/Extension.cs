﻿using Extensibility;
using Microsoft.Vbe.Interop;
using Ninject;
using Ninject.Extensions.Factory;
using Rubberduck.Root;
using Rubberduck.UI;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Ninject.Extensions.Interception;
using NLog;

namespace Rubberduck
{
    [ComVisible(true)]
    [Guid(ClassId)]
    [ProgId(ProgId)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    // ReSharper disable once InconsistentNaming // note: underscore prefix hides class from COM API
    public class _Extension : IDTExtensibility2
    {
        private const string ClassId = "8D052AD8-BBD2-4C59-8DEC-F697CA1F8A66";
        private const string ProgId = "Rubberduck.Extension";

        private IKernel _kernel;
        private App _app;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public void OnAddInsUpdate(ref Array custom)
        {
        }

        public void OnBeginShutdown(ref Array custom)
        {
        }

        // ReSharper disable InconsistentNaming
        public void OnConnection(object Application, ext_ConnectMode ConnectMode, object AddInInst, ref Array custom)
        {
            _kernel = new StandardKernel(new NinjectSettings{LoadExtensions = true}, new FuncModule(), new DynamicProxyModule());

            try
            {
                var currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += LoadFromSameFolder;
                _kernel.Load(new RubberduckModule((VBE)Application, (AddIn)AddInInst));
                _app = _kernel.Get<App>();
                _app.Startup();
            }
            catch (Exception exception)
            {
                _logger.Fatal(exception);
                System.Windows.Forms.MessageBox.Show(exception.ToString(), RubberduckUI.RubberduckLoadFailure, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath))
            {
                return null;
            }

            var assembly = Assembly.LoadFile(assemblyPath);
            return assembly;
        }

        public void OnStartupComplete(ref Array custom)
        {
        }

        public void OnDisconnection(ext_DisconnectMode RemoveMode, ref Array custom)
        {
            if (_app != null)
            {
                _app.Shutdown();
                _app = null;
            }

            if (_kernel != null)
            {
                _kernel.Dispose();
                _kernel = null;
            }
        }
    }
}
