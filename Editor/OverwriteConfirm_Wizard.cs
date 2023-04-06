#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace HH.LUTTextureGenerator
{
    public class OverwriteConfirm_Wizard: ScriptableWizard {

        static Texture2D _texture;
        static string _Path;
        static string _Name;
        static bool _ShouldSet;

        public static void MenuEntryCall(Texture2D tex, string filePath, string fileName, bool SetTexture) {
            DisplayWizard<OverwriteConfirm_Wizard>("Confirm Overwrite");

            _texture = tex;
            _Path = filePath;
            _Name = fileName;
            _ShouldSet = SetTexture;
        }

        private void Awake() {
            minSize = new Vector2(684, 100);
        }

        void OnGUI() {
            Rect text_rect = new Rect(0, -position.height/2+10, position.width, position.height);
            EditorGUI.LabelField(text_rect, "This file already exists: " + _Name);    
            Rect text_rect_2 = new Rect(0, -position.height/2+25, position.width, position.height);
            EditorGUI.LabelField(text_rect_2, "At file path: " + _Path);  
            Rect text_rect_3 = new Rect(0, -position.height/2+50, position.width, position.height);
            EditorGUI.LabelField(text_rect_3, "Do you want to overwrite it?");   

            Rect Confirm_rect =  new Rect(position.width/2, position.height-30, position.width/2, 20);
            Rect Cancel_rect =  new Rect(0, position.height-30, position.width/2, 20);
            
            if(GUI.Button(Confirm_rect, "Confirm Overwrite"))
            {
                LUTGenerator_EditorWindow.SaveTexture2DToPNG(_texture, _Path, _Name, _ShouldSet);
                Close();
            }

            if(GUI.Button(Cancel_rect, "Cancel"))
            {
                Close();
            }
        }
    }
}
#endif