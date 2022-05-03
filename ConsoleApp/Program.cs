using System;
using NolNetwork;

namespace ConsoleApp
{
    internal static class Program
    {
        private const int PORT = 7777;
        private const string IP_ADDRESS = "127.0.0.1";
    
        private static void Main(string[] args)
        {
            if (args.Length == 0)
                return;
            
            switch (args[0])
            {
                case "-host":   StartHost();   break;
                case "-client": StartClient(); break;
                case "-server": StartServer(); break;
            }
        }

        private static void StartHost()
        {
            var server = new Server(IP_ADDRESS, PORT);
            var client = new Client();
            var isDone = false;
            
            server.Start();
            client.Connect(IP_ADDRESS, PORT);
            
            while (!isDone)
            {
                server.Update();
                client.Update();
                
                if (Console.KeyAvailable)
                {
                    var pressedKey = Console.ReadKey(true).Key;
                    
                    isDone = pressedKey == ConsoleKey.Escape;

                    if (pressedKey == ConsoleKey.Spacebar)
                        client.SendMessage("Ping");
                }
            }
            
            server.Shutdown();
            client.Shutdown();
        }

        private static void StartServer()
        {
            var server = new Server(IP_ADDRESS, PORT);
            var isDone = false;
            
            server.Start();
            
            while (!isDone)
            {
                server.Update();
                
                if (Console.KeyAvailable)
                    isDone = Console.ReadKey(true).Key == ConsoleKey.Escape;
            }
            
            server.Shutdown();
        }

        private static void StartClient()
        {
            var client = new Client();
            var isDone = false;
            
            client.Connect(IP_ADDRESS, PORT);
            
            while (!isDone)
            {
                client.Update();
                
                if (Console.KeyAvailable)
                {
                    var pressedKey = Console.ReadKey(true).Key;
                    
                    isDone = pressedKey == ConsoleKey.Escape;

                    if (pressedKey == ConsoleKey.Spacebar)
                        client.SendMessage("Ping");
                }
            }
            
            client.Shutdown();
        }
    }
}