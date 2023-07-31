#if UNITY_IOS && !TP_CORE
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace TiltingPoint.Auth.Editor
{
    /// <summary>
    /// Enables C++ and Objective C exceptions in XCode for using @try.
    /// </summary>
    public class TPAuthPostProcessor
    {
        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            var xcodeProjectPath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj");
            var pbxPath = Path.Combine(xcodeProjectPath, "project.pbxproj");
            var xcodeProjectLines = File.ReadAllLines(pbxPath);
            var sb = new StringBuilder();
            foreach (var line in xcodeProjectLines)
            {
                if (line.Contains("GCC_ENABLE_OBJC_EXCEPTIONS") ||
                    line.Contains("GCC_ENABLE_CPP_EXCEPTIONS") ||
                    line.Contains("CLANG_ENABLE_MODULES"))
                {
                    var newLine = line.Replace("NO", "YES");
                    sb.AppendLine(newLine);
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            File.WriteAllText(pbxPath, sb.ToString());
        }
    }
}
#endif