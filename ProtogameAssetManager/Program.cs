using System;
using Process4;
using Ninject;
using NDesk.Options;
using Process4.Attributes;
using Protogame;
using System.Reflection;
using System.IO;
using System.Collections.Generic;

namespace ProtogameAssetManager
{
    [Distributed(Architecture.ServerClient, Caching.PushOnChange)]
    static class Program
    {
        static void RegisterEditorsFromAssembly(Assembly assembly, IKernel kernel)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;
                if (type.Assembly == typeof(FontAsset).Assembly)
                    continue;
                if (typeof(IAssetEditor).IsAssignableFrom(type))
                {
                    Console.WriteLine("Binding IAssetEditor: " + type.Name);
                    kernel.Bind<IAssetEditor>().To(type);
                }
                if (typeof(IAssetLoader).IsAssignableFrom(type))
                {
                    Console.WriteLine("Binding IAssetLoader: " + type.Name);
                    kernel.Bind<IAssetLoader>().To(type);
                }
                if (typeof(IAssetSaver).IsAssignableFrom(type))
                {
                    Console.WriteLine("Binding IAssetSaver: " + type.Name);
                    kernel.Bind<IAssetSaver>().To(type);
                }
            }
        }
    
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var connectToRunningGame = false;
            var options = new OptionSet
            {
                { "connect", "Internal use only (used by the game client).", v => connectToRunningGame = true }
            };
            try
            {
                options.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.Write("ProtogameAssetManager.exe: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `ProtogameAssetManager.exe --help` for more information.");
                return;
            }
            
            var kernel = new StandardKernel();
            kernel.Load<Protogame2DIoCModule>();
            kernel.Load<ProtogameAssetIoCModule>();
            kernel.Load<ProtogamePlatformingIoCModule>();
            kernel.Load<AssetManagerIoCModule>();

            var runningFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var workingDirectoryInfo = new DirectoryInfo(Environment.CurrentDirectory);
            var scannedUnique = new List<string>();
            foreach (var file in runningFile.Directory.GetFiles("*.dll"))
            {
                if (scannedUnique.Contains(file.FullName))
                    continue;
                Console.WriteLine("Scanning " + file.Name);
                try
                {
                    RegisterEditorsFromAssembly(Assembly.LoadFrom(file.FullName), kernel);
                    scannedUnique.Add(file.FullName);
                }
                catch (BadImageFormatException) { }
            }
            foreach (var file in runningFile.Directory.GetFiles("*.exe"))
            {
                if (scannedUnique.Contains(file.FullName))
                    continue;
                Console.WriteLine("Scanning " + file.Name);
                try
                {
                    RegisterEditorsFromAssembly(Assembly.LoadFrom(file.FullName), kernel);
                    scannedUnique.Add(file.FullName);
                }
                catch (BadImageFormatException) { }
            }
            foreach (var file in workingDirectoryInfo.GetFiles("*.dll"))
            {
                if (scannedUnique.Contains(file.FullName))
                    continue;
                Console.WriteLine("Scanning " + file.Name);
                try
                {
                    RegisterEditorsFromAssembly(Assembly.LoadFrom(file.FullName), kernel);
                    scannedUnique.Add(file.FullName);
                }
                catch (BadImageFormatException) { }
            }
            foreach (var file in workingDirectoryInfo.GetFiles("*.exe"))
            {
                if (scannedUnique.Contains(file.FullName))
                    continue;
                Console.WriteLine("Scanning " + file.Name);
                try
                {
                    RegisterEditorsFromAssembly(Assembly.LoadFrom(file.FullName), kernel);
                    scannedUnique.Add(file.FullName);
                }
                catch (BadImageFormatException) { }
            }
            
            if (connectToRunningGame)
            {
                var node = new LocalNode();
                node.Network = new ProtogameAssetManagerNetwork(node, true);
                node.Join();
                var assetManagerProvider = new NetworkedAssetManagerProvider(node, kernel);
                kernel.Bind<IAssetManagerProvider>().ToMethod(x => assetManagerProvider);
            }
            else
                kernel.Bind<IAssetManagerProvider>().To<LocalAssetManagerProvider>().InSingletonScope();

            using (var game = new AssetManagerGame(kernel))
            {
                game.Run();
            }
        }
    }
}