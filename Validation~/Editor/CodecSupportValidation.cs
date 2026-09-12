using System;
using System.IO;
using System.Linq;
using System.Reflection;
using K13A.TSMP;
using K13A.TSMP.Udon;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
#if UDONSHARP
using UdonSharp;
using UdonSharp.Compiler;
using UdonSharpEditor;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Editor;
#endif
using Object = UnityEngine.Object;

public static class CodecSupportValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static string CodecName => Environment.GetEnvironmentVariable("TSMP_VALIDATION_CODEC");
    private static string Package => "Packages/com.kibalab.tsmp.codec." + CodecName.ToLowerInvariant();
    private static string PrefabPath => Package + "/Runtime/Codec_" + CodecName + ".prefab";

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log("[CodecValidation] PASS " + message);
    }

    private static TSMPCodec SelectCodec(TSMPSetup setup)
    {
        setup.RefreshInstalledCodecs();
        int index = Array.FindIndex(setup.codecPrefabs, item => item != null && item.displayName == CodecName);
        Check(index >= 0, "Codec discovered by Setup: " + CodecName);
        setup.selectedCodecIndex = index;
        setup.ApplyNow();
        var encoder = (TSMPEncoder)setup.encoder;
        Check(encoder.selectedCodec != null && encoder.selectedCodec.displayName == CodecName, "Setup selected codec");
        foreach (Transform child in setup.GetComponentsInChildren<Transform>(true))
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "No missing scripts: " + child.name);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Check(prefab != null && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) == 0, "SDK-neutral package prefab");
#if UDONSHARP
        Check(UdonSharpEditorUtility.GetBackingUdonBehaviour(encoder.selectedCodec) != null, "Automatic backing UdonBehaviour");
#endif
        foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { Package }))
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
            var material = new Material(shader);
            try { ShaderUtil.CompilePass(material, 0, true); }
            finally { Object.DestroyImmediate(material); }
            Check(!ShaderUtil.ShaderHasError(shader), "Shader compiles: " + shader.name);
        }
        return encoder.selectedCodec;
    }

#if !UDONSHARP
    private static void NativeScene()
    {
        typeof(UnitySupportValidation).GetMethod("CreateScene", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        var setup = Object.FindObjectOfType<TSMPSetup>();
        var codec = SelectCodec(setup);
        TestRasterVariants(codec, null);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Validation/Loopback.unity");
        AssetDatabase.SaveAssets();
    }

    public static void Play()
    {
        NativeScene();
        EditorApplication.EnterPlaymode();
    }

    public static void Build()
    {
        NativeScene();
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.Disabled);
        string path = Environment.GetEnvironmentVariable("TSMP_VALIDATION_BUILD");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Validation/Loopback.unity" },
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        File.WriteAllText(Path.ChangeExtension(path, ".build-report.txt"),
            "Result=" + report.summary.result + "\nErrors=" + report.summary.totalErrors +
            "\nWarnings=" + report.summary.totalWarnings + "\nBackend=Mono\nStripping=Disabled");
        Check(report.summary.result == BuildResult.Succeeded, "Windows x64 Player build");
    }
#endif

    private static void SetOption(TSMPCodec codec, string field, object value)
    {
        codec.GetType().GetField(field).SetValue(codec, value);
    }

    private static void TestRasterVariants(TSMPCodec original, Action<TSMPCodec, byte[], Color32[]> vmTest)
    {
        var clone = Object.Instantiate(original.gameObject);
        var codec = clone.GetComponent<TSMPCodec>();
        try
        {
            int variants = CodecName == "RGB16" ? 4 : CodecName == "Color256" ? 3 : 1;
            for (int variant = 0; variant < variants; variant++)
            {
                if (CodecName == "RGB16")
                {
                    SetOption(codec, "rBits", variant < 2 ? 4 : 5);
                    SetOption(codec, "gBits", variant < 2 ? 4 : 6);
                    SetOption(codec, "bBits", 4);
                    SetOption(codec, "localRefine", variant % 2 == 1);
                }
                if (CodecName == "Color256")
                {
                    SetOption(codec, "robustChannelDecode", variant > 0);
                    SetOption(codec, "localRefine", variant == 2);
                }
                byte[] bytes = Enumerable.Range(0, 1027).Select(i => (byte)((i * 73 + 19) & 255)).ToArray();
                var texture = new Texture2D(640, 360, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
                try
                {
                    Check(codec.TryWriteFrame(texture, 8, new byte[FrameHeader.Size], bytes, out string error), "Native raster variant " + variant + ": " + error);
                    Check(texture.GetPixels32().Any(pixel => pixel.r != pixel.g || pixel.g != pixel.b), "Nonblank color payload");
                    DecodeRaster(codec, texture, bytes);
                    vmTest?.Invoke(codec, bytes, texture.GetPixels32());
                }
                finally { Object.DestroyImmediate(texture); }
            }
        }
        finally { Object.DestroyImmediate(clone); }
    }

    private static void DecodeRaster(TSMPCodec codec, Texture source, byte[] expected)
    {
        codec.activeWidthBlocks = 80;
        codec.calibrationStartBlock = 400;
        codec.decodeStage = 2;
        codec.byteCount = expected.Length;
        codec.codecOptionBytes = codec.GetCodecOptionBytes();
        codec.ApplyDecodeOptions();
        Check(codec.selectedDecodeMaterial != null, "Decode material selected");
        var material = new Material(codec.selectedDecodeMaterial);
        var output = new RenderTexture(512, 1, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var readback = new Texture2D(512, 1, TextureFormat.RGBA32, false, true);
        var previous = RenderTexture.active;
        try
        {
            material.SetTexture("_MainTex", source);
            material.SetFloat("_BlockSize", 8);
            material.SetFloat("_SampleSize", 1);
            material.SetFloat("_StartBlock", codec.payloadStartRow * 80);
            material.SetFloat("_ByteCount", expected.Length);
            material.SetFloat("_ActiveWidthBlocks", 80);
            material.SetFloat("_SourceWidth", 640);
            material.SetFloat("_SourceHeight", 360);
            material.SetFloat("_OutputWidth", 512);
            material.SetFloat("_OutputHeight", 1);
            material.SetFloat("_FlipY", 1);
            output.Create();
            Graphics.Blit(source, output, material);
            RenderTexture.active = output;
            readback.ReadPixels(new Rect(0, 0, 512, 1), 0, 0);
            readback.Apply();
            var actual = new byte[expected.Length];
            Check(DecoderReadbackRuntime.TryCopyBytes(readback.GetPixels32(), actual, actual.Length, out int pixels, out string error), "GPU byte readback: " + error);
            int mismatch = -1;
            for (int i = 0; i < actual.Length; i++) if (actual[i] != expected[i]) { mismatch = i; break; }
            Check(mismatch < 0, "GPU payload matches all 1027 bytes; first mismatch=" + mismatch);
        }
        finally
        {
            RenderTexture.active = previous;
            output.Release();
            Object.DestroyImmediate(output);
            Object.DestroyImmediate(readback);
            Object.DestroyImmediate(material);
        }
    }

#if UDONSHARP
    public static void Udon()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        bool errors = false;
        Application.LogCallback listener = (text, stack, type) => { if (type == LogType.Error || type == LogType.Exception) errors = true; };
        Application.logMessageReceived += listener;
        try { UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { IsEditorBuild = false }); }
        finally { Application.logMessageReceived -= listener; }
        Check(!errors, "Full UdonSharp client compilation");
        AssetDatabase.SaveAssets();
        var template = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.kibalab.tsmp.core/Samples/TSMPController.prefab");
        var owner = (GameObject)PrefabUtility.InstantiatePrefab(template);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Validation/CodecVm.unity");
        var setup = owner.GetComponent<TSMPSetup>();
        setup.width = 640;
        setup.height = 360;
        SelectCodec(setup);
        ((TSMPEncoder)setup.encoder).autoEncode = false;
        ((TSMPDecoder)setup.decoder).applyEveryFrame = false;
        SessionState.SetBool("TSMP.CodecValidation", true);
        Attach();
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Attach()
    {
        if (!SessionState.GetBool("TSMP.CodecValidation", false)) return;
        EditorApplication.playModeStateChanged -= Entered;
        EditorApplication.playModeStateChanged += Entered;
    }

    private static void Entered(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        SessionState.SetBool("TSMP.CodecValidation", false);
        EditorApplication.playModeStateChanged -= Entered;
        EditorApplication.delayCall += () =>
        {
            try
            {
                var setup = Object.FindObjectOfType<TSMPSetup>();
                var codec = ((TSMPEncoder)setup.encoder).selectedCodec;
                Check(codec != null && codec.displayName == CodecName, "Prepared codec survives Play Mode");
                ((TSMPEncoder)setup.encoder).autoEncode = false;
                ((TSMPDecoder)setup.decoder).applyEveryFrame = false;
                TestRasterVariants(codec, TestVm);
                File.WriteAllText(Environment.GetEnvironmentVariable("TSMP_VALIDATION_RESULT"),
                    "PASS\nUnity=" + Application.unityVersion + "\nCodec=" + CodecName + "\nFull Udon client compile, automatic backing, actual VM raster parity and GPU byte decode");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                File.WriteAllText(Environment.GetEnvironmentVariable("TSMP_VALIDATION_RESULT"), "FAIL\n" + exception);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        };
    }

    private static void TestVm(TSMPCodec codec, byte[] bytes, Color32[] expected)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(Package + "/Runtime/Scripts/TSMPCodec" + CodecName + ".asset");
        asset.UpdateProgram();
        var program = asset.GetRealProgram();
        Check(program != null && program.ByteCode.Length > 0, "Compiled codec bytecode");
        var vm = UdonEditorManager.Instance.ConstructUdonVM();
        vm.LoadProgram(program);
        var backing = new GameObject("Codec VM").AddComponent<UdonBehaviour>();
        try
        {
            Check((bool)typeof(UdonBehaviour).GetMethod("ResolveUdonHeapReferences", Private).Invoke(backing, new object[] { program.SymbolTable, program.Heap }), "Udon heap references");
            typeof(UdonBehaviour).GetField("_program", Private).SetValue(backing, program);
            typeof(UdonBehaviour).GetField("_udonVM", Private).SetValue(backing, vm);
            typeof(UdonBehaviour).GetField("_udonManager", Private).SetValue(backing, UdonManager.Instance);
            foreach (var field in codec.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (field.FieldType.IsPrimitive && !field.IsInitOnly)
                    program.Heap.SetHeapVariable(program.SymbolTable.GetAddressFromSymbol(field.Name), field.GetValue(codec), field.FieldType);
            Set(program, "encoderRequestWidth", 640);
            Set(program, "encoderRequestHeight", 360);
            Set(program, "encoderRequestBlockSize", 8);
            Call(program, vm, nameof(TSMPCodec.OnTSMPEncoderQuery));
            int[] query = (int[])program.Heap.GetHeapVariable(program.SymbolTable.GetAddressFromSymbol("encoderQueryValues"));
            Check(query[EncoderCodecRuntime.QueryPayloadCapacityBytes] == codec.GetPayloadCapacityBytes(640, 360, 8), "Udon/native capacity matches");
            for (int mode = 0; mode < 2; mode++)
            {
                var pixels = new Color32[mode == 0 ? 640 * 360 : 80 * 45];
                Set(program, "encoderPixels", pixels);
                Set(program, "encoderPixelsAreBlocks", mode == 1);
                Set(program, "encoderPayloadBytes", bytes);
                Set(program, "encoderPayloadByteCount", bytes.Length);
                Call(program, vm, nameof(TSMPCodec.OnTSMPEncoderWritePayload));
                Check((bool)program.Heap.GetHeapVariable(program.SymbolTable.GetAddressFromSymbol("encoderWriteResult")), "Udon writer succeeds");
                int lastBlock = codec.GetPayloadStartRow(640, 8) * 80 + codec.GetPayloadBlocksForBytes(bytes.Length);
                for (int block = 400; block < lastBlock; block++)
                {
                    int x = block % 80;
                    int y = 44 - block / 80;
                    int actual = mode == 0 ? y * 8 * 640 + x * 8 : y * 80 + x;
                    var a = pixels[actual];
                    var b = expected[y * 8 * 640 + x * 8];
                    bool unused = block >= 400 + CalibrationCount(codec) && block < codec.GetPayloadStartRow(640, 8) * 80;
                    if (!unused && (a.r != b.r || a.g != b.g || a.b != b.b))
                        throw new InvalidOperationException("Udon/native pixel mismatch at block " + block + " mode=" + mode);
                }
                Debug.Log("[CodecValidation] PASS Udon/native payload parity, block texture=" + (mode == 1));
            }
        }
        finally { Object.DestroyImmediate(backing.gameObject); }
    }

    private static int CalibrationCount(TSMPCodec codec)
    {
        if (CodecName == "RGB20") return 320;
        if (CodecName == "Color256") return 256;
        return (1 << (int)codec.GetType().GetField("rBits").GetValue(codec))
            + (1 << (int)codec.GetType().GetField("gBits").GetValue(codec))
            + (1 << (int)codec.GetType().GetField("bBits").GetValue(codec));
    }

    private static void Set<T>(IUdonProgram program, string name, T value)
    {
        program.Heap.SetHeapVariable(program.SymbolTable.GetAddressFromSymbol(name), value, typeof(T));
    }

    private static void Call(IUdonProgram program, IUdonVM vm, string name)
    {
        vm.SetProgramCounter(program.EntryPoints.GetAddressFromSymbol(name));
        Check(vm.Interpret() == 0, "Udon event " + name);
    }
#endif
}
