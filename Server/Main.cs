using System;

namespace CoolNameSpace
{
    class Application
    {
        static Server server;
        public static void Main()
        {
            server = new Server();
            server.StartServer();
        }
    }
}
