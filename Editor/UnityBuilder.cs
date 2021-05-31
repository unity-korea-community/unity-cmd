using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace UNKO.Unity_Builder
{
    /// <summary>
    /// 빌드 전 세팅 (빌드 후 되돌리기용)
    /// </summary>
    public class PlayerSetting_Backup
    {
        public string defineSymbol { get; private set; }
        public string productName { get; private set; }

        public BuildTargetGroup buildTargetGroup { get; private set; }

        public PlayerSetting_Backup(BuildTargetGroup buildTargetGroup, string defineSymbol, string productName)
        {
            this.buildTargetGroup = buildTargetGroup;
            this.defineSymbol = defineSymbol;
            this.productName = productName;
        }

        public void Restore()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defineSymbol);
            PlayerSettings.productName = productName;
        }
    }

    public class UnityBuilder
    {
        public static void Build()
        {
            if (GetSO_FromCommandLine("configpath", out BuildConfig config))
            {
                Build(config);
            }
            else
            {
                Debug.LogError("require -configpath");
            }
        }

        public static void Build(BuildConfig buildConfig)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildConfig.buildTarget);
            BuildPlayerOptions buildPlayerOptions = Generate_BuildPlayerOption(buildConfig);
            PlayerSetting_Backup editorSetting_Backup = SettingBuildConfig_To_EditorSetting(buildConfig, buildTargetGroup);

            string overwriteConfigJson = Environment.GetEnvironmentVariable("overwrite");
            if (string.IsNullOrEmpty(overwriteConfigJson) == false)
            {
                Debug.Log($"overwrite config : {overwriteConfigJson}");
                JsonUtility.FromJsonOverwrite(overwriteConfigJson, buildConfig);
            }

            Dictionary<string, string> commandLine = new Dictionary<string, string>();
            try
            {
                buildConfig.OnPreBuild(commandLine, ref buildPlayerOptions);
                BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(buildPlayerOptions);
                buildConfig.OnPostBuild(commandLine);

                PrintBuildResult(buildConfig.GetBuildPath(), report);
            }
            catch (Exception e)
            {
                Debug.Log("Error - " + e);
                throw;
            }
            finally
            {
                editorSetting_Backup.Restore();
            }
            Debug.Log($"After Build DefineSymbol Current {PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup)}");

            // 2018.4 에서 프로젝트 전체 리임포팅 하는 이슈 대응
            // https://issuetracker.unity3d.com/issues/osx-batchmode-build-hangs-at-refresh-detecting-if-any-assets-need-to-be-imported-or-removed
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public static bool GetSO_FromCommandLine<T>(string commandLine, out T outFile)
            where T : ScriptableObject
        {
            outFile = null;
            string path = Environment.GetEnvironmentVariable(commandLine);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"environment variable {commandLine} is null or empty");
                return false;
            }

            outFile = AssetDatabase.LoadAssetAtPath<T>(path);
            return outFile != null;
        }

        // ==============================================================================================

        #region private

        private static BuildPlayerOptions Generate_BuildPlayerOption(BuildConfig buildConfig)
        {
            List<string> sceneNames = new List<string>(buildConfig.buildSceneNames);
            sceneNames.ForEach(sceneName => sceneName += ".unity");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = sceneNames.ToArray(),
                locationPathName = buildConfig.GetBuildPath(),
                target = buildConfig.buildTarget,
                options = BuildOptions.None
            };

            return buildPlayerOptions;
        }


        private static PlayerSetting_Backup SettingBuildConfig_To_EditorSetting(BuildConfig buildConfig, BuildTargetGroup buildTargetGroup)
        {
            string defineSymbol_Backup = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, buildConfig.defineSymbol);

            string productName_Backup = PlayerSettings.productName;
            PlayerSettings.productName = buildConfig.productName;

            return new PlayerSetting_Backup(buildTargetGroup, defineSymbol_Backup, productName_Backup);
        }

        private static void PrintBuildResult(string path, BuildReport report)
        {
            BuildSummary summary = report.summary;
            Debug.Log($"Build Result:{summary.result}, Path:{path}");

            if (summary.result == BuildResult.Failed)
            {
                int errorIndex = 1;
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        {
                            Debug.LogFormat("Build Fail Log[{0}] : type : {1}\n" +
                                            "content : {2}", ++errorIndex, msg.type, msg.content);
                        }
                    }
                }
            }
        }

        #endregion private
    }
}

