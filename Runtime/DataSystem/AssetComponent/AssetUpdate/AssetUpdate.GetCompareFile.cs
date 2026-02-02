using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MUFramework.NetSystem;
using MUFramework.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace MUFramework.DataSystem
{
    public sealed partial class AssetUpdate : SingletonMonoAuto<AssetUpdate>
    {
        public static class GetCompareFile
        {
            

            /// <summary>
            /// 从远端下载 ABCompareInfo 文件，然后将其暂时存储在 ABCompareInfo_temp 中
            /// </summary>
            /// <param name="overCallBack">下载完成 AB 包对比文件后执行的回调</param>
            public static async Task<string> GetRemoteABCompareFile(UnityAction<string> updateCallback,
                Dictionary<string, ABInfo> remoteABDic)
            {
                string info = "";
                updateCallback("开始下载远程资源对比文件");
                
                // 对于 persistentDataPath 的File加载，不需要额外的处理
                string ABCompareInfoPath = Application.persistentDataPath + "/ABCompareInfo_temp.txt";
                int redownloadCount = AssetUpdate.RedownLoadMaxNum;
                
                // 1. 从资源服务器下载资源对比文件
                // 用FTP的相关API进行下载，HTTP服务器的话可以使用WWW或者UnityWebRequest下载
                bool isOver = false;
                
                while (!isOver && redownloadCount > 0)
                {
                    await Task.Run(() =>
                    {
                        // 将远端的 ABCompareInfo.txt 下载到本地的 ABCompareInfo_temp.txt 当中
                        isOver = FtpComponent.DownLoadABFile("ABCompareInfo.txt", ABCompareInfoPath); 
                    });
                    --redownloadCount;
                }
                
                Debug.Log(ABCompareInfoPath);
                
                // 3. 获取远端的资源对比文件中的字符串信息，在本地进行拆分
                if (isOver)
                {
                    // 读取 ABCompareInfo_temp.txt 并拆分 AB 包信息
                    info = File.ReadAllText(Application.persistentDataPath + "/ABCompareInfo_Temp.txt");
                    ABInfo.ABContentSplit(info, remoteABDic);
                }
                
                return info;
            }

            /// <summary>
            /// 加载本地的资源对比文件，将其解析出来
            /// </summary>
            public static async Task<string> GetLocalABCompareFile(UnityAction<string> updateCallBack, Dictionary<string, ABInfo> localABDic)
            {
                string info = "";
                string filePath = "";
                
                // 加载本地的AB对比文件流程分为三个分支
                // 1. 首先检查 persistentDataPath 有没有
                // 如果可读可写的文件夹 persistentDataPath 当中存在对比文件，则说明之前已经下载更新过了
                if (File.Exists(Application.persistentDataPath + "/ABCompareInfo.txt"))
                {
                    // 使用 UnityWebRequest 加载文件时，需要在前面加上"file://"前缀
                    filePath = "file:///" + Application.persistentDataPath + "/ABCompareInfo.txt";
                    
                    info = await LoadLocalABCompareFileAndSplit(filePath, localABDic);
                    return info;
                }
                // 2. 如果在 persistentDataPath 中没有（第一次进入游戏时才会发生）
                // 如果仅可读的文件夹 streamingAssetsPath 中有，则表示默认资源文件可以不用下载，直接从本地获取
                else if (File.Exists(Application.streamingAssetsPath + "/ABCompareInfo.txt"))
                {
                    filePath = Path.Combine
                    (
#if UNITY_ANDROID
                        // 使用安卓读取 Application.streamingAssetsPath 时，默认是有前缀的
                        Application.streamingAssetsPath
#else
                        "file:///" + Application.streamingAssetsPath
#endif
                        ,"ABCompareInfo.txt"
                    );
                    
                    info = await LoadLocalABCompareFileAndSplit(filePath, localABDic);
                    return info;
                }
                // 3. 如果以上两种都不进入，则表示当前为第一次进入，并且在 streamingAssetsPath 没有默认资源
                else
                {
                    // 不执行操作
                    return "DownloadAll";
                }
            }

            /// <summary>
            /// 根据 GetLocalABCompareFileInfo 得到 当前 AB 包对比文件的文件路径，然后对其进行加载并拆分，将其存储到 localABInfo 字典中
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="overCallBack"></param>
            /// <returns></returns>
            private static async Task<string> LoadLocalABCompareFileAndSplit(string filePath,
                Dictionary<string, ABInfo> localABDic)

            {
                // 通过 UnityWebRequest 来加载本地文件
                UnityWebRequest req = UnityWebRequest.Get(filePath);
                var operation = req.SendWebRequest();
                
                string info = req.downloadHandler.text;
                if (string.IsNullOrEmpty(info))
                {
                    Debug.LogWarning("本地AB包对比文件加载失败！");
                }

                if (req.result == UnityWebRequest.Result.Success)
                {
                    // 解析本地的 AB 包对比文件，将 AB 包信息存储到字典当中
                    ABInfo.ABContentSplit(info, localABDic);
                }

                return info;
            }
        }   
    }
}