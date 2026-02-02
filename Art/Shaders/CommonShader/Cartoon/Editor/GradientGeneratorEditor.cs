using System.IO;
using UnityEditor;
using UnityEngine;

namespace CommonShader.Water
{
    [CustomEditor(typeof(CartoonController))]
    public class GradientGeneratorEditor : Editor
    {
        private CartoonController cartoonController;

        void OnEnable()
        {
            cartoonController = (CartoonController)target;
        }

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("生成纹理"))
            {
                string defaultPath = Application.dataPath + "Art/Texture/";
                string path = EditorUtility.SaveFilePanel("保存纹理", defaultPath, "ShadowRampMap", "png");
                if (path != null && Directory.Exists(path))
                {
                    File.WriteAllBytes(path,cartoonController.RampTexture01.EncodeToPNG());
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogWarning("路径不存在");
                }
            }
        }
    }
}