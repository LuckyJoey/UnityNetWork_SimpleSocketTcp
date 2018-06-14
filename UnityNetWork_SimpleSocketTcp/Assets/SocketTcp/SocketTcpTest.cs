using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocketTcpTest : MonoBehaviour {

    private string Ip = "127.0.0.1";  
    private int Port = 5690;  
    void Start()  
    {  
        SocketTCPServer.init(Ip, Port);           //开启并初始化服务器  
        SocketTCPClient.CreateInstance(Ip, Port); //客户端连接服务器  
    }  
    void Update()  
    {  
        if (Input.GetKeyDown(KeyCode.Space))  
        {  
            string[] str = {"测试字符串a","abc","def"};  
            SocketTCPClient.SendMessage(SocketTCPClient.BuildDataPackage(1, 2, 3, 4,5, str));  
            //string[] str2 = { "我是与1同时发送的测试字符串2，请注意我是否与其他信息粘包", "test2", "test22" };  
            //SocketTCPClient.SendMessage(SocketTCPClient.BuildDataPackage(1, 6, 7, 8, 9, str2));  
        }  
    }  
    void OnApplicationQuit()  
    {  
        SocketTCPClient.Close();  
        SocketTCPServer.Close();  
    }  
}
