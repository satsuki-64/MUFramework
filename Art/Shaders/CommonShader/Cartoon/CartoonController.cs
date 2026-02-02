using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CommonShader.Water
{
    public class CartoonController : MonoBehaviour
    {
        /// <summary>
        /// 水体的渐变纹理
        /// </summary>
        [Header("渐变纹理")]
        public Texture2D RampTexture01;
        
        [Header("渐变效果控制")]
        public Gradient waterGradient01;
       
        
        public int count;
        
        private void OnValidate()
        {
            // 参数一、二：材质的宽和高，单位为像素
            RampTexture01 = new Texture2D(128, 1);
            RampTexture01.wrapMode = TextureWrapMode.Clamp;
            
            count = RampTexture01.width * RampTexture01.height;
            
            Color[] colors1 = new Color[count];
            
            for (int i = 0; i < count; i++)
            {
                colors1[i] = waterGradient01.Evaluate((float)i/ count);
            }

            RampTexture01.SetPixels(colors1);
            RampTexture01.Apply();
            
            // 全局赋值，将 RampTexture01 纹理赋值到 _RampTexture01 上 
            Shader.SetGlobalTexture("_ShadowRampTex", RampTexture01);
        }
    }
}