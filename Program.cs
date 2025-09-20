using BasicGameServer;

namespace UdpServer;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("UDP 게임 서버 시작");
        var server = new UdpGameServer(9999);
        var serverTask = server.StartAsync();
        
        Console.WriteLine("서버 종료 'q'");
        while (true)
        {
            var input = Console.ReadLine();
            if (input == "q")
            {
                break;
            }
        }
        
        // TODO: 서버 종료 처리
    }
}