using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.IO;
using System.Threading;
using System.Text;
public class SocketTCPServer {


    //SocketTCPServer.cs  
    private static string ip = "127.0.0.1";  
    private static int port = 5690;  
    private static Socket socketServer;  
    public static List<Socket> listPlayer = new List<Socket>();  
    private static Socket sTemp;
    static Thread threadListenAccept;
    ///<summary>  
    ///绑定地址并监听  
    ///</summary>  
    ///ip地址 端口 类型默认为TCP  
    public static void init(string ipStr, int iPort)  
    {  
        try  
        {  
            ip = ipStr;  
            port = iPort;  
            //创建服务器Socket对象，并设置相关属性 
            socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //绑定ip和端口
            socketServer.Bind(new IPEndPoint(IPAddress.Parse(ip), port)); 
            //设置最长的连接请求队列长度  //对于socketServer绑定的IP和端口开启监听
            socketServer.Listen(10);  
            Debug.Log(string.Format("启动监听{0}成功", socketServer.LocalEndPoint.ToString()));  
            //在新线程中监听客户端的连接 
            threadListenAccept = new Thread(new ThreadStart(ListenAccept));  
            threadListenAccept.Start();  
        }  
        catch (ArgumentNullException e)  
        {  
            Debug.Log(e.ToString());  
        }  
        catch (SocketException e)  
        {  
            Debug.Log(e.ToString());  
        }  
    }
    static Thread threadReceiveMessage;
    ///<summary>  
    ///监听用户连接  
    ///</summary>  
    private static void ListenAccept()  
    {  
       
        while (true)  
        {  
            //为新的客户端连接创建一个Socket对象 //如果在socketServer上有新的socket连接，则将其存入sTemp，并添加到链表            
            sTemp = socketServer.Accept();            
            listPlayer.Add(sTemp);  
            Debug.Log(string.Format("客户端{0}成功连接", sTemp.RemoteEndPoint.ToString())); 
            ////向连接的客户端发送连接成功的数据  
            //ByteBuffer buffer = new ByteBuffer();  
            //buffer.WriteString("Connected Server");  
            //sTemp.Send(WriteMessage(buffer.ToBytes()));  


            //每个客户端连接创建一个线程来接受该客户端发送的消息 
            threadReceiveMessage = new Thread((ReceiveMessage));  
            threadReceiveMessage.Start(sTemp);   
        }  
    }  


    public static void Close()
    {
        threadListenAccept.Abort();
        threadReceiveMessage.Abort();
    }

    /// <summary>  
/// 构建消息数据包  
/// </summary>  
/// <param name="Crccode">消息校验码，判断消息开始</param>  
/// <param name="sessionid">用户登录成功之后获得的身份ID</param>  
/// <param name="command">主命令</param>  
/// <param name="subcommand">子命令</param>  
/// <param name="encrypt">加密方式</param>  
/// <param name="MessageBody">消息内容（string数组）</param>  
/// <returns>返回构建完整的数据包</returns>  
public static byte[] BuildDataPackage(int Crccode,long sessionid, int command,int subcommand, int encrypt, string[] MessageBody)  
{  
    //消息校验码默认值为0x99FF  
    Crccode = 65433;  
    //消息头各个分类数据转换为字节数组（非字符型数据需先转换为网络序  HostToNetworkOrder:主机序转网络序）  
    byte[] CrccodeByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Crccode));  
    byte[] sessionidByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(sessionid));  
    byte[] commandByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(command));  
    byte[] subcommandByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(subcommand));  
    byte[] encryptByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(encrypt));  
    //计算消息体的长度  
    int MessageBodyLength = 0;  
    for (int i = 0; i < MessageBody.Length; i++)  
    {  
        if (MessageBody[i] == "")  
            break;  
        MessageBodyLength += Encoding.UTF8.GetBytes(MessageBody[i]).Length;  
    }  
    //定义消息体的字节数组（消息体长度MessageBodyLength + 每个消息前面有一个int变量记录该消息字节长度）  
    byte[] MessageBodyByte = new byte[MessageBodyLength + MessageBody.Length*4];  
    //记录已经存入消息体数组的字节数，用于下一个消息存入时检索位置  
    int CopyIndex = 0;  
    for (int i = 0; i < MessageBody.Length; i++)  
    {  
        //单个消息  
        byte[] bytes = Encoding.UTF8.GetBytes(MessageBody[i]);  
        //先存入单个消息的长度  
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length)).CopyTo(MessageBodyByte, CopyIndex);  
        CopyIndex += 4;  
        bytes.CopyTo(MessageBodyByte, CopyIndex);  
        CopyIndex += bytes.Length;  
    }  
    //定义总数据包（消息校验码4字节 + 消息长度4字节 + 身份ID8字节 + 主命令4字节 + 子命令4字节 + 加密方式4字节 + 消息体）  
    byte[] totalByte = new byte[28 + MessageBodyByte.Length];  
    //组合数据包头部（消息校验码4字节 + 消息长度4字节 + 身份ID8字节 + 主命令4字节 + 子命令4字节 + 加密方式4字节）  
    CrccodeByte.CopyTo(totalByte,0);  
    BitConverter.GetBytes(IPAddress.HostToNetworkOrder(MessageBodyByte.Length)).CopyTo(totalByte,4);  
    sessionidByte.CopyTo(totalByte, 8);  
    commandByte.CopyTo(totalByte, 16);  
    subcommandByte.CopyTo(totalByte, 20);  
    encryptByte.CopyTo(totalByte, 24);  
    //组合数据包体  
    MessageBodyByte.CopyTo(totalByte,28);  
    Debug.Log("发送数据包的总长度为："+ totalByte.Length);  
    return totalByte;  
}  
///<summary>  
///发送信息  
///</summary>  
    public static void SendMessage(byte[] sendBytes,Socket socket)  
{  

        //获取远程终结点的IP和端口信息  
        IPEndPoint ipe = (IPEndPoint)socket.RemoteEndPoint;  
        socket.Send(sendBytes, sendBytes.Length, 0);  
    
}  

    ///<summary>  
  ///接收消息  
  ///</summary>  
    private static void ReceiveMessage(object clientSocket)  
  {  
        Socket mClientSocket = (Socket)clientSocket;



      while (true)  
      {  

          //接受消息头（消息校验码4字节 + 消息长度4字节 + 身份ID8字节 + 主命令4字节 + 子命令4字节 + 加密方式4字节 = 28字节）  
          int HeadLength = 28;  
          //存储消息头的所有字节数  
          byte[] recvBytesHead = new byte[HeadLength];  
          //如果当前需要接收的字节数大于0，则循环接收  
          while (HeadLength > 0)  
          {  
              byte[] recvBytes1 = new byte[28]; 
       
              //将本次传输已经接收到的字节数置0  
              int iBytesHead = 0;  
              //如果当前需要接收的字节数大于缓存区大小，则按缓存区大小进行接收，相反则按剩余需要接收的字节数进行接收  
              if (HeadLength >= recvBytes1.Length)  
              {  
                    iBytesHead = mClientSocket.Receive(recvBytes1, recvBytes1.Length, 0);  
              }  
              else  
              {  
                    iBytesHead = mClientSocket.Receive(recvBytes1, HeadLength, 0);  
              }  
              //将接收到的字节数保存  
              recvBytes1.CopyTo(recvBytesHead, recvBytesHead.Length - HeadLength);  
              //减去已经接收到的字节数  
              HeadLength -= iBytesHead;  
          }  
          //接收消息体（消息体的长度存储在消息头的4至8索引位置的字节里）  
          byte[] bytes = new byte[4];  
          Array.Copy(recvBytesHead, 4, bytes, 0, 4);  
          int BodyLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0));  
          //存储消息体的所有字节数  
          byte[] recvBytesBody = new byte[BodyLength];  
          //如果当前需要接收的字节数大于0，则循环接收  
          while (BodyLength > 0)  
          {  
              byte[] recvBytes2 = new byte[BodyLength < 1024 ? BodyLength : 1024];  
              //将本次传输已经接收到的字节数置0  
              int iBytesBody = 0;  
              //如果当前需要接收的字节数大于缓存区大小，则按缓存区大小进行接收，相反则按剩余需要接收的字节数进行接收  
              if (BodyLength >= recvBytes2.Length)  
              {  
                    iBytesBody = mClientSocket.Receive(recvBytes2, recvBytes2.Length, 0);  
              }  
              else  
              {  
                    iBytesBody = mClientSocket.Receive(recvBytes2, BodyLength, 0);  
              }  
              //将接收到的字节数保存  
              recvBytes2.CopyTo(recvBytesBody, recvBytesBody.Length - BodyLength);  
              //减去已经接收到的字节数  
              BodyLength -= iBytesBody;  
          }  
          //一个消息包接收完毕，解析消息包  
          UnpackData(recvBytesHead,recvBytesBody);  


            //测试：服务端数据发送至客户端
            string[] str = {"服务端的数据","服务端的数据test1","服务端的数据test12"};  
            SendMessage(BuildDataPackage(31, 32, 33, 34,35, str),mClientSocket); 
      }  
  }  
  /// <summary>  
  /// 解析消息包  
  /// </summary>  
  /// <param name="Head">消息头</param>  
  /// <param name="Body">消息体</param>  
  public static void UnpackData(byte[] Head, byte[] Body)  
  {  
      byte[] bytes = new byte[4];  
      Array.Copy(Head, 0, bytes, 0, 4);  
      Debug.Log("接收到数据包中的校验码为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));  
  
      bytes = new byte[8];  
      Array.Copy(Head, 8, bytes, 0, 8);  
      Debug.Log("接收到数据包中的身份ID为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt64(bytes, 0)));  
  
      bytes = new byte[4];  
      Array.Copy(Head, 16, bytes, 0, 4);  
      Debug.Log("接收到数据包中的数据主命令为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));  
  
      bytes = new byte[4];  
      Array.Copy(Head, 20, bytes, 0, 4);  
      Debug.Log("接收到数据包中的数据子命令为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));  
  
      bytes = new byte[4];  
      Array.Copy(Head, 24, bytes, 0, 4);  
      Debug.Log("接收到数据包中的数据加密方式为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));  
  
      bytes = new byte[Body.Length];  
      for (int i = 0; i < Body.Length;)  
      {  
          byte[] _byte = new byte[4];  
          Array.Copy(Body, i, _byte, 0, 4);  
          i += 4;  
          int num = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_byte, 0));  
  
          _byte = new byte[num];  
          Array.Copy(Body, i, _byte, 0, num);  
          i += num;  
          Debug.Log("接收到数据包中的数据有：" + Encoding.UTF8.GetString(_byte, 0, _byte.Length));  
      }  


  } 


















    /// <summary>  
        /// 数据转换，网络发送需要两部分数据，一是数据长度，二是主体数据  
        /// </summary>  
        /// <param name="message"></param>  
        /// <returns></returns>  
        private static byte[] WriteMessage(byte[] message)  
        {  
            MemoryStream ms = null;  
            using (ms = new MemoryStream())  
            {  
                ms.Position = 0;  
                BinaryWriter writer = new BinaryWriter(ms);  
                ushort msglen = (ushort)message.Length;  
                writer.Write(msglen);  
                writer.Write(message);  
                writer.Flush();  
                return ms.ToArray();  
            }  
        } 
    /// <summary>  
        /// 接收指定客户端Socket的消息  
        /// </summary>  
        /// <param name="clientSocket"></param>  
        private static void RecieveMessage2(object clientSocket)  
        {  
            Socket mClientSocket = (Socket)clientSocket;  
            while (true)  
            {  
                try  
                {
                    byte[] result=new byte[8096];
                    int receiveNumber = mClientSocket.Receive(result);  
                    Debug.Log(string.Format("接收客户端{0}消息， 长度为{1}", mClientSocket.RemoteEndPoint.ToString(), receiveNumber));  
                    ByteBuffer buff = new ByteBuffer(result);  
                    //数据长度  
                    int len = buff.ReadShort();  
                    //数据内容  
                    string data = buff.ReadString();  
                    Debug.Log(string.Format("数据内容：{0}", data));  
                }  
                catch (Exception ex)  
                {  
                    Console.WriteLine(ex.Message);  
                    mClientSocket.Shutdown(SocketShutdown.Both);  
                    mClientSocket.Close();  
                    break;  
                }  
            }  
        }  


}
