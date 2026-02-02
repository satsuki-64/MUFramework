using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MUFramework.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MUFramework.Editor
{
    public class ABTools : EditorWindow
    {
        private int nowSelectedIndex = 0;
        private string[] targetStrings = new string[]{"PC","IOS","Android"};

        public static string serverIP = "ftp://127.0.0.1";
        
        [MenuItem("MUFramework/AB包工具/打开工具窗口")]
        private static void OpenWindow()
        {
            // 获取一个编辑器窗口对象
            ABTools window = EditorWindow.GetWindowWithRect(typeof(ABTools), new Rect(0, 0, 350, 220)) as ABTools;
            window.Show();
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 150, 15), "平台选择");
            
            // 页签显示，从数组中去除字符，返回索引，进行显示
            nowSelectedIndex = GUI.Toolbar(new Rect(10, 30, 305, 20), nowSelectedIndex, targetStrings);
            
            // 资源服务器IP地址设置
            GUI.Label(new Rect(10, 60, 150, 15), "服务器IP地址");
            serverIP = GUI.TextField(new Rect(10, 80, 150, 20), serverIP);
            
            // 创建对比文件按钮
            if (GUI.Button(new Rect(10, 110, 100, 40), "创建对比文件"))
            {
                CreateABCompareFile();
            }
            // 保存默认资源到StreamingAssets的按钮
            if (GUI.Button(new Rect(115, 110, 200, 40), "保存默认资源到StreamingAssets"))
            {
                MoveABToStreamAssets();
            }
            // 上传AB包到远端服务器
            if (GUI.Button(new Rect(10,160,305,40),"上传AB包和对比文件"))
            {
                UploadAllABFiles();
            }
        }
        
        public static string Path = "/SubProject/ABProject/AB/";
        
        /// <summary>
        /// 创建 AB 包对比文件
        /// </summary>
        private void CreateABCompareFile()
        {
            // 根据选择的平台，获取对应的AB包文件夹的文件夹信息，用于生成AB包对比文件
            DirectoryInfo directory = Directory.CreateDirectory(Application.dataPath + Path + targetStrings[nowSelectedIndex]);
            
            // 获取该目录下的所有文件信息
            FileInfo[] fileInfos = directory.GetFiles();

            // 用于存储信息的字符串
            string abCompareInfo = "";
            
            // 输出所有文件的信息
            foreach (FileInfo info in fileInfos)
            {
                // 没有后缀的认为是AB包，只想要AB包的信息
                if (string.IsNullOrEmpty(info.Extension))
                {
                    Debug.Log(info.Name);

                    // 拼接一个AB包的信息
                    abCompareInfo += info.Name + " " + info.Length + " " + MD5Utils.GetMD5(info.FullName);
                    
                    // 用一个分隔符分开不同文件的信息
                    abCompareInfo += "\n";
                } 
            }
            
            // 将最后一个 "|" 去除
            abCompareInfo.Substring(0, abCompareInfo.Length - 1);
            
            // 将拼接好的AB包资源信息存储到文件本地
            File.WriteAllText(Application.dataPath + Path + targetStrings[nowSelectedIndex] + "/ABCompareInfo.txt", abCompareInfo);
            
            // 刷新编辑器
            AssetDatabase.Refresh();
            
            Debug.Log("AB包对比文件生成成功");
        }
        
        /// <summary>
        /// 将选中资源移动到 StreamingAssets 当中
        /// </summary>
        private void MoveABToStreamAssets()
        {
            // 这里因为引用了UnityEngine，因此访问的是Unity当中的object
            UnityEngine.Object[] selectedAssets = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
            if (selectedAssets.Length == 0)
            {
                return;
            }

            // 用于拼接本地默认AB包资源信息的字符串
            string abCompareInfo = "";
            
            foreach (Object asset in selectedAssets)
            {
                // 得到选中资源的路径
                string assetPath = AssetDatabase.GetAssetPath(asset);
                
                // 因为是要把最后一个斜杠后背的字符串截取出来，因此这里需要加1
                string fileName = assetPath.Substring(assetPath.LastIndexOf("/"));
                string targetPath = Application.streamingAssetsPath + fileName;
                AssetDatabase.CopyAsset(assetPath,targetPath);
                
                // 获取拷贝到StreamingAssets文件夹中的文件的详细信息
                FileInfo fileInfo = new FileInfo(Application.streamingAssetsPath + fileName);
                
                // 如果有后缀，证明不是AB包，则跳过
                if (fileInfo.Extension is not "")
                {
                    continue;
                }

                // 拼接信息
                abCompareInfo += fileInfo.Name + " " + fileInfo.Length + " " + MD5Utils.GetMD5(fileInfo.FullName);
                abCompareInfo += "\n";
            }
            
            // 去除掉最后一行额外的换行符
            abCompareInfo = abCompareInfo.Substring(0, abCompareInfo.LastIndexOf("\n"));
            
            // 将本地的资源的AB对比文件存储到本地
            File.WriteAllText(Application.streamingAssetsPath + "ABCompareInfo.txt",abCompareInfo);
            
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// 上传AB包文件到服务器
        /// </summary>
        private void UploadAllABFiles()
        {
            DirectoryInfo directory = Directory.CreateDirectory(Application.dataPath + "/SubProject/ABProject/AB/" +
                                                                targetStrings[nowSelectedIndex] + "/");
            
            // 获取该目录下的所有文件信息
            FileInfo[] fileInfos = directory.GetFiles();
            
            // 输出所有文件的信息
            foreach (FileInfo info in fileInfos)
            {
                // 没有后缀的认为是 AB 包，而只有 AB 包需要上传
                // 或者文件的后缀是 "txt" 的，也需要上传
                if (string.IsNullOrEmpty(info.Extension) || info.Extension == ".txt")
                {
                    // 上传文件到FTP服务器当中
                    FtpUploadFile(info.FullName,info.Name);
                } 
            }
        }

        /// <summary>
        /// 异步上传文件到FTP服务器
        /// </summary>
        /// <param name="filePath">上传文件的路径</param>
        private async void FtpUploadFile(string filePath, string fileName)
        {
            // 将当前上传的相关逻辑放到一个异步的 Task 当中，并且直接运行（Task.Run）
            await Task.Run(() =>
            {
                try
                {
                    // 1. 创建一个FTP连接用于上传
                    // 连接到本地的FTP服务器
                    FtpWebRequest request =
                        WebRequest.Create(new Uri(serverIP + "/AB/" + targetStrings[nowSelectedIndex] + "/" + fileName))
                            as FtpWebRequest;

                    // 2. 设置一个通信凭证，这样才能上传
                    NetworkCredential n = new NetworkCredential("Wan", "123456");
                    request.Credentials = n;

                    // 3. 其他设置（包含设置代理为空、请求完毕后是否关闭控制连接、操作命令-上传、指定传输的类型（二进制））
                    request.Proxy = null;
                    // 关闭是否存活
                    request.KeepAlive = false;
                    // 设置当前WebRequest的上传方式为FTP服务器的上传文件
                    request.Method = WebRequestMethods.Ftp.UploadFile;
                    // 二进制传输
                    request.UseBinary = true;

                    // 4. 上传文件
                    // FTP的流对象
                    // 这是一个用于上传的流，将这个流当中的数据上传到FTP服务器当中
                    Stream upLoadStream = request.GetRequestStream();

                    // 读取本地的文件流，将其写入上传的文件流对象
                    using (FileStream file = File.OpenRead(filePath))
                    {
                        // 分多次读取，每一次读取2048B，即2KB
                        byte[] bytes = new byte[2048];

                        // 这里的file.Read就是从文件流当中读取2048B，即2KB的数据，将其读取到file文件流当中
                        // 返回值代表这一次读取了多少字节，假设当前文件只有1KB，则返回值就是1024
                        int contentLength = file.Read(bytes, 0, bytes.Length);

                        // 如果读取出的内容长度contentLength不等于0，表示这一次有内容读取，因此要将其写入到上传流（upLoadStream）当中
                        // 循环上传文件中的数据，直到contentLength=0，表示所有内容都读取完了
                        while (contentLength != 0)
                        {
                            // 写入到上传流当中
                            // 读的长度是 contentLength，所以写的长度也是 contentLength
                            upLoadStream.Write(bytes, 0, contentLength);

                            // 写完再读
                            contentLength = file.Read(bytes, 0, bytes.Length);
                        }

                        // 循环完毕之后，证明上传结束
                        file.Close();
                        upLoadStream.Close();

                        Debug.Log(fileName + "上传完成");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("上传失败：" + e.Message);
                }
            });
        }
    }
}