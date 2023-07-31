using System.IO;
using UnityEditor;
using UnityEngine;

namespace TiltingPoint.Auth.Editor
{
    public class AuthConfigWindow : EditorWindow
    {
        private const string RESOURCES_FOLDER = "Assets/TiltingPointSDK/Auth/Resources/";
        private const string PACKAGE_LINK_FILE = "Packages/com.tiltingpoint.auth/Editor/DependencySetup/link.xml";
        private const string CONFIG_ASSET = "AuthConfig.asset";
        private TPAuthConfig _authConfig;
        private string _callbackUrlInput;
        private string _clientIdInput;
        private string _issuerInput;
        private string _logoutUrlInput;
        private string _tokenUrlInput;
        private string _verifyEmailUrlInput;

        private void OnGUI()
        {
            ReadConfig();
            _issuerInput = EditorGUILayout.TextField("Issuer: ", _issuerInput);
            _clientIdInput = EditorGUILayout.TextField("Client ID: ", _clientIdInput);
            _callbackUrlInput = EditorGUILayout.TextField("Callback URL: ", _callbackUrlInput);
            _tokenUrlInput = EditorGUILayout.TextField("Token URL: ", _tokenUrlInput);
            _verifyEmailUrlInput = EditorGUILayout.TextField("Verify Email URL: ", _verifyEmailUrlInput);
            _logoutUrlInput = EditorGUILayout.TextField("Logout URL: ", _logoutUrlInput);

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Discard"))
            {
                Close();
            }

            if (GUILayout.Button("Save"))
            {
                SaveConfig();
            }

            GUILayout.EndHorizontal();
        }

        [MenuItem("TiltingPoint/Auth/Configuration")]
        public static void SetupAuth()
        {
            CheckForLinkFile();
            var window = GetWindow<AuthConfigWindow>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 120);
            window.ShowPopup();
        }

        private static void CheckForLinkFile()
        {
            var pathToLinkAssets = Path.Combine(RESOURCES_FOLDER, "link.xml");
            if (File.Exists(pathToLinkAssets))
            {
                return;
            }

            if (!Directory.Exists(RESOURCES_FOLDER))
            {
                Directory.CreateDirectory(RESOURCES_FOLDER);
            }

            FileUtil.CopyFileOrDirectory(PACKAGE_LINK_FILE, pathToLinkAssets);

            Debug.Log($"[TP AUTH] A new link.xml file has been added at {RESOURCES_FOLDER}");
        }

        private void ReadConfig()
        {
            if (_authConfig)
            {
                return;
            }

            const string fullPath = RESOURCES_FOLDER + CONFIG_ASSET;
            if (!File.Exists(fullPath))
            {
                Debug.Log($"[TP AUTH] Setting up a new config file at {fullPath}");
                _authConfig = CreateInstance<TPAuthConfig>();
                if (!Directory.Exists(RESOURCES_FOLDER))
                {
                    Directory.CreateDirectory(RESOURCES_FOLDER);
                }

                AssetDatabase.CreateAsset(_authConfig, fullPath);
            }
            else
            {
                _authConfig = (TPAuthConfig) EditorGUIUtility.Load(fullPath);
            }

            _issuerInput = _authConfig.issuer;
            _clientIdInput = _authConfig.clientId;
            _callbackUrlInput = _authConfig.callbackUrl;
            _tokenUrlInput = _authConfig.tokenUrl;
            _verifyEmailUrlInput = _authConfig.verifyEmailUrl;
            _logoutUrlInput = _authConfig.logoutUrl;
        }

        private void SaveConfig()
        {
            EditorUtility.SetDirty(_authConfig);
            _authConfig.issuer = _issuerInput;
            _authConfig.clientId = _clientIdInput;
            _authConfig.callbackUrl = _callbackUrlInput;
            _authConfig.tokenUrl = _tokenUrlInput;
            _authConfig.verifyEmailUrl = _verifyEmailUrlInput;
            _authConfig.logoutUrl = _logoutUrlInput;
            PrefabUtility.RecordPrefabInstancePropertyModifications(_authConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Close();
        }
    }
}