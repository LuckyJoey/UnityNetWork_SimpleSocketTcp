using UnityEngine;
using System.Collections;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using System.Collections.Generic;
/// <summary>  
/// scoket服务器监听端口脚本  
/// </summary>  
public class SocketServer : MonoBehaviour
{

    private Thread thStartServer;//定义启动socket的线程  
    void Start()
    {
        thStartServer = new Thread(StartServer);
        thStartServer.Start();//启动该线程  
    }

    void Update()
    {
    }

    private void StartServer()
    {
        const int bufferSize = 8792;//缓存大小,8192字节  
        IPAddress ip = IPAddress.Parse("127.0.0.1");

        TcpListener tlistener = new TcpListener(ip, 9999);
        tlistener.Start();
        Debug.Log("Socket服务器监听启动......");

        //TcpClient remoteClient = tlistener.AcceptTcpClient();//接收已连接的客户端,阻塞方法  
        //Debug.Log("客户端已连接！local:" + remoteClient.Client.LocalEndPoint + "<---Client:" + remoteClient.Client.RemoteEndPoint);
        //NetworkStream streamToClient = remoteClient.GetStream();//获得来自客户端的流  
        do
        {
            GameClient user = new GameClient(tlistener.AcceptTcpClient());  
            Debug.Log(user._clientIP + "   client logined"); 
            //try  //直接关掉客户端，服务器端会抛出异常  
            //{
            //    //接收客户端发送的数据部分  
            //    byte[] buffer = new byte[bufferSize];//定义一个缓存buffer数组  
            //    int byteRead = streamToClient.Read(buffer, 0, bufferSize);//将数据搞入缓存中（有朋友说read()是阻塞方法，测试中未发现程序阻塞）  
            //    if (byteRead == 0)//连接断开，或者在TCPClient上调用了Close()方法，或者在流上调用了Dispose()方法。  
            //    {
            //        Debug.Log("客户端连接断开......");
            //        break;
            //    }

            //    string msg = Encoding.Unicode.GetString(buffer, 0, byteRead);//从二进制转换为字符串对应的客户端会有从字符串转换为二进制的方法  
            //    Debug.Log("接收数据：" + msg + ".数据长度:[" + byteRead + "byte]");
            //}
            //catch (Exception ex)
            //{
            //    Debug.Log("客户端异常：" + ex.Message);
            //    break;
            //}
        }
        while (true);
    }

    void OnApplicationQuit()
    {
        thStartServer.Abort();//在程序结束时杀掉线程，想起以前老谢给我讲的，起线程就像拉屎，完事一定要记得自己擦，系统不会给你擦，经测试不擦第二次启动unity会无响应  
    }


}

public class GameClient
{
    public static Hashtable allClient = new Hashtable();
    public static List<string> ipList = new List<string>();
    private TcpClient _client;
    public string _clientIP;
    public string _clientNick;
    private byte[] data;

    public GameClient(TcpClient client)
    {
        this._client = client;
        this._clientIP = client.Client.RemoteEndPoint.ToString();
        if (allClient.Count <= 2)
        {
            allClient.Add(this._clientIP, this);
            ipList.Add(this._clientIP);
            data = new byte[this._client.ReceiveBufferSize];
            client.GetStream().BeginRead(data, 0, System.Convert.ToInt32(this._client.ReceiveBufferSize), RceiveMessage, null);
            this.sendMessage("login success");
        }
        else
        {
            this.sendMessage("connect num is max,so connect failed");
        }
    }
    public void RceiveMessage(IAsyncResult ar)
    {
        int bytesread;
        try
        {
            lock (this._client.GetStream())
            {
                bytesread = this._client.GetStream().EndRead(ar);
            }
            if (bytesread < 1)
            {
                allClient.Remove(this._clientIP);
                Guangbo("server error");
                return;
            }
            else
            {
                string messageReceived = System.Text.Encoding.UTF8.GetString(data, 0, bytesread);
                Debug.Log("server recive :" + messageReceived);
                if (!messageReceived.Contains("+"))
                {
                    this._clientNick = messageReceived;
                    Debug.Log(this._clientIP + this._clientNick);
                }
                else
                {
                    string[] strVect = messageReceived.Split('+');
                }
                lock (this._client.GetStream())
                {
                    Debug.Log("lock (this._client.GetStream()):");
                    this._client.GetStream().BeginRead(data, 0, System.Convert.ToInt32(this._client.ReceiveBufferSize),
                        RceiveMessage, null);
                }
            }
            this.sendMessage("test_serverSend:"+System.DateTime.Now);
        }
        catch (Exception ex)
        {
            Debug.Log("something wrong");
            allClient.Remove(this._clientIP);
            Guangbo(this._clientNick + " leave");
        }
    }

    public void Guangbo(string message)
    {
        Debug.Log("Guangbo:" + message);
        foreach (DictionaryEntry c in allClient)
        {
            ((GameClient)(c.Value)).sendMessage(message);
        }
    }

    public void sendMessage(string message)
    {
        Debug.Log("server_sendMessage:" + this._clientNick + " ,send: " + message);
        try
        {
            System.Net.Sockets.NetworkStream ns;
            lock (this._client.GetStream())
            {
                ns = this._client.GetStream();
            }
            byte[] bytestosend = System.Text.Encoding.UTF8.GetBytes(message);
            ns.Write(bytestosend, 0, bytestosend.Length);
            ns.Flush();
        }
        catch (Exception ex)
        {
            Debug.Log("sendMessage_ex:"+ex.Message);
        }
    }
}