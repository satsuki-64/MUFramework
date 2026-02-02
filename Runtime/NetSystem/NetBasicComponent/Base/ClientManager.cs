using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MUFramework.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MUFramework.NetSystem
{
    public class ClientManager : SingletonMonoAuto<ClientManager>
    {
        public GameObject Camera;
        
        #region 客户端相关设置

        public int PlayerID = 0;
        
        /// <summary>
        /// 当前客户端的 Socket 
        /// </summary>
        private Socket socket;

        /// <summary>
        /// 用于存放发送消息的队列
        /// </summary>
        private Queue<BaseMsg> sendMsgQueue = new Queue<BaseMsg>();
        
        /// <summary>
        /// 用于接收消息的队列
        /// </summary>
        private Queue<BaseMsg> receiveMsgQueue = new Queue<BaseMsg>();
        
        /// <summary>
        /// 用于接收消息的容器
        /// </summary>
        private byte[] receiveBytes = new byte[1024*1024];
        
        /// <summary>
        /// 分包时，用于缓存字节数组和字节数组长度的容器
        /// </summary>
        private byte[] cacheBytes = new byte[1024*1024];
        private int cacheNum = 0;

        /// <summary>
        /// 返回收到的字节数
        /// </summary>
        private int receiveNumber;
        
        /// <summary>
        /// 当前是否连接中
        /// </summary>
        private bool isConnected = false;

        /// <summary>
        /// 向服务器发送心跳消息的间隔时间
        /// </summary>
        private const int SEND_HEART_MSG_TIME = 2;
        
        /// <summary>
        /// 心跳消息
        /// </summary>
        private HeartMsg heartMsg;
        private PlayerMsg playerMsg;
        
        private bool isFirstRun = true;

        #endregion

        #region 常用方法

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Connect(string ip,int port)
        {
            Close();
            
            if (socket == null)
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

            // 连接服务端
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            
            try
            {
                socket.Connect(ipPoint);
                isConnected = true;

                Task.Run(SendMsg);
                Task.Run(ReceiveMsg);
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10061)
                {
                    Log.Warning("服务器拒绝连接", LogModule.Network);
                }
                else
                {
                    Log.Warning("连接失败：" + e.ErrorCode + e.Message, LogModule.Network);
                }
            }
        }

        /// <summary>
        /// 发送消息，将其压入待发送消息列表
        /// </summary>
        /// <param name="info"></param>
        public void Send(BaseMsg info)
        {
            if (sendMsgQueue != null && info != null)
            {
                sendMsgQueue.Enqueue(info);   
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        private void SendMsg()
        {
            while (isConnected)
            {
                if (sendMsgQueue.Count > 0 && socket != null)
                {
                    socket.Send(sendMsgQueue.Dequeue().Writing());
                }
            }
        }
        
        /// <summary>
        /// 发送心跳消息
        /// </summary>
        private void SendHeartMsg()
        {
            if (isConnected)
            {
                if (heartMsg == null)
                {
                    heartMsg = new HeartMsg(PlayerID);
                }

                if (playerMsg == null)
                {
                    playerMsg = new PlayerMsg(PlayerID);
                    playerMsg.PlayerData = new PlayerData();
                    playerMsg.PlayerData.position = new Vector3(Random.Range(0, 100), Random.Range(0, 100), Random.Range(-110, 100));
                }

                if (playerMsg !=null)
                {
                    playerMsg.PlayerData.position = new Vector3(Random.Range(0, 100), Random.Range(0, 100), Random.Range(-110, 100));
                }

                Send(heartMsg);    
                Send(playerMsg);  
            }
        }
        
        /// <summary>
        /// 关闭连接，并主动发送一条断开连接的消息给服务端
        /// </summary>
        public void Close()
        {
            if (socket != null)
            {
                Log.Debug("客户端主动断开连接", LogModule.Network);
                QuitMsg msg = new QuitMsg(PlayerID);
                socket.Send(msg.Writing());
                socket.Shutdown(SocketShutdown.Both);
                socket.Disconnect(false);
                socket.Close();
                socket = null;
                isConnected = false;
            }
        }
        
        #endregion

        #region 消息处理

        private void ReceiveMsg()
        {
            while (isConnected)
            {
                // 0. 如果当前 Socket 当中有可读字节
                if (socket.Available > 0)
                {
                    // 将接收到的字节放入到 receiveBytes 当中，receiveNumber 代表接收到的字节数
                    receiveNumber = socket.Receive(receiveBytes);
                    
                    // 1. 首先解析消息的 ID
                    // 使用字节数组的前四个字节，解析 ID
                    int msgID = BitConverter.ToInt32(receiveBytes, 0);
                    BaseMsg baseMsg = null;
                    
                    // 2. 根据解析的消息ID，选择不同的 Msg 处理方式
                    switch (msgID)
                    {
                        case 999:
                            ConnectMsg connectMsg = new ConnectMsg(PlayerID);
                            connectMsg.Reading(receiveBytes);
                            baseMsg = connectMsg;
                            PlayerID = connectMsg.GetPlayerID();
                            break;
                        
                        case 1001:
                            QuitMsg quitMsg = new QuitMsg(PlayerID);
                            quitMsg.Reading(receiveBytes);
                            baseMsg = quitMsg;
                            Close();
                            break;
                        case 1002:
                            PlayerMsg playerMsg = new PlayerMsg(PlayerID);
                            playerMsg.Reading(receiveBytes);
                            baseMsg = playerMsg;
                            Camera.transform.position = playerMsg.PlayerData.position;
                            break;
                        default:
                            Debug.LogWarning("无法解析出消息类型！");
                            break;
                    }

                    // 如果消息为空，证明是不知道的类型的消息，没有解析出数据
                    if (baseMsg == null)
                    {
                        continue;
                    }

                    // 3. 将收到的消息放入公共容器
                    receiveMsgQueue.Enqueue(baseMsg);
                }
            }
        }

        #endregion

        #region 其他

        public void Run()
        {
            Connect("127.0.0.1", 8080);

            if (isFirstRun)
            {
                // 客户端定时循环发送心跳消息
                InvokeRepeating("SendHeartMsg", 0, SEND_HEART_MSG_TIME);
                isFirstRun = false;
            }
        }

        private void Update()
        {
            // 连接服务器
            if (Input.GetKeyDown(KeyCode.E) && !isConnected)
            {
                Run();
            }

            // 断开连接
            if (Input.GetKeyDown(KeyCode.Z) && isConnected)
            {
                Close();
            }
            
            // 如果当前接收线程得到了数据并存放在了接收队列当中，则将其出队列、输出
            if (isConnected && receiveMsgQueue.Count > 0)
            {
                Log.Debug("接收到服务器的消息：" + receiveMsgQueue.Dequeue());
            }
        }
        
        private void OnDestroy()
        {
            if (isConnected)
            {
                Close();                
            }
        }

        #endregion
    }
}