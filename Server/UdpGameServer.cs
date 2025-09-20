
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BasicGameServer;

/* UDP(user Datagram Protocal)
 * * 1. 비연결 지향적 프로토콜  
   *    - TCP와 달리 연결 설정 과정(3-way handshake)이 없음  
   *    - 클라이언트가 바로 데이터를 전송할 수 있음  
   * * 2. 신뢰성이 없는 프로토콜  
   *    - 패킷 손실 가능성이 있음 (네트워크 상황에 따라)  
   *    - 패킷 순서가 바뀔 수 있음  
   *    - 중복 패킷이 도착할 수 있음  
   * * 3. 빠른 속도  
   *    - 오버헤드가 적어서 TCP보다 빠름  
   *    - 실시간 게임에 적합 (약간의 패킷 손실보다는 속도가 중요)  
   * * 4. 사용 사례  
   *    - 실시간 게임 (FPS, 레이싱 게임)  
   *    - 실시간 영상/음성 스트리밍  
   *    - DNS 조회  
   *    - 온라인 게임의 위치 정보 전송  */

public class UdpGameServer
{
    private readonly int _port; // 포트 
    private UdpClient _udpServer;  // UDP 소켓을 래핑
    private CancellationTokenSource _cancellationTokenSource;  // 취소 토큰 소스
    
    // 연결된 클라이언트 목록
    private readonly ConcurrentDictionary<IPEndPoint, DateTime> _connectedClients;
    
    // 생성자
    public UdpGameServer(int port)
    {
        _port = port;
        _connectedClients = new ConcurrentDictionary<IPEndPoint, DateTime>();
    }
    
    // 서버 시작 - UDP 리스너 구동
    public async Task StartAsync()
    {
        try
        {
            // UDP서버 소켓 생성 및 포트 바인딩
            _udpServer = new UdpClient(_port);
            // 취소 토큰 생성
            _cancellationTokenSource = new CancellationTokenSource();
        
            Console.WriteLine($"UDP 서버가동 시작 : 포트 {_port}");
            
            // 메시지 수신 처리
            await ReceiveMessageAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine("서버시작 오류 " + e.Message);
            throw;
        }
    }
    
    // 메시지 수신 메소드
    private async Task ReceiveMessageAsync()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                // UDP 패킷 수신 (비동기)
                var result = await _udpServer.ReceiveAsync();
                // 수신데이터 IP Addr,port 정보
                var clientEndPoint = result.RemoteEndPoint;
                // 수신 받은 데이터를 (byte array)
                var receivedData = result.Buffer;
                
                // 클라이언트 추가 또는 업데이트
                _connectedClients.AddOrUpdate(clientEndPoint, DateTime.Now, (key, value) => DateTime.Now);
                Console.WriteLine($"메시지 수신: {clientEndPoint} ({receivedData.Length} bytes)");
                
                // 수신한 메시지를 처리하는 메소드 호출
                await ProcessMessageAsync(clientEndPoint, receivedData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
    
    // 메시지 전송 메소드 
    private async Task SendToClientAsync(EndPoint clientEndPoint, string message)
    {
        try
        {
            // 문자열을 바이트로 변환
            var responseData = System.Text.Encoding.UTF8.GetBytes(message);
            // 전송 SendAsync() 사용
            await _udpServer.SendAsync(responseData, responseData.Length, (IPEndPoint)clientEndPoint);
            
            Console.WriteLine($"메시지 전송: {clientEndPoint} -> {message}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    
    // 메시지 종류별로 처리하는 메소드
    private async Task ProcessMessageAsync(EndPoint clientEndPoint, byte[] data)
    {
        try
        {
            var message = System.Text.Encoding.UTF8.GetString(data).Trim();
            Console.WriteLine("클라이언트 메시지" + message);

            // Ping - Pong
            if (message.ToUpper() == "PING")
            {
                await SendToClientAsync(clientEndPoint, "PONG");
            }
            else if (message.ToUpper().StartsWith("MOVE:"))
            {
                Console.WriteLine($"플레이어 이동:{message}");
                // 브로드캐스팅 로직 (송신한 클라이언트를 제외한 모든 유저에게 송신)
                await BroadcastMessageAsync(clientEndPoint, message);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    
    // Broadcasting Logic
    private async Task BroadcastMessageAsync(EndPoint senderEndPoint, string message)
    {
        var messageData = Encoding.UTF8.GetBytes(message);
        int sendCount = 0;
        
        // 2분 이내에 활성화 된 클라이언트를 필터링 하기 위한 시간
        var cutoffTime = DateTime.Now.AddMinutes(-2);

        foreach (var kvp in _connectedClients)
        {
            Console.WriteLine($"{kvp.Key} - (활성 : {kvp.Value > cutoffTime})");

            if (kvp.Value > cutoffTime)
            {
                await _udpServer.SendAsync(messageData, messageData.Length, (IPEndPoint)senderEndPoint);
                sendCount++;
            }
        }
        
        Console.WriteLine($"브로드캐스트: {sendCount}명에게 전송 -> {message}");
    }
    
}
