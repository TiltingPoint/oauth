using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace TiltingPoint.Auth.Editor
{
    // todo: backward compatibility support with older unity versions .

    /// <summary>
    /// Sets up a proper android dependency setup.
    /// </summary>
    public class AuthDependencySetup : MonoBehaviour
    {
        private const string DEPENCENCY_SETUP_FOLDER = "Packages/com.tiltingpoint.auth/Editor/DependencySetup";
        private const string ANDROID_PLUGIN_FOLDER = "Assets/Plugins/Android";
        private const string MANIFEST_TOOLS = "http://schemas.android.com/tools";
        private const string AUTH_CONFIG_NAME = "AuthConfig";
        private const string ORIGINAL_UNITY_GRADLE = "com.android.tools.build:gradle:3.4.0";
        private const string UPDATED_UNITY_GRADLE = "com.android.tools.build:gradle:4.0.1";
        private const string APPCOMPAT_IMPLEMENTATION = "implementation 'com.android.support:appcompat-v7:26.0.0'";
        private const string BROWSER_IMPLEMENTATION = "implementation 'androidx.browser:browser:1.3.0'";
        private const string PROPERTIES_FLAG = "**ADDITIONAL_PROPERTIES**";
        private const string ANDROIDX_PROP = "android.useAndroidX=true";
        private const string JETIFIER_PROP = "android.enableJetifier=true";
        private const string APPAUTH_REDIRECT_ACTIVITY = "net.openid.appauth.RedirectUriReceiverActivity";
        private const string APPAUTH_REDIRECT_ACTIVITY_PROPERTY = "android:name=\"" + APPAUTH_REDIRECT_ACTIVITY + "\"";
        private const string TP_AUTH_ACTIVITY = "com.tiltingpoint.android.AuthActivity";
        private const string ANDROID_TITLEBAR = "@android:style/Theme.Translucent.NoTitleBar";

        // base android manifest with correct backup rules
        private static readonly string AndroidManifestInPackage = Path.Combine(DEPENCENCY_SETUP_FOLDER, "AndroidManifest.xml");

        // contains gradle version used to build project, default 3.4.0, needed 4.0.1
        private static readonly string BaseProjectTemplateInPackage = Path.Combine(DEPENCENCY_SETUP_FOLDER, "baseProjectTemplate.gradle");

        // contains gradle properties AndroidX? Jetifier? here!
        private static readonly string BaseGradlePropertiesInPackage = Path.Combine(DEPENCENCY_SETUP_FOLDER, "gradleTemplate.properties");

        // contains base information about dependencies, if included, libraries will be added during build time, no need to download anything in Editor
        private static readonly string MainTemplateInPackage = Path.Combine(DEPENCENCY_SETUP_FOLDER, "mainTemplate.gradle");

        private static readonly string AndroidManifestInAssets = Path.Combine(ANDROID_PLUGIN_FOLDER, "AndroidManifest.xml");

        private static readonly string BaseProjectTemplateInAssets = Path.Combine(ANDROID_PLUGIN_FOLDER, "baseProjectTemplate.gradle");

        // contains gradle properties AndroidX? Jetifier? here!
        private static readonly string BaseGradlePropertiesInAssets = Path.Combine(ANDROID_PLUGIN_FOLDER, "gradleTemplate.properties");

        private static readonly string MainTemplateInAssets = Path.Combine(ANDROID_PLUGIN_FOLDER, "mainTemplate.gradle");

        [MenuItem("TiltingPoint/Auth/Android dependencies")]
        public static void SetupAuthDependencies()
        {
            // Create folder if it does not exist
            Directory.CreateDirectory(ANDROID_PLUGIN_FOLDER);

            CheckAndroidManifestContents();

            CheckBaseProjectTemplateContents();

            CheckGradlePropertiesTemplateContents();

            CheckMainTemplateContents();

            AssetDatabase.Refresh();

            Debug.Log("[TP Auth] Android dependencies setup complete.");
        }

        private static bool AddBeforeLine(string file, string tag, string lineToAdd)
        {
            var txtLines = File.ReadAllLines(file).ToList();
            var index = txtLines.IndexOf(tag);
            if (index > 0)
            {
                txtLines.Insert(txtLines.IndexOf(tag), lineToAdd);
                File.WriteAllLines(file, txtLines);
                return true;
            }

            return false;
        }

        private static void AddActivities(XmlDocument manifest)
        {
            /*
                <activity
                        android:name="net.openid.appauth.RedirectUriReceiverActivity"
                        tools:node="replace"
                        android:exported="true">
                    <intent-filter>
                        <action android:name="android.intent.action.VIEW"/>
                        <category android:name="android.intent.category.DEFAULT"/>
                        <category android:name="android.intent.category.BROWSABLE"/>
                        <data android:scheme="com.example.authtest1"/>
                    </intent-filter>
                </activity>
                <activity android:name="com.tiltingpoint.android.AuthActivity" android:theme="@android:style/Theme.Translucent.NoTitleBar"></activity>
             */

            var config = Resources.Load<TPAuthConfig>(AUTH_CONFIG_NAME);

            if (config == null)
            {
                Debug.LogError(
                    "[TP Auth] Could not load config file. Please try setting it up using TiltingPoint > Auth Config");
                return;
            }

            var callbackUrl = config.callbackUrl;
            if (string.IsNullOrEmpty(callbackUrl))
            {
                Debug.LogError(
                "[TP Auth] Callback URL is empty. Please try setting it up using TiltingPoint > Auth Config");
                return;
            }

            if (callbackUrl.EndsWith(":/"))
            {
                callbackUrl = callbackUrl.Remove(callbackUrl.Length - 2);
            }

            if (manifest.DocumentElement != null)
            {
                foreach (XmlNode node in manifest.DocumentElement.ChildNodes)
                {
                    if (node.Name != "application")
                    {
                        continue;
                    }

                    var actionElement = manifest.CreateElement("action");
                    actionElement.SetAttribute("android__name", "android.intent.action.VIEW");
                    var categoryElement = manifest.CreateElement("category");
                    categoryElement.SetAttribute("android__name", "android.intent.category.DEFAULT");
                    var categoryElement2 = manifest.CreateElement("category");
                    categoryElement2.SetAttribute("android__name", "android.intent.category.BROWSABLE");
                    var dataElement = manifest.CreateElement("data");
                    dataElement.SetAttribute("android__scheme", callbackUrl);

                    var intentElement = manifest.CreateElement("intent-filter");
                    intentElement.AppendChild(actionElement);
                    intentElement.AppendChild(categoryElement);
                    intentElement.AppendChild(categoryElement2);
                    intentElement.AppendChild(dataElement);

                    var appAuthActivity = manifest.CreateElement("activity");
                    appAuthActivity.SetAttribute("android__name", APPAUTH_REDIRECT_ACTIVITY);
                    appAuthActivity.SetAttribute("tools__node", "replace");
                    appAuthActivity.SetAttribute("android__exported", "true");
                    appAuthActivity.AppendChild(intentElement);

                    var tpAuthActivity = manifest.CreateElement("activity");
                    tpAuthActivity.SetAttribute("android__name", TP_AUTH_ACTIVITY);
                    tpAuthActivity.SetAttribute("android__theme", ANDROID_TITLEBAR);

                    var applicationElement = (XmlElement)node;
                    applicationElement.AppendChild(appAuthActivity);
                    applicationElement.AppendChild(tpAuthActivity);

                    Debug.Log($"[TP Auth]: New activities have been added to your manifest.");
                    manifest.Save(AndroidManifestInAssets);
                    CleanManifestFile();
                    return;
                }
            }

            Debug.LogError("[TP Auth]: Could not find <application> in your app’s AndroidManifest.xml file.");
        }

        private static void CheckAndroidManifestContents()
        {
            var androidManifestExists = File.Exists(AndroidManifestInAssets);
            if (!androidManifestExists)
            {
                FileUtil.CopyFileOrDirectory(AndroidManifestInPackage, AndroidManifestInAssets);
                var manifestFile = new XmlDocument();
                manifestFile.Load(AndroidManifestInAssets);
                AddActivities(manifestFile);
            }
            else
            {
                XmlDocument manifestFile = null;
                var line = $"xmlns:tools=\"{MANIFEST_TOOLS}\"";
                var toolsCheck = CheckFileContents(
                    AndroidManifestInAssets,
                    line);
                if (!toolsCheck)
                {
                    manifestFile = new XmlDocument();
                    manifestFile.Load(AndroidManifestInAssets);
                    CheckAndroidTools(manifestFile);
                }

                var activityCheck = CheckFileContents(
                    AndroidManifestInAssets,
                    APPAUTH_REDIRECT_ACTIVITY_PROPERTY);
                if (!activityCheck)
                {
                    if (manifestFile == null)
                    {
                        manifestFile = new XmlDocument();
                        manifestFile.Load(AndroidManifestInAssets);
                    }

                    AddActivities(manifestFile);
                }
            }
        }

        private static void CheckAndroidTools(XmlDocument manifest)
        {
            var toolsFound = false;
            if (manifest.DocumentElement == null)
            {
                return;
            }

            foreach (XmlNode node in manifest.DocumentElement.Attributes)
            {
                if (node.Name != "xmlns:tools")
                {
                    continue;
                }

                toolsFound = true;
                var sb = new StringBuilder();
                sb.Append("[TP Auth] The Auth package requires the usage of android default tools in your AndroidManifest.xml");
                sb.Append($"\nExpected value is {MANIFEST_TOOLS}");
                sb.Append($"\nCurrent value is: {node.Value}");
                Debug.LogError(sb.ToString());
            }

            if (!toolsFound)
            {
                manifest.DocumentElement.SetAttribute("xmlns__tools", MANIFEST_TOOLS);
                manifest.Save(AndroidManifestInAssets);
                CleanManifestFile();
                Debug.Log($"[TP Auth] Added {MANIFEST_TOOLS} to your AndroidManifest.xml");
            }
        }

        private static void CheckBaseProjectTemplateContents()
        {
            var baseProjectTemplateExists = File.Exists(BaseProjectTemplateInAssets);
            if (!baseProjectTemplateExists)
            {
                FileUtil.CopyFileOrDirectory(BaseProjectTemplateInPackage, BaseProjectTemplateInAssets);
                Debug.Log("[TP Auth] baseProjectTemplate.gradle has been added to the project.");
            }
            else
            {
                CheckGradleVersion();
            }
        }

        private static void CheckGradlePropertiesTemplateContents()
        {
            var baseGradlePropertiesExists = File.Exists(BaseGradlePropertiesInAssets);

            // Copy over gradle properties if it doesn't exist.
            if (!baseGradlePropertiesExists)
            {
                FileUtil.CopyFileOrDirectory(BaseGradlePropertiesInPackage, BaseGradlePropertiesInAssets);
                Debug.Log("[TP Auth] gradleTemplate.properties has been added to the project.");
            }
            else
            {
                if (!CheckFileContents(BaseGradlePropertiesInAssets, ANDROIDX_PROP))
                {
                    AddProperty(ANDROIDX_PROP);
                }

                if (!CheckFileContents(BaseGradlePropertiesInAssets, JETIFIER_PROP))
                {
                    AddProperty(JETIFIER_PROP);
                }
            }
        }

        private static void CheckMainTemplateContents()
        {
            var mainTemplateExists = File.Exists(MainTemplateInAssets);

            // Copy over mainTemplate.gradle if it doesn't exist
            if (!mainTemplateExists)
            {
                FileUtil.CopyFileOrDirectory(MainTemplateInPackage, MainTemplateInAssets);
                Debug.Log("[TP Auth] mainTemplate.gradle has been added to the project.");
            }
            else
            {
                if (!CheckFileContents(MainTemplateInAssets, APPCOMPAT_IMPLEMENTATION))
                {
                    AddImplementation(APPCOMPAT_IMPLEMENTATION);
                }

                if (!CheckFileContents(MainTemplateInAssets, BROWSER_IMPLEMENTATION))
                {
                    AddImplementation(BROWSER_IMPLEMENTATION);
                }
            }
        }

        private static void AddImplementation(string implementation)
        {
            const string flagTag = "**DEPS**}";
            var tabbedImplementation = $"    {implementation}";
            Debug.Log(AddBeforeLine(MainTemplateInAssets, flagTag, tabbedImplementation) ? $"[TP Auth] {implementation} has been added to {MainTemplateInAssets}."
                : $"[TP Auth] Error adding {implementation} to {MainTemplateInAssets}. Please check the file formatting");
        }

        private static void AddProperty(string property)
        {
            Debug.Log(AddBeforeLine(BaseGradlePropertiesInAssets, PROPERTIES_FLAG, property) ? $"[TP Auth] {property} has been added to {BaseGradlePropertiesInAssets}."
                : $"[TP Auth] Error adding {property} to {BaseGradlePropertiesInAssets}. Please check the file formatting");
        }

        private static bool CheckFileContents(string pathInAssets, string pattern)
        {
            if (!File.Exists(pathInAssets))
            {
                Debug.LogError($"[TP Auth] {pathInAssets} not found.");
                return false;
            }

            var fileContents = File.ReadAllText(pathInAssets);
            return Regex.IsMatch(fileContents, pattern);
        }

        private static void CleanManifestFile()
        {
            // Due to XML writing issue with XmlElement methods which are unable
            // to write "android:[param]" string, we have wrote "android__[param]" string instead.
            // Now make the replacement: "android:[param]" -> "android__[param]"
            var manifestContent = File.ReadAllText(AndroidManifestInAssets);

            var regex = new Regex("android__");
            manifestContent = regex.Replace(manifestContent, "android:");

            regex = new Regex("tools__");
            manifestContent = regex.Replace(manifestContent, "tools:");

            regex = new Regex("xmlns__");
            manifestContent = regex.Replace(manifestContent, "xmlns:");

            TextWriter manifestWriter = new StreamWriter(AndroidManifestInAssets);
            manifestWriter.Write(manifestContent);
            manifestWriter.Close();
        }

        private static void CheckGradleVersion()
        {
            if (!CheckFileContents(BaseProjectTemplateInAssets, ORIGINAL_UNITY_GRADLE))
            {
                return;
            }

            var manifestContent = File.ReadAllText(BaseProjectTemplateInAssets);

            var regex = new Regex(ORIGINAL_UNITY_GRADLE);
            manifestContent = regex.Replace(manifestContent, UPDATED_UNITY_GRADLE);
            TextWriter manifestWriter = new StreamWriter(BaseProjectTemplateInAssets);
            manifestWriter.Write(manifestContent);
            manifestWriter.Close();
            Debug.Log("[TP Auth] Gradle version has been bumped to 4.0.1");
        }
    }
}