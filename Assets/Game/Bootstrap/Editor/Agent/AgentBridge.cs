using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Game.Bootstrap.Editor.Agent
{
    /// <summary>
    /// Lets an automation agent drive the Editor through files in the <c>AgentBridge/</c> folder at the project root.
    /// </summary>
    /// <remarks>
    /// This assembly has no references to game or package assemblies, so it keeps compiling (and keeps reporting
    /// compile errors) while the rest of the project is broken or packages are still installing.
    /// The agent writes <c>AgentBridge/command.txt</c>; the Editor runs it, deletes the file and appends the outcome
    /// to <c>AgentBridge/result.log</c>. Compiler messages of every compilation go to <c>AgentBridge/compile.log</c>.
    /// Commands (one per line): <c>refresh</c>, <c>packages</c>, <c>defines</c>, <c>invoke Namespace.Type.Method</c>,
    /// <c>status</c>.
    /// </remarks>
    [InitializeOnLoad]
    public static class AgentBridge
    {
        /// <summary>
        /// Registry packages the project needs; versions are left to the Editor so it picks the ones verified for it.
        /// </summary>
        public static readonly string[] RequiredPackages =
        {
            "com.unity.addressables",
            "com.unity.nuget.newtonsoft-json",
            "com.unity.test-framework",
        };

        /// <summary>
        /// Scripting defines the stack rules require (UniTask awaiting DOTween tweens).
        /// </summary>
        public const string RequiredDefines = "UNITASK_DOTWEEN_SUPPORT";

        private const string BridgeFolder = "AgentBridge";
        private const double PollIntervalSeconds = 1.0;

        private static readonly List<string> _compileMessages = new List<string>();
        private static AddAndRemoveRequest _packageRequest;
        private static double _nextPoll;

        static AgentBridge()
        {
            Directory.CreateDirectory(BridgeFolder);
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            EditorApplication.update += OnUpdate;
            Log("domain loaded; assemblies: " + string.Join(", ", LoadedGameAssemblies()));
            EditorApplication.delayCall += InstallPackagesAndDefines;
        }

        /// <summary>
        /// Adds the missing registry packages and the required scripting defines. Safe to call repeatedly.
        /// </summary>
        [MenuItem("Tools/Agent/Install Packages And Defines")]
        public static void InstallPackagesAndDefines()
        {
            ApplyDefines();
            var manifest = File.ReadAllText(Path.Combine("Packages", "manifest.json"));
            var missing = RequiredPackages.Where(p => !manifest.Contains("\"" + p + "\"")).ToArray();
            if (missing.Length == 0)
            {
                Log("packages: all present");
                return;
            }

            Log("packages: adding " + string.Join(", ", missing));
            _packageRequest = Client.AddAndRemove(missing);
        }

        private static void OnUpdate()
        {
            PollPackageRequest();
            if (EditorApplication.timeSinceStartup < _nextPoll)
            {
                return;
            }

            _nextPoll = EditorApplication.timeSinceStartup + PollIntervalSeconds;
            var path = Path.Combine(BridgeFolder, "command.txt");
            if (!File.Exists(path))
            {
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            var lines = File.ReadAllLines(path);
            File.Delete(path);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }
                Execute(line);
            }
        }

        private static void Execute(string line)
        {
            Log("> " + line);
            try
            {
                var parts = line.Split(new[] { ' ' }, 2);
                switch (parts[0])
                {
                    case "refresh":
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        CompilationPipeline.RequestScriptCompilation();
                        break;
                    case "packages":
                        InstallPackagesAndDefines();
                        break;
                    case "defines":
                        ApplyDefines();
                        break;
                    case "invoke":
                        Invoke(parts[1].Trim());
                        break;
                    case "dotween":
                        InstallDOTween(parts[1].Trim());
                        break;
                    case "status":
                        Log("compiling=" + EditorApplication.isCompiling + " playing=" + EditorApplication.isPlaying +
                            " assemblies=" + string.Join(", ", LoadedGameAssemblies()));
                        break;
                    default:
                        Log("unknown command: " + parts[0]);
                        break;
                }
            }
            catch (Exception e)
            {
                Log("FAILED " + line + "\n" + e);
            }
        }

        private static void Invoke(string fullName)
        {
            var dot = fullName.LastIndexOf('.');
            var typeName = fullName.Substring(0, dot);
            var methodName = fullName.Substring(dot + 1);
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(typeName, false))
                .FirstOrDefault(t => t != null);
            if (type == null)
            {
                throw new InvalidOperationException("type not found: " + typeName);
            }

            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                throw new InvalidOperationException("method not found: " + fullName);
            }

            var result = method.Invoke(null, null);
            Log("ok " + fullName + (result != null ? " -> " + result : string.Empty));
        }

        /// <summary>
        /// Downloads the official DOTween zip, unpacks <c>Demigiant/</c> into <c>Assets/Plugins/</c> and adds the
        /// <c>DOTween.Modules</c> assembly definition that the stack rules reference.
        /// </summary>
        /// <param name="url">Direct link to the DOTween zip on dotween.demigiant.com.</param>
        private static void InstallDOTween(string url)
        {
            var zipPath = Path.Combine("Temp", "dotween.zip");
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                var op = request.SendWebRequest();
                while (!op.isDone)
                {
                    System.Threading.Thread.Sleep(50);
                }

                if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    throw new InvalidOperationException("download failed: " + request.error);
                }

                File.WriteAllBytes(zipPath, request.downloadHandler.data);
                Log("dotween: downloaded " + request.downloadHandler.data.Length + " bytes");
            }

            var extract = Path.Combine("Temp", "dotween");
            if (Directory.Exists(extract))
            {
                Directory.Delete(extract, true);
            }
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extract);
            var source = Directory.GetDirectories(extract, "Demigiant", SearchOption.AllDirectories).FirstOrDefault();
            if (source == null)
            {
                throw new InvalidOperationException("no Demigiant folder in the zip");
            }

            var target = Path.Combine("Assets", "Plugins", "Demigiant");
            Directory.CreateDirectory(Path.Combine("Assets", "Plugins"));
            if (Directory.Exists(target))
            {
                Directory.Delete(target, true);
            }
            Directory.Move(source, target);

            var modules = Directory.GetDirectories(target, "Modules", SearchOption.AllDirectories).FirstOrDefault();
            if (modules != null)
            {
                File.WriteAllText(Path.Combine(modules, "DOTween.Modules.asmdef"),
                    "{\n    \"name\": \"DOTween.Modules\",\n    \"references\": [\"UnityEngine.UI\"],\n" +
                    "    \"autoReferenced\": true\n}\n");
            }

            Log("dotween: installed to " + target + (modules != null ? ", modules asmdef at " + modules : ""));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ApplyDefines()
        {
            foreach (var target in new[] { NamedBuildTargetName.Standalone, NamedBuildTargetName.Android, NamedBuildTargetName.iOS })
            {
                var named = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(target);
                var current = PlayerSettings.GetScriptingDefineSymbols(named);
                var set = new HashSet<string>(current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                if (set.Add(RequiredDefines))
                {
                    PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", set));
                    Log("defines: " + target + " += " + RequiredDefines);
                }
            }
        }

        private static void PollPackageRequest()
        {
            if (_packageRequest == null || !_packageRequest.IsCompleted)
            {
                return;
            }

            Log(_packageRequest.Status == StatusCode.Success
                ? "packages: installed " + string.Join(", ", _packageRequest.Result.Select(p => p.name + "@" + p.version))
                : "packages: FAILED " + _packageRequest.Error?.message);
            _packageRequest = null;
        }

        private static void OnCompilationStarted(object context)
        {
            _compileMessages.Clear();
        }

        private static void OnAssemblyCompilationFinished(string assembly, CompilerMessage[] messages)
        {
            foreach (var m in messages)
            {
                if (m.type == CompilerMessageType.Error || m.message.Contains("warning CS0"))
                {
                    _compileMessages.Add($"{m.type}: {m.file}({m.line},{m.column}): {m.message}");
                }
            }
        }

        private static void OnCompilationFinished(object context)
        {
            var errors = _compileMessages.Count(m => m.StartsWith("Error"));
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.UtcNow:O}] compilation finished: {errors} error(s), " +
                          $"{_compileMessages.Count - errors} warning(s)");
            foreach (var m in _compileMessages)
            {
                sb.AppendLine(m);
            }

            File.WriteAllText(Path.Combine(BridgeFolder, "compile.log"), sb.ToString());
        }

        private static IEnumerable<string> LoadedGameAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name)
                .Where(n => n.StartsWith("Game.") || n == "UniTask" || n == "UniRx" || n == "VContainer" ||
                            n == "DOTween.Modules" || n == "Unity.Addressables")
                .OrderBy(n => n);
        }

        private static void Log(string message)
        {
            var line = $"[{DateTime.UtcNow:O}] {message}";
            Debug.Log("[AgentBridge] " + message);
            File.AppendAllText(Path.Combine(BridgeFolder, "result.log"), line + Environment.NewLine);
        }

        private static class NamedBuildTargetName
        {
            public const BuildTargetGroup Standalone = BuildTargetGroup.Standalone;
            public const BuildTargetGroup Android = BuildTargetGroup.Android;
            public const BuildTargetGroup iOS = BuildTargetGroup.iOS;
        }
    }
}
