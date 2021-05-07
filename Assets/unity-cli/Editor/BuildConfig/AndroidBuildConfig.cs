using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Unity_CLI
{
    [CreateAssetMenu(fileName = "AndroidBuildConfig", menuName = Unity_CLIString.CreateAssetMenu_Prefix + "/AndroidBuildConfig")]
    public class AndroidBuildConfig : BuildConfigBase
    {
        public override BuildTarget buildTarget => BuildTarget.Android;

        public string keyalias_name;
        public string keyalias_password;

        /// <summary>
        /// Keystore 파일의 경로입니다. `파일경로/파일명.keystore` 까지 쓰셔야 합니다.
        /// <para>UnityProject/Asset/ 기준의 상대 경로입니다. </para>
        /// <para>예를들어 UnityProject/Asset 폴더 밑에 example.keystore가 있으면 "/example.keystore" 입니다.</para>
        /// </summary>
        public string keystore_relativePath;
        public string keystore_password;

        /// <summary>
        /// CPP 빌드를 할지 체크, CPP빌드는 오래 걸리므로 Test빌드가 아닌 Alpha 빌드부터 하는걸 권장
        /// 아직 미지원
        /// </summary>
        public ScriptingImplementation scriptingBackEnd;

        public int bundleVersionCode;

        public override void ResetSetting()
        {
            base.ResetSetting();

            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            scriptingBackEnd = PlayerSettings.GetScriptingBackend(targetGroup);
            bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
        }

        public override void OnPreBuild(IDictionary<string, string> commandLine)
        {
            PlayerSettings.Android.keyaliasName = keyalias_name;
            PlayerSettings.Android.keyaliasPass = keyalias_password;

            PlayerSettings.Android.keystoreName = Application.dataPath + keystore_relativePath;
            PlayerSettings.Android.keystorePass = keystore_password;
            PlayerSettings.Android.bundleVersionCode = bundleVersionCode;

            // if (usecppbuild)
            //     PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            // else
            //     PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);

            Debug.LogFormat("ApplySetting [Android]\n" +
                            "PackageName : {0}\n" +
                            "keyaliasName : {1}, keyaliasPass : {2}\n" +
                            "keystoreName : {3}, keystorePass : {4}\n" +
                PlayerSettings.applicationIdentifier,
                PlayerSettings.Android.keyaliasName, PlayerSettings.Android.keyaliasPass,
                PlayerSettings.Android.keystoreName, PlayerSettings.Android.keystorePass);
        }

        public override void OnPostBuild(IDictionary<string, string> commandLine)
        {
        }

        public override string GetBuildPath()
        {
            return base.GetBuildPath() + ".apk";
        }
    }

    [CustomEditor(typeof(AndroidBuildConfig))]
    public class AndroidBuildConfig_Inspector : Editor
    {
        string _commandLine;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            AndroidBuildConfig config = target as AndroidBuildConfig;
            if (GUILayout.Button("Build!"))
            {
                CLIBuilder.Build(config);
            }

            _commandLine = EditorGUILayout.TextField("commandLine", _commandLine);
            if (GUILayout.Button($"Build with \'{_commandLine}\'"))
            {
                string[] commands = _commandLine.Split(' ');
                for (int i = 0; i < commands.Length; i++)
                {
                    string command = commands[i];
                    bool hasNextCommand = i + 1 < commands.Length;
                    if (command.StartsWith("-"))
                    {
                        if (hasNextCommand)
                            Environment.SetEnvironmentVariable(command, commands[i + 1]);
                        else
                            Environment.SetEnvironmentVariable(command, "");
                    }
                }

                CLIBuilder.Build();
            }
        }
    }
}
