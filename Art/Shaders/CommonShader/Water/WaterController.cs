using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CommonShader.Water
{
    public class WaterController : MonoBehaviour
    {
        /// <summary>
        /// 水体的渐变纹理
        /// </summary>
        [Header("水体的渐变纹理")]
        public Texture2D RampTexture01;
        public Texture2D RampTexture02;
        
        [Header("水体渐变效果控制")]
        public Gradient waterGradient01;
        public Gradient waterGradient02;
        
        public int count;
        
        private void OnValidate()
        {
            // 参数一、二：材质的宽和高，单位为像素
            RampTexture01 = new Texture2D(512, 1);
            RampTexture01.wrapMode = TextureWrapMode.Clamp;
            
            count = RampTexture01.width * RampTexture01.height;
            
            Color[] colors1 = new Color[count];
            
            for (int i = 0; i < 512; i++)
            {
                colors1[i] = waterGradient01.Evaluate((float)i/ 511);
            }

            RampTexture01.SetPixels(colors1);
            RampTexture01.Apply();
            
            RampTexture02 = new Texture2D(512, 1);
            RampTexture02.wrapMode = TextureWrapMode.Clamp;
            RampTexture02.filterMode = FilterMode.Bilinear;
            
            Color[] colors2 = new Color[count];
            for (int i = 0; i < 512; i++)
            {
                colors2[i] = waterGradient02.Evaluate((float)i/ 511);
            }

            RampTexture02.SetPixels(colors2);
            RampTexture02.Apply();
            
            // 全局赋值，将 RampTexture01 纹理赋值到 _RampTexture01 上 
            Shader.SetGlobalTexture("_RampTexture01", RampTexture01);
            Shader.SetGlobalTexture("_RampTexture02", RampTexture02);
        }
    }
}