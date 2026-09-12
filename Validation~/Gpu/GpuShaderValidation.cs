#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using K13A.TSMP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public sealed class GpuShaderValidation : MonoBehaviour
{
    const string Generated = "Assets/GpuShaderValidation/Generated";
    static string Mode => Environment.GetEnvironmentVariable("TSMP_GPU_MODE");
    static string CodecName => Environment.GetEnvironmentVariable("TSMP_GPU_CODEC") ?? "RGB16";
    static string Package => "Packages/com.kibalab.tsmp.codec." + CodecName.ToLowerInvariant();
    readonly List<string> results = new List<string>();

    public static void Run()
    {
        Directory.CreateDirectory(Generated);
        foreach(string path in Directory.GetFiles(Environment.GetEnvironmentVariable("TSMP_GPU_BASELINES"), "*.shader.txt"))
        {
            string name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
            string text = Regex.Replace(File.ReadAllText(path), "Shader \"[^\"]+\"", "Shader \"Hidden/Regression/" + name + "\"");
            File.WriteAllText(Generated + "/" + name + ".shader", text);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool("TSMP.GpuShaderValidation", true);
        Attach();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Attach()
    {
        EditorApplication.playModeStateChanged -= Entered;
        EditorApplication.playModeStateChanged += Entered;
    }

    static void Entered(PlayModeStateChange state)
    {
        if(state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("TSMP.GpuShaderValidation", false)) return;
        SessionState.SetBool("TSMP.GpuShaderValidation", false);
        new GameObject("GPU Shader Validation").AddComponent<GpuShaderValidation>();
    }

    IEnumerator Start()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
        ProfilerDriver.SetAreaEnabled(ProfilerArea.GPU, true);
        Profiler.enabled = true;
        new GameObject("Frame Pump").AddComponent<Camera>().cullingMask = 0;
        results.Add("Unity=" + Application.unityVersion + ", GPU=" + SystemInfo.graphicsDeviceName + ", API=" + SystemInfo.graphicsDeviceType);
        var stack = new Stack<IEnumerator>();
        stack.Push(Cases());
        while(stack.Count > 0)
        {
            IEnumerator current = stack.Peek();
            bool next;
            try { next = current.MoveNext(); }
            catch(Exception exception) { Finish(exception); yield break; }
            if(!next) { stack.Pop(); continue; }
            if(current.Current is IEnumerator nested) { stack.Push(nested); continue; }
            yield return current.Current;
        }
        Finish(null);
    }

    void Finish(Exception error)
    {
        results.Insert(0, error == null ? "PASS" : "FAIL " + error);
        string path = Environment.GetEnvironmentVariable("TSMP_GPU_RESULTS");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllLines(path, results);
        Debug.Log(string.Join("\n", results));
        Profiler.enabled = false;
        EditorApplication.Exit(error == null ? 0 : 1);
    }

    static void Set(TSMPCodec codec, string field, object value) => codec.GetType().GetField(field)?.SetValue(codec,value);

    IEnumerator Cases()
    {
        var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Package + "/Runtime/Codec_" + CodecName + ".prefab"));
        var codec = instance.GetComponent<TSMPCodec>();
        string[] shaders = CodecName == "RGB20" ? new[]{"TSMPDecodeRgb20Bytes"} : Mode == "G02" ?
            new[]{"TSMPDecodeRgb16RefineBytes", "TSMPDecodeRgb16VariableRefineBytes"} : new[]{"TSMPDecodeRgb16VariableBytes", "TSMPDecodeRgb16VariableRefineBytes"};
        int checks = 0;
        int baselineRoundTripFailures = 0;
        foreach(string shaderName in shaders)
        {
            var oldMaterial = new Material(AssetDatabase.LoadAssetAtPath<Shader>(Generated + "/" + shaderName + ".shader"));
            var newMaterial = new Material(AssetDatabase.LoadAssetAtPath<Shader>(Package + "/Runtime/Shaders/" + shaderName + ".shader"));
            foreach(Material m in new[]{oldMaterial,newMaterial}) { ShaderUtil.CompilePass(m,0,true); if(ShaderUtil.ShaderHasError(m.shader)) throw new Exception("Shader compile error: " + m.shader.name); }
            int[][] modes = CodecName == "RGB20" ? new[]{new[]{7,7,6}} : !shaderName.Contains("Variable") ? new[]{new[]{4,4,4}} : Mode == "G02" ? new[]{new[]{5,6,4},new[]{8,8,4}} :
                new[]{new[]{1,1,1},new[]{1,1,4},new[]{2,3,3},new[]{4,4,4},new[]{5,6,4},new[]{6,6,4},new[]{8,8,4}};
            foreach(int[] bits in modes)
            {
                Set(codec,"rBits",bits[0]);Set(codec,"gBits",bits[1]);Set(codec,"bBits",bits[2]);
                byte[] payload = Enumerable.Range(0,1027).Select(i=>(byte)(i*73+19)).ToArray();
                var source = new Texture2D(1280,720,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Point};
                if(!codec.TryWriteFrame(source,8,new byte[56],payload,out string error)) throw new Exception(error);
                var noisy = Noise(source);
                var flat = new Texture2D(1280,720,TextureFormat.RGBAFloat,false,true){filterMode=FilterMode.Point};
                flat.SetPixels(Enumerable.Repeat(new Color(.314159f,.314159f,.314159f,1),1280*720).ToArray());flat.Apply(false,false);
                var output = new RenderTexture(4096,1,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);output.Create();
                foreach(int radius in shaderName.Contains("Refine") ? new[]{1,2,3} : new[]{1})
                foreach(int sample in new[]{1,4,8})
                foreach(int count in new[]{0,1,2,3,4,5,7,16,31,56,257,1027})
                foreach(Texture input in new Texture[]{source,noisy,flat})
                {
                    foreach(Material m in new[]{oldMaterial,newMaterial}) Setup(m,count,codec.GetPayloadStartRow(1280,8)*160,sample,radius,bits);
                    Graphics.Blit(input,output,newMaterial);byte[] actual=Read(output);
                    bool roundTrip = payload.Take(count).SequenceEqual(actual.Take(count));
                    if(Mode=="G02" || bits.Sum()>=8)
                    {
                        Graphics.Blit(input,output,oldMaterial);byte[] expected=Read(output);
                        if(!expected.SequenceEqual(actual)) throw new Exception($"Shader outputs differ: {shaderName}, bits={string.Join("/",bits)}, sample={sample}, radius={radius}, count={count}, input={input==source}");
                    }
                    if(input==source && !roundTrip)
                    {
                        if(bits.Max()<8) throw new Exception($"Round-trip failed: {shaderName}, bits={string.Join("/",bits)}, sample={sample}, radius={radius}, count={count}");
                        baselineRoundTripFailures++;
                    }
                    if(actual.Skip(count).Any(value=>value!=0)) throw new Exception("Nonzero trailing bytes");
                    checks++;
                }
                if(bits.SequenceEqual(new[]{4,4,4}) || bits.SequenceEqual(new[]{5,6,4}) || CodecName=="RGB20")
                foreach(int sample in new[]{1,4}) foreach(int count in new[]{4,56,1027})
                {
                    foreach(Material m in new[]{oldMaterial,newMaterial}) Setup(m,count,codec.GetPayloadStartRow(1280,8)*160,sample,2,bits);
                    yield return Measure(shaderName+",sample="+sample+",bytes="+count,source,output,oldMaterial,newMaterial);
                }
                output.Release();foreach(Object item in new Object[]{source,noisy,flat,output})Object.Destroy(item);
            }
            Object.Destroy(oldMaterial);Object.Destroy(newMaterial);
        }
        Object.Destroy(instance);
        results.Add("GPU output cases="+checks);
        results.Add("Existing eight-bit-channel quantization round-trip failures="+baselineRoundTripFailures);
    }

    static Texture2D Noise(Texture2D source)
    {
        Color[] pixels=source.GetPixels();var random=new System.Random(419);
        for(int i=0;i<pixels.Length;i++) {Color c=pixels[i]; c.r=Mathf.Clamp01(c.r*.9431f+.0247f+(float)(random.NextDouble()-.5)*.012f); c.g=Mathf.Clamp01(c.g*.9613f+.0173f+(float)(random.NextDouble()-.5)*.012f); c.b=Mathf.Clamp01(c.b*.9217f+.0311f+(float)(random.NextDouble()-.5)*.012f);pixels[i]=c;}
        var texture=new Texture2D(source.width,source.height,TextureFormat.RGBAFloat,false,true){filterMode=FilterMode.Point};texture.SetPixels(pixels);texture.Apply(false,false);return texture;
    }

    static void Setup(Material m,int count,int start,int sample,int radius,int[] bits)
    {
        m.SetFloat("_BlockSize",8);m.SetFloat("_SampleSize",sample);m.SetFloat("_StartBlock",start);m.SetFloat("_ByteCount",count);
        m.SetFloat("_ActiveWidthBlocks",160);m.SetFloat("_SourceWidth",1280);m.SetFloat("_SourceHeight",720);m.SetFloat("_OutputWidth",4096);m.SetFloat("_OutputHeight",1);m.SetFloat("_FlipY",1);
        m.SetFloat("_Rgb16CalibrationStartBlock",800);m.SetFloat("_Rgb20CalibrationStartBlock",800);m.SetFloat("_RBits",bits[0]);m.SetFloat("_GBits",bits[1]);m.SetFloat("_BBits",bits[2]);m.SetFloat("_RefineRadius",radius);
    }

    static byte[] Read(RenderTexture target)
    {
        var request=AsyncGPUReadback.Request(target,0,TextureFormat.RGBA32);request.WaitForCompletion();
        if(request.hasError)throw new Exception("GPU readback failed");return request.GetData<byte>().ToArray();
    }

    IEnumerator Measure(string label,Texture source,RenderTexture output,Material baseline,Material candidate)
    {
        var sa=CustomSampler.Create("TSMP.Compare.A",true);var sb=CustomSampler.Create("TSMP.Compare.B",true);
        var a=new CommandBuffer();a.BeginSample(sa);a.Blit(source,output,baseline);a.EndSample(sa);
        var b=new CommandBuffer();b.BeginSample(sb);b.Blit(source,output,candidate);b.EndSample(sb);
        var ra=sa.GetRecorder();var rb=sb.GetRecorder();ra.enabled=true;rb.enabled=true;
        var av=new List<double>();var bv=new List<double>();
        for(int frame=0;frame<65;frame++)
        {
            if((frame&1)==0){Graphics.ExecuteCommandBuffer(a);Graphics.ExecuteCommandBuffer(b);}else{Graphics.ExecuteCommandBuffer(b);Graphics.ExecuteCommandBuffer(a);}
            if(frame>=20 && ra.gpuSampleBlockCount==1 && rb.gpuSampleBlockCount==1){av.Add(ra.gpuElapsedNanoseconds/1000.0);bv.Add(rb.gpuElapsedNanoseconds/1000.0);}
            yield return null;
        }
        a.Dispose();b.Dispose();
        if(av.Count<20 || av.All(value=>value<=0))throw new Exception("GPU timing unavailable");
        av.Sort();bv.Sort();string line=$"TIMING,{label},before_us={av[av.Count/2]:F3},after_us={bv[bv.Count/2]:F3},samples={av.Count}";results.Add(line);Debug.Log(line);
    }
}
#endif
