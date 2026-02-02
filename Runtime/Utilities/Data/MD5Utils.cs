using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace MUFramework.Utilities
{
    public class MD5Utils
    {
        public static string GetMD5(string filePath)
        {
            // 将文件以流的形式打开
            using (FileStream file = new FileStream(filePath, FileMode.Open))
            {
                // 声明一个MD5对象， 用于生成MD5码
                MD5 md5 = new MD5CryptoServiceProvider();
                
                // 得到目标路径的文件对应的MD5码，16字节的数组
                byte[] md5Info = md5.ComputeHash(file);
                
                file.Close();                
                
                Debug.Log("MD5原生字节："+file.ToString());
                
                // 将其转化为32字节的字符串
                StringBuilder sb = new StringBuilder();
                
                // 将16个字节全部转成16进制后，拼接加入到字符串当中，减少MD5码的长度
                for (int i =0 ; i < md5Info.Length ; i++) 
                {
                    sb.Append(md5Info[i].ToString("x2"));
                }
                
                Debug.Log("MD5转码后：" + sb.ToString());
                return sb.ToString();
            }

            Debug.Log("创建文件流失败");
            return null;
        }
    }
}