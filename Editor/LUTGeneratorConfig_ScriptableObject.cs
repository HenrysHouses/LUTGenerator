#if UNITY_EDITOR

using UnityEngine;

namespace HH.LUTTextureGenerator
{
    [CreateAssetMenu(menuName = "Gradient Asset")]
    public class LUTGeneratorConfig_ScriptableObject : ScriptableObject
    {
        [HideInInspector] public Gradient previousGradient = new Gradient();
        [HideInInspector] public string Preview_LUT_Asset_Full_Path => Preview_LUT_Asset_Path + Preview_LUT_Asset_Name;
        [HideInInspector] public string Preview_LUT_Asset_Path => "Assets/LUTGenerator/Assets/";
        [HideInInspector] public string Preview_LUT_Asset_Name => "Preview_LUT.asset";
        // public string Target_RenderTexture_Path {get; private set;} = "Assets/LUTGenerator/Assets/Render_LUT.renderTexture";
        [SerializeField] public string _gradientName = "LUT_Render_{0}";
        [SerializeField] public string _gradientSavePath = "LUTGenerator/Outputs/";
        public string[] SearchTexturePropertyStrings {get; private set;} = new string[]{"Tex", "Texture"};
        public string[] ignoreTexturePropertyNames {get; private set;} = new string[]{"_SmoothnessTextureChannel"};
        public int resolution = 520;
        public bool SetTextureOnCreation = false;
        public bool SelectTextureOnCreation = true;
    }
}
#endif