using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

namespace MUFramework.NetSystem
{
    public class Client : MonoBehaviour
    {
        public GameObject camera;
        private Socket socket;
        private bool isConnected = false;
        private Coroutine receiveCoroutine;

        private void Update()
        {
            // 连接服务器
            if (Input.GetKeyDown(KeyCode.E) && !isConnected)
            {
                ClientTest();
            }

            // 断开连接
            if (Input.GetKeyDown(KeyCode.Z) && isConnected)
            {
                Disconnect();
            }
        }

        private void ClientTest()
        {
            // 1. 创建套接字 socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);

            // 2. 与服务端相连    
            try
            {
                socket.Connect(ipPoint);
                isConnected = true;
                Debug.Log("连接服务器成功");

                // 启动持续接收数据的协程
                receiveCoroutine = StartCoroutine(ReceiveDataContinuous());
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10051)
                {
                    Debug.Log("服务器拒绝连接");
                }
                else
                {
                    Debug.Log("连接服务器失败：" + e.ErrorCode);
                }
            }
        }

        public bool isTrue = false;
        private IEnumerator ReceiveDataContinuous()
        {
            byte[] receiveBytes = new byte[1024];
            
            while (isConnected)
            {
                // 检查Socket是否有数据可读
                if (socket.Available > 0)
                {
                    try
                    {
                        int receiveNum = socket.Receive(receiveBytes);
                        if (receiveNum > 0)
                        {
                            if (isTrue == false)
                            {
                                isTrue = true;
                                continue;
                            }

                            // 反序列化服务器发来的三个int数值
                            Vector3 receivedData = DeserializeIntArray(receiveBytes, receiveNum);
                            if (receivedData != null)
                            {
                                Debug.Log($"收到服务器数据: {receivedData.x}, {receivedData.y}, {receivedData.z}");
                                camera.transform.position = receivedData;
                            }
                            else
                            {
                                Debug.LogWarning("接收到的数据格式不正确");
                            }
                        }
                        else
                        {
                            // 接收数据为0表示连接已关闭
                            Debug.Log("服务器断开连接");
                            Disconnect();
                            yield break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("接收数据错误: " + ex.Message);
                        Disconnect();
                        yield break;
                    }
                }
                
                // 等待下一帧继续检查，避免阻塞主线程
                yield return null;
            }
        }

        /// <summary>
        /// 反序列化字节数组为int数组
        /// </summary>
        /// <param name="data">接收到的字节数组</param>
        /// <param name="dataLength">有效数据长度</param>
        /// <returns>反序列化后的int数组</returns>
        private Vector3 DeserializeIntArray(byte[] data, int dataLength)
        {
            try
            {
                PlayerData playerData = new PlayerData();
                playerData.Reading(data);
                return playerData.position;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("反序列化失败: " + ex.Message);
                return Vector3.zero;
            }
        }

        private void Disconnect()
        {
            if (isConnected)
            {
                isConnected = false;
                
                // 停止接收协程
                if (receiveCoroutine != null)
                {
                    StopCoroutine(receiveCoroutine);
                    receiveCoroutine = null;
                }

                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    Debug.Log("连接已断开");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("断开连接时出错: " + ex.Message);
                }
            }
        }

        private void OnDestroy()
        {
            // 确保对象销毁时断开连接
            Disconnect();
        }
    }
}