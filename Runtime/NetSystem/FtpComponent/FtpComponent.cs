using System;
using System.IO;
using System.Net;
using UnityEngine;

namespace MUFramework.NetSystem
{
    public static class FtpComponent
    {
        public static readonly string FtpIP = "127.0.0.1";
        
        public static bool DownLoadABFile(string fileName, string localPath)
        {
            try
            {
                // 0. 根据当前的平台，从FTP服务器下载对应的内容
                string pInfo = "AB/" + 
                #if UNITY_IOS
                    "IOS";
                #elif UNITY_ANDROID
                    "Android";
                #else
                    "PC";
                #endif
                
                // 1. 创建一个FTP连接用于下载
                FtpWebRequest request =
                    WebRequest.Create(new Uri($"ftp://{FtpIP}/" + pInfo + "/" + fileName)) as FtpWebRequest;

                // 2. 设置一个通信凭证，这样才能下载
                // 如果有匿名账号，则可以不设置凭证，但是不建议使用匿名账号
                NetworkCredential n = new NetworkCredential("Wan", "123456");
                request.Credentials = n;

                // 3. 其他设置（包含设置代理为空、请求完毕后是否关闭控制连接、操作命令-上传、指定传输的类型（二进制））
                request.Proxy = null;
                // 关闭是否存活
                request.KeepAlive = false;
                // 设置当前WebRequest的上传方式为FTP服务器的下载文件
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                // 二进制传输
                request.UseBinary = true;

                // 4. 上传文件
                // FTP的流对象
                // 这是一个用于上传的流，将这个流当中的数据上传到FTP服务器当中
                FtpWebResponse response = request.GetResponse() as FtpWebResponse;
                // 获得当前response中的文件流
                Stream downloadStream = response.GetResponseStream();

                using (FileStream file = File.Create(localPath))
                {
                    // 分多次下载，每一次读取2048B，即2KB
                    byte[] bytes = new byte[2048];

                    // 现在应当是从下载流当中读取文件
                    int contentLength = downloadStream.Read(bytes, 0, bytes.Length);

                    // 循环下载数据
                    while (contentLength != 0)
                    {
                        // 写入到本地文件流当中
                        // 读的长度是 contentLength，所以写的长度也是 contentLength
                        file.Write(bytes, 0, contentLength);

                        // 写完再读
                        contentLength = downloadStream.Read(bytes, 0, bytes.Length);
                    }

                    // 循环完毕之后，证明上传结束
                    file.Close();
                    downloadStream.Close();
                }
                
                Debug.Log(fileName + "下载成功");
                return true;
            }
            catch (Exception e)
            {
                Debug.Log("下载失败：" + e.Message); 
                return false;
            }
        }
        
        /// <summary>
        /// 将 FTP 服务器上的文件下载到本地指定的文件夹
        /// </summary>
        /// <param name="filePath">FTP服务器上待下载的目标</param>
        /// <param name="localPath">本地路径</param>
        /// <returns></returns>
        public static bool DownLoadFile(string filePath, string localPath)
        {
            try
            {
                // 1. 创建一个FTP连接用于下载
                FtpWebRequest request =
                    WebRequest.Create(new Uri($"ftp://{FtpIP}/" + filePath)) as FtpWebRequest;

                // 2. 设置一个通信凭证，这样才能下载
                // 如果有匿名账号，则可以不设置凭证，但是不建议使用匿名账号
                NetworkCredential n = new NetworkCredential("Wan", "123456");
                request.Credentials = n;

                // 3. 其他设置（包含设置代理为空、请求完毕后是否关闭控制连接、操作命令-上传、指定传输的类型（二进制））
                request.Proxy = null;
                // 关闭是否存活
                request.KeepAlive = false;
                // 设置当前WebRequest的上传方式为FTP服务器的下载文件
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                // 二进制传输
                request.UseBinary = true;

                // 4. 上传文件
                // FTP的流对象
                // 这是一个用于上传的流，将这个流当中的数据上传到FTP服务器当中
                FtpWebResponse response = request.GetResponse() as FtpWebResponse;
                // 获得当前response中的文件流
                Stream downloadStream = response.GetResponseStream();

                using (FileStream file = File.Create(localPath))
                {
                    // 分多次下载，每一次读取2048B，即2KB
                    byte[] bytes = new byte[2048];

                    // 现在应当是从下载流当中读取文件
                    int contentLength = downloadStream.Read(bytes, 0, bytes.Length);

                    // 循环下载数据
                    while (contentLength != 0)
                    {
                        // 写入到本地文件流当中
                        // 读的长度是 contentLength，所以写的长度也是 contentLength
                        file.Write(bytes, 0, contentLength);

                        // 写完再读
                        contentLength = downloadStream.Read(bytes, 0, bytes.Length);
                    }

                    // 循环完毕之后，证明上传结束
                    file.Close();
                    downloadStream.Close();
                }
                
                Debug.Log("下载成功");
                return true;
            }
            catch (Exception e)
            {
                Debug.Log("下载失败：" + e.Message); 
                return false;
            }
        }
    }
}