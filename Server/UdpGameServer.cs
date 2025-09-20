
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
    
}
