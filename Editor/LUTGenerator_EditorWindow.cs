#if UNITY_EDITOR

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HH.LUTTextureGenerator
{
    public class LUTGenerator_EditorWindow : EditorWindow
    {
        // Non Nullable Properties
        static Texture _previewTexture;
        // static RenderTexture _renderTexture;
        static LUTGeneratorConfig_ScriptableObject _Config;
        const string Config_Path = "Assets/LUTGenerator/Config/";
        const string Config_Name = "LUTGeneratorConfig.asset";
        

        [MenuItem("Window/Generation/LUT", false, 10)]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            LUTGenerator_EditorWindow window = (LUTGenerator_EditorWindow)EditorWindow.GetWindow(typeof(LUTGenerator_EditorWindow));
            window.titleContent = new GUIContent("LUT Generator", "LUT Texture Generator");
            window.Show();

            loadAssets();
            onPreviewMaterialChange(_currentMaterial);
        }

        protected void OnEnable ()
        {
            // Here we retrieve the data if it exists or we save the default field initialisers we set above
            var data = EditorPrefs.GetString("LUTGeneratorWindow", JsonUtility.ToJson(this, false));
            // Then we apply them to this window
            JsonUtility.FromJsonOverwrite(data, this);
        
            loadAssets();

            onPreviewMaterialChange(_currentMaterial, true);
            _gradient = _Config.previousGradient;
        }

        protected void OnDisable ()
        {
            // We get the Json data
            var data = JsonUtility.ToJson(this, false);
            // And we save it
            EditorPrefs.SetString("LUTGeneratorWindow", data);
            resetCurrentTextureToDefault();
            _Config.previousGradient = _gradient;
        }

        static void loadAssets()
        {
            if(_Config == null)
                _Config = AssetDatabase.LoadAssetAtPath<LUTGeneratorConfig_ScriptableObject>(Config_Path + Config_Name);

            if(_Config == null)
            {
                if(!Directory.Exists(Config_Path)) 
                    Directory.CreateDirectory(Config_Path);
             
                ScriptableObject SO = ScriptableObject.CreateInstance(typeof(LUTGeneratorConfig_ScriptableObject));
                _Config = SO as LUTGeneratorConfig_ScriptableObject;

                AssetDatabase.CreateAsset(_Config, Config_Path + Config_Name);
                AssetDatabase.SaveAssets();
                Debug.LogWarning("New config created");
            }

            // This could be used to make a render within the window
            // if(_renderTexture == null)
            //     _renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(Target_RenderTexture_Path);

            if(!Directory.Exists(_Config.Preview_LUT_Asset_Path)) 
            {
                Directory.CreateDirectory(_Config.Preview_LUT_Asset_Path);
                Texture2D tex = new Texture2D(_Config.resolution, 1, TextureFormat.RGB24, false);
                AssetDatabase.CreateAsset(tex, _Config.Preview_LUT_Asset_Full_Path);
            }

            if(_previewTexture == null)
                _previewTexture = AssetDatabase.LoadAssetAtPath<Texture>(_Config.Preview_LUT_Asset_Full_Path);
        }

        // Preview Variables
        static Material _currentMaterial;
        static Gradient _gradient = new Gradient();
        static Gradient _targetGradient = new Gradient();
        static string[] _materialTextureName = new string[0];
        [field:SerializeField] static int _selectedTexture;
        static Texture _currentTextureDefault;
        static bool PreviewToggle = true;

        void OnGUI()
        {
            GUILayout.Label("Gradient");
            EditorGUILayout.GradientField(_gradient);

            GUILayout.Space(15);

            if(GUILayout.Button("Reset Gradient"))
            {
                _gradient = new Gradient();
            }      

            if(checkGradientChange())
            {
                Render(_targetGradient, false); // render preview
                updatePreview();
            }
        
            GUILayout.Space(5);
            GUILayout.Label("Preview");

            Material targetMaterial = (Material)EditorGUILayout.ObjectField(new GUIContent(" Material"), _currentMaterial, typeof(Material), true);
            onPreviewMaterialChange(targetMaterial);

            if(_currentMaterial)
            {
                GUI.enabled = false;
                EditorGUILayout.ObjectField(new GUIContent("Shader"), _currentMaterial.shader, typeof(Shader), true);
                GUI.enabled = true;
                
                int targetTexture = EditorGUILayout.Popup("Target Texture Property" ,_selectedTexture, _materialTextureName);

                // bool shouldRegister = targetTexture != _selectedTexture;
                // if(shouldRegister)
                //     Undo.RegisterCompleteObjectUndo(this, "Target Texture Preview");

                onSelectedTextureChange(targetTexture);

                string buttonText = "";
                if(!PreviewToggle)
                    buttonText = "On";
                else
                    buttonText = "Off";

                if(GUILayout.Button("Toggle Preview " + buttonText))
                {
                    PreviewToggle = !PreviewToggle;
                    updatePreview();
                }      
            }

            GUILayout.Space(5);
            GUILayout.Label("Rendering Options");

            _Config._gradientName = EditorGUILayout.TextField("File Name", _Config._gradientName);
            _Config._gradientSavePath = EditorGUILayout.TextField("Save Path", _Config._gradientSavePath);

            _Config.SetTextureOnCreation = EditorGUILayout.Toggle("Set Texture On Save", _Config.SetTextureOnCreation);

            string buttonText2 = "";
            if(_Config.SetTextureOnCreation)
                buttonText2 = " and set texture";

            if(GUILayout.Button("Save As .png" + buttonText2))
            {
                Render(_targetGradient, true); // render texture
            }            
        }

        /// <summary>Updates '_currentMaterial' if a change is detected</summary>
        /// <param name="forceUpdate">update the material without resetting the preview texture</param>
        static void onPreviewMaterialChange(Material target, bool forceUpdate = false)
        {
            if(target == _currentMaterial && !forceUpdate)
                return;
            
            //Reset to default Texture
            if(!forceUpdate)
                resetCurrentTextureToDefault();
            
            // Get all texture properties
            List<string> TexProperty = new List<string>();

            if(target != null)
            {
                for (int i = 0; i < target.shader.GetPropertyCount(); i++)
                {
                    string PropName = target.shader.GetPropertyName(i);

                    bool ignoreProp = false;
                    for (int j = 0; j < _Config.ignoreTexturePropertyNames.Length; j++)
                    {
                        if(PropName.Equals(_Config.ignoreTexturePropertyNames[j]))
                        {
                            ignoreProp = true;
                            break;
                        }
                    }

                    if(ignoreProp)
                        continue;

                    for (int j = 0; j < _Config.SearchTexturePropertyStrings.Length; j++)
                    {
                        if(PropName.Contains(_Config.SearchTexturePropertyStrings[j]))
                        {
                            TexProperty.Add(PropName);
                            break;
                        }
                    }
                }
            }

            _materialTextureName = TexProperty.ToArray();

            // Set new material
            _currentMaterial = target;

            onSelectedTextureChange(_selectedTexture, true);
        }

        /// <summary>Updates the target texture property for previewing the gradient if a change is detected</summary>
        /// <param name="forceUpdate">Update the target property without resetting the preview texture</param>
        static void onSelectedTextureChange(int target, bool forceUpdate = false)
        {
            if(_currentMaterial == null)
                return;

            if(target == _selectedTexture && !forceUpdate)
                return;
            
            // Reset to previous Texture
            if(!forceUpdate)
                resetCurrentTextureToDefault();

            // Get default Texture
            string targetTex = _materialTextureName[target];
            _currentTextureDefault = _currentMaterial.GetTexture(targetTex);
            // Set preview Texture
            if(PreviewToggle)
                _currentMaterial.SetTexture(targetTex, _previewTexture);
            else
                _currentMaterial.SetTexture(targetTex, _currentTextureDefault);
            // Set selected target
            _selectedTexture = target;
        }

        /// <summary>Detects changes in the gradient</summary>
        /// <returns>true if the gradient has changed</returns>
        bool checkGradientChange()
        {
            bool hasChanged = false;

            if(_gradient.colorKeys.Length != _targetGradient.colorKeys.Length)
                hasChanged = true;

            if(_gradient.alphaKeys.Length != _targetGradient.alphaKeys.Length)
                hasChanged = true;

            for (int i = 0; i < _gradient.colorKeys.Length; i++)
            {
                if(hasChanged)
                    break;
                
                if(_gradient.colorKeys[i].color != _targetGradient.colorKeys[i].color)
                    hasChanged = true;

                if(_gradient.colorKeys[i].time != _targetGradient.colorKeys[i].time)
                    hasChanged = true;
            }

            for (int i = 0; i < _gradient.alphaKeys.Length; i++)
            {
                if(hasChanged)
                    break;
                
                if(_gradient.alphaKeys[i].alpha != _targetGradient.alphaKeys[i].alpha)
                    hasChanged = true;

                if(_gradient.alphaKeys[i].time != _targetGradient.alphaKeys[i].time)
                    hasChanged = true;
            }

            if(hasChanged)
            {
                _targetGradient.alphaKeys = _gradient.alphaKeys;
                _targetGradient.colorKeys = _gradient.colorKeys;
            }

            return hasChanged;
        }

        /// <summary>Render the gradient into a texture</summary>
        /// <param name="target">Gradient to render</param>
        /// <param name="GeneratePNG">false - only render to preview, true - render to a .png</param>
        void Render(Gradient target, bool GeneratePNG)
        {
            // # RenderTexture to Texture2D         
            // Graphics.Blit(_defaultWhite, _renderTexture, _currentMaterial); // render material with shader onto render texture
            // Texture2D tex = new Texture2D(_resolution, 1, TextureFormat.RGB24, false);
            // tex.wrapMode = TextureWrapMode.Clamp;
            // ReadPixels looks at the active RenderTexture.
            // RenderTexture.active = _renderTexture;
            // tex.ReadPixels(new Rect(0, 0, _resolution, _resolution), 0, 0);
            // tex.Apply();

            Texture2D tex = new Texture2D(_Config.resolution, 1, TextureFormat.RGB24, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            // Fill it
            for (int i = 0; i < _Config.resolution; ++i)
            {
                var ratio = i / (float)(_Config.resolution - 1);
                tex.SetPixel(i, 0, target.Evaluate(ratio));
            }
            tex.Apply();

            string fileName = "";
            string filePath = "";

            if(GeneratePNG)
            {
                textureFullPath(out filePath, out fileName);
             
                if(!Directory.Exists(filePath)) 
                    Directory.CreateDirectory(filePath);
             
                if(System.IO.File.Exists(filePath+fileName))
                {
                    OverwriteConfirm_Wizard.MenuEntryCall(tex, filePath, fileName, _Config.SetTextureOnCreation);
                }
                else
                    SaveTexture2DToPNG(tex, filePath, fileName, _Config.SetTextureOnCreation);
            }
            else
            {
                fileName  = _Config.Preview_LUT_Asset_Full_Path;
                AssetDatabase.CreateAsset(tex, fileName);
                _previewTexture = AssetDatabase.LoadAssetAtPath<Texture>(_Config.Preview_LUT_Asset_Full_Path);
            }
        }

        /// <summary>Saves a Texture2D as a png at file path with name</summary>
        /// <param name="tex">Target texture to save</param>
        /// <param name="filePath">Full system file path</param>
        /// <param name="fileName">Name of the saved file</param>
        /// <param name="SetAsTexture">Sets the saved texture to the current preview material texture property</param>
        public static void SaveTexture2DToPNG(Texture2D tex, string filePath, string fileName, bool SetAsTexture)
        {
            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(filePath + fileName, bytes);
            AssetDatabase.Refresh();
            Debug.Log("Texture Generated to: " + filePath + fileName);

            if(SetAsTexture)
            {
                PreviewToggle = true;
                onPreviewMaterialChange(_currentMaterial);
                _currentMaterial = null;
            }
            else
                onPreviewMaterialChange(null);

            if(!_Config.SelectTextureOnCreation)
                return;

            string[] split = filePath.Split("Assets/");
            EditorUtility.FocusProjectWindow();
            Object CreatedObject = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/"+split[1]+fileName);
            Selection.activeObject = CreatedObject;
        }

        void textureFullPath(out string Path, out string Name)
        {
            Path =  Application.dataPath + "/" + _Config._gradientSavePath; 

            if(_Config._gradientName.Contains("{0}"))
                Name = string.Format(_Config._gradientName,
                    System.DateTime.Now.ToString("yyy-MM-dd_HH-mm-ss"));
            else
                Name = _Config._gradientName;

            Name += ".png";
        }

        /// <summary>Resets the preview material texture to its original texture</summary>
        static void resetCurrentTextureToDefault()
        {
            if(_materialTextureName == null)
                return;

            if(_materialTextureName.Length == 0)
                return;

            // ! does not reach this line

            if(_selectedTexture > _materialTextureName.Length)
                return;
            
            string currentTex = _materialTextureName[_selectedTexture];

            if(_currentMaterial == null)
                return;

            if(_currentMaterial.GetTexture(currentTex) != _previewTexture)
                return;
        

            _currentMaterial.SetTexture(currentTex, _currentTextureDefault);
            
            if(_currentMaterial.GetTexture(currentTex) == _previewTexture)
                Debug.LogWarning("Could not reset Texture");
        }

        void updatePreview()
        {
            if(_currentMaterial == null)
                return;

            if(_materialTextureName.Length == 0)
                return;

            if(_selectedTexture > _materialTextureName.Length)
                return;

            string currentTexture = _materialTextureName[_selectedTexture];

            if(PreviewToggle)
                _currentMaterial.SetTexture(currentTexture, _previewTexture);
            else
                _currentMaterial.SetTexture(currentTexture, _currentTextureDefault);
        }
    }
}
#endif