using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MUFramework.Utilities;
using MUFramework.NetSystem;
using UnityEngine;
using UnityEngine.Events;

namespace MUFramework.DataSystem
{
    public sealed partial class AssetUpdate : SingletonMonoAuto<AssetUpdate>
    {
        /// <summary>
        /// 用于存储远端AB包信息的字典，之后和本地进行对比即可完成更新
        /// </summary>
        private Dictionary<string,ABInfo> remoteABInfo = new Dictionary<string,ABInfo>();
        
        /// <summary>
        /// 用于存储本地AB包信息的字典，之后和远程进行对比即可完成更新
        /// </summary>
        private Dictionary<string,ABInfo> localABInfo = new Dictionary<string,ABInfo>();
        
        /// <summary>
        /// 待下载的AB包的列表，存储AB包的名字
        /// </summary>
        private List<string> downLoadList = new List<string>();
        
        /// <summary>
        /// FTP下载失败时，AB包重新下载的次数
        /// </summary>
        public static readonly int RedownLoadMaxNum = 5;

        /// <summary>
        /// 用于检查当前AB包是否需要更新，使用本地AB包信息和远端AB包信息进行对比
        /// </summary>
        /// <param name="overCallBack">执行成功或者失败后的回调</param>
        /// <param name="updateInfoCallBack">进度更新</param>
        public async void CheckUpdate(UnityAction<bool> overCallBack, UnityAction<string> updateInfoCallBack)
        {
            // 0. 初始化
            remoteABInfo.Clear();
            localABInfo.Clear();
            downLoadList.Clear();
            
            // 1. 下载远端以及本地的 AB 包对比文件
            updateInfoCallBack("开始更新资源");
            string remoteInfo = await GetCompareFile.GetRemoteABCompareFile(updateInfoCallBack, remoteABInfo);
            string localInfo = await GetCompareFile.GetLocalABCompareFile(updateInfoCallBack, localABInfo);
            
            if (string.IsNullOrEmpty(remoteInfo) || string.IsNullOrEmpty(localInfo)) 
            {
                overCallBack(false);
            }
            else
            {
                if (localInfo == "DownloadAll") updateInfoCallBack("当前为第一次进入，并且在 StreamingAssets 中没有没有默认资源");
                
                // 2. 对比远端以及本地 AB 包信息，得到待下载列表
                CompareLocalAndRemoteAB(updateInfoCallBack);
            
                // 3. 根据待下载列表，下载 AB 包
                if (downLoadList != null && downLoadList.Count > 0) 
                {
                    bool isSucceedDownLoad = await DownLoadAllABFile(updateInfoCallBack);
                    if (isSucceedDownLoad)
                    {
                        // remoteInfo 是远端的 AB 包对比文件信息，将其覆盖为本地
                        File.WriteAllText(Application.persistentDataPath + "/ABCompareInfo.txt", remoteInfo);
                        updateInfoCallBack("更新本地AB包对比文件为最新");
                        overCallBack(true);
                    }
                    else
                    {
                        overCallBack(false);
                    }
                }
                else
                {
                    overCallBack(false);
                }
            }
        }
        
        /// <summary>
        /// 对比远端和本地的 AB 包信息，并将需要下载的 AB 包加入到待下载列表中，将本地有、远端没有的资源从本地删除（仅删除可读写文件夹当中的）
        /// </summary>
        /// <param name="updateInfoCallBack"></param>
        private void CompareLocalAndRemoteAB(UnityAction<string> updateInfoCallBack)
        {
            if (remoteABInfo != null && localABInfo != null) 
            {
                foreach (string abName in remoteABInfo.Keys)
                {
                    // 1. 如果当前本地的 AB 包当中不存在，但是远端存在的资源
                    if (!localABInfo.ContainsKey(abName))
                    {
                        // 结果：将其加入到下载列表
                        // 注意：如果当前是第一次进入，并且在 StreamingAssets 中没有没有默认资源，则 localABInfo 中没有内容，因此远端的所有 AB 包被下载
                        downLoadList.Add(abName);
                    }
                    else // 2. 如果在本地和远端存在都存在的资源，则判断 MD5 码是否一致，进而判断文件是否改变
                    {
                        // 2. 如果当前本地的资源和远端的MD5不一样
                        if (localABInfo[abName].md5 != remoteABInfo[abName].md5)
                        {
                            // 结果：将内容不一致的资源也加入下载列表
                            downLoadList.Add(abName);
                        }
                        // 2.2 如果当前本地的资源和远端的MD5一样
                        else
                        {
                            // 结果：MD5 码相同，表示是同一个资源，因此不需要更新
                        }

                        // 在本地和远端存在都存在的资源，不管是否需要更新，都需要将其移除
                        // 在经过这一步移除之后，剩下的内容肯定是本地有、远端没有的内容，这个字典可以用于在最后将这些本地剩下的信息进行删除
                        localABInfo.Remove(abName);
                    }
                }
                updateInfoCallBack("对比完成");
            
                // 3 对比完成之后，删除没用的内容
                updateInfoCallBack("删除无用的AB包文件");
                foreach (string abName in localABInfo.Keys)
                {
                    // 如果本地的可读写文件夹中有远端不存在的资源，则需要将其删除
                    // 默认资源中的信息不能进行删除
                    if (File.Exists(Application.persistentDataPath + "/" + abName))
                    {
                        File.Delete(Application.persistentDataPath + "/"  + abName);
                    }
                }
            }
            else
            {
                Debug.LogWarning("远程AB包字典与本地AB包字典为空！请检查");
            }
        }
        
        /// <summary>
        /// 下载 downLoadList 当中存储的所有 AB 包文件
        /// </summary>
        private async Task<bool> DownLoadAllABFile(UnityAction<string> updateProcess)
        {
            // 本地存储的路径
            string localPath = "";
            
            // 这一次下载要下载的资源数量
            int downLoadMaxNum = downLoadList.Count;
            
            // 下载成功的资源数
            int downLoadOverNum = 0;
            
            int redownLoadOverNum = RedownLoadMaxNum;
            
            // 是否下载成功
            bool isOver = false;
            
            // 这一次下载需要多少个资源
            List<string> succeedLoadedList = new List<string>();

            // 在网络错误时，进行最多n次的重新下载
            while (downLoadList.Count > 0 && redownLoadOverNum > 0) 
            {
                for (int i = 0; i < downLoadList.Count; i++) 
                {
                    isOver = false;
                    localPath = Path.Combine(Application.persistentDataPath, downLoadList[i]);
                    
                    await Task.Run(() =>
                    {
                        // 根据当前的待下载 AB 包列表，下载到persistentDataPath路径
                        isOver = FtpComponent.DownLoadABFile(downLoadList[i], localPath);
                    });
                
                    // 如果当前资源下载成功，将其加入到 succeedLoadedList 列表当中
                    if (isOver)
                    {
                        // 2. 要知道现在下载了多少，直到结束
                        updateProcess(++downLoadOverNum + "/" + downLoadMaxNum); 
                        succeedLoadedList.Add(downLoadList[i]);
                    }
                }
                
                // 把下载成功的文件名，从待下载列表中移除
                for (int i = 0; i < succeedLoadedList.Count; i++)
                {
                    downLoadList.Remove(succeedLoadedList[i]);
                }

                --redownLoadOverNum;
            }

            return downLoadList.Count == 0;
        }
    }
}