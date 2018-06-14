using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.UI;

public class SocketClient : MonoBehaviour
{


    public static SocketClient instance;
    public int portNo = 9999;
    private TcpClient _client;
    byte[] data;
    public string url = "127.0.0.1";

    public void Awake()
    {
        instance = this;
    }

    public string idtext = "";
    bool connect = false;  //只允许登录一次  
    public void login()
    {
        if (idtext != "" && !connect)
        {
            connect = true;
            this._client = new TcpClient();
            this._client.Connect(url, portNo);
            data = new byte[this._client.ReceiveBufferSize];
            SendMessage(idtext);
            this._client.GetStream().BeginRead(this.data, 0, System.Convert.ToInt32(this._client.ReceiveBufferSize),
            ReceiveMessage, null);

        }
        else
        {
            Debug.Log("connect failed or connected");
        }
    }
    public void ReceiveMessage(IAsyncResult ar)
    {
        try
        {
            int bytesRead;
            bytesRead = this._client.GetStream().EndRead(ar);
            if (bytesRead < 1)
            {
                return;
            }
            else
            {
                Debug.Log("Client_ReceiveMessage:"+System.Text.Encoding.UTF8.GetString(data, 0, bytesRead));
                string message = System.Text.Encoding.UTF8.GetString(data, 0, bytesRead);
            }
        }
        catch (Exception ex)
        {

        }
    }

    public void SendMessage(string message)
    {
        if (connect)
        {
            try
            {
                NetworkStream ns = this._client.GetStream();
                byte[] dataMsg = System.Text.Encoding.UTF8.GetBytes(message);
                ns.Write(dataMsg, 0, dataMsg.Length);
                Debug.Log("Client_SendMessage:" + message);
                ns.Flush();
            }
            catch (Exception ex)
            {
                Debug.Log("Excep:" + ex.Message);
            }
        }
        else
        {
            Debug.Log("not connect");
        }
    }
    int i = 0;
	private void OnGUI()
	{
        if(GUI.Button(new Rect(150, 40, 100, 40), "Login"))
        {
            login();
        }
        if (GUI.Button(new Rect(150, 120, 100, 40), "SendMessage"))
        {
            SendMessage("_abc_"+i);
            i++;
        }

	}

}