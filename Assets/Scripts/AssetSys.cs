﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Events;
using XLua;

public static class AssetExtern
{
    public static T LoadABAsset<T>(this AssetBundle bundle, string subPath) where T : UnityEngine.Object
    {
        var asset = bundle.LoadAsset<T>(BundleConfig.BundleResRoot + subPath);
        AppLog.d(subPath);
        return asset;
    }

    public static T GetXml<T>(this AssetBundle bundle, string path)
    {
        var stream = bundle.LoadAsset<TextAsset>(path);
        var deserializer = new XmlSerializer(typeof(T));
        var xml = (T)deserializer.Deserialize(new MemoryStream(stream.bytes));
        return xml;
    }

    public static T GetXml<T>(this TextAsset text)
    {
        var deserializer = new XmlSerializer(typeof(T));
        var xml = (T)deserializer.Deserialize(new MemoryStream(text.bytes));
        return xml;
    }
}

public static class BundleHelper
{
    #region 压缩

    #region LZMA
    public const int kPropSize = SevenZip.Compression.Lzma.Encoder.kPropSize;
    public class CProgressInfo : SevenZip.ICodeProgress
    {
        public Int64 ApprovedStart;
        public Int64 InSize;
        public System.DateTime Time;
        public void Init() { InSize = 0; }
        public void SetProgress(Int64 inSize, Int64 outSize)
        {
            if(inSize >= ApprovedStart && InSize == 0)
            {
                Time = DateTime.UtcNow;
                InSize = inSize;
            }
        }
    }

    public static void CompressFileLZMA(string inFile, string outFile)
    {
        if(!File.Exists(inFile))
        {
            AppLog.e(inFile + " not found");
            return;
        }

        var outDir = Path.GetDirectoryName(outFile);
        if(!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }

        SevenZip.Compression.Lzma.Encoder coder = new SevenZip.Compression.Lzma.Encoder();
        FileStream inStream = new FileStream(inFile, FileMode.Open);
        FileStream outStream = new FileStream(outFile, FileMode.Create);

        // Write the encoder properties
        coder.WriteCoderProperties(outStream); // 5 byte

        // Write the decompressed file size.
        outStream.Write(BitConverter.GetBytes(inStream.Length), 0, sizeof(long)); // 8 byte

        // Encode the file.
        CProgressInfo progressInfo = new CProgressInfo();
        Int32 dictionary = 1 << 21;
        progressInfo.ApprovedStart = dictionary;
        progressInfo.Init();
        coder.Code(inStream, outStream, inStream.Length, -1, progressInfo);
        outStream.Flush();

        outStream.Close();
        inStream.Close();
    }

    public static string Md5(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] hash_byte = md5.ComputeHash(stream);

        var md5str = System.BitConverter.ToString(hash_byte);
        md5str = md5str.Replace("-", "");

        md5.Clear();
        return md5str;
    }

    public static string Md5(string fname)
    {
        FileStream inStream = new FileStream(fname, FileMode.Open);
        var md5str = Md5(inStream);
        inStream.Close();
        return md5str;
    }

    public static void DecompressFileLZMA(string inFile, string outFile)
    {
        FileStream input = new FileStream(inFile, FileMode.Open);
        FileStream output = new FileStream(outFile, FileMode.Create);

        DecompressFileLZMA(input, output);

        output.Close();
        input.Close();
    }


    public static void DecompressFileLZMA(Stream input, Stream output)
    {
        SevenZip.Compression.Lzma.Decoder coder = new SevenZip.Compression.Lzma.Decoder();
        input.Seek(0, SeekOrigin.Begin);
        output.Seek(0, SeekOrigin.Begin);

        // Read the decoder properties
        byte[] properties = new byte[kPropSize];
        input.Read(properties, 0, kPropSize);

        // Read in the decompress file size.
        byte[] fileLengthBytes = new byte[sizeof(long)];
        input.Read(fileLengthBytes, 0, 8);
        long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);

        // Decompress the file.
        coder.SetDecoderProperties(properties);
        coder.Code(input, output, input.Length, fileLength, null);
        output.Flush();

        output.Seek(0, SeekOrigin.Begin);
    }
    #endregion LZMA

    #endregion 压缩

}

public class DataObject : UnityEngine.Object
{
    public byte[] Data { get; protected set; }
    public DataObject(byte[] data)
    {
        Data = data;
    }
    public override string ToString()
    {
        return Encoding.UTF8.GetString(Data);
    }
}

[LuaCallCSharp]
public class AssetSys : SingleMono<AssetSys>
{
    static string mCacheRoot = "";
    /// <summary>
    /// Application.dataPath + "/AssetBundle/${PlatformName}/" 
    /// </summary>
    public static string CacheRoot
    {
        get
        {
            if(string.IsNullOrEmpty(mCacheRoot))
            {
                var cacheDirName = "AssetBundle/";
#if UNITY_EDITOR
#if UNITY_ANDROID
                cacheDirName += PlatformName(RuntimePlatform.Android) + "/";
                mCacheRoot = Application.dataPath + "/../" + cacheDirName;
#elif UNITY_IPHONE
                cacheDirName += PlatformName(RuntimePlatform.IPhonePlayer) + "/";
                mCacheRoot = Application.dataPath + "/../" + cacheDirName;
#endif
#else //!UNITY_EDITOR
#if UNITY_ANDROID
                mCacheRoot = Application.persistentDataPath + "/" + cacheDirName;
#elif UNITY_IPHONE
                mCacheRoot = Application.persistentDataPath + "/" + cacheDirName;
#elif UNITY_WINDOWS
                mCacheRoot = Application.streamingAssetsPath + "/../" + cacheDirName;
#else
                cacheDirName += PlatformName(RuntimePlatform.Android);
                CacheRoot = Application.streamingAssetsPath + "/../" + cacheDirName;
#endif
#endif
            }
            return mCacheRoot;
        }
    } // set in Runtime
    static string mHttpRoot = null;
    /// <summary>
    /// http://ip:port/path/to/root/platform/
    /// </summary>
    /// <value>The http root.</value>
    public static string HttpRoot
    {
        get
        {
            if(string.IsNullOrEmpty(mHttpRoot))
            {
				mHttpRoot = BundleConfig.Instance ().ServerRoot;// "http://10.23.114.141:8008/";
#if UNITY_EDITOR
                mHttpRoot += PlatformName(RuntimePlatform.Android);
#elif UNITY_ANDROID
                mHttpRoot += PlatformName(RuntimePlatform.Android);
#elif UNITY_IPHONE
                mHttpRoot += PlatformName(RuntimePlatform.IPhonePlayer);
#elif UNITY_WINDOWS
                mHttpRoot += PlatformName(RuntimePlatform.WindowsPlayer);
#else
                mHttpRoot += PlatformName(RuntimePlatform.Android);
#endif
                mHttpRoot += "/";
            }
            return mHttpRoot;
        }
    }

    public static string PlatformName(RuntimePlatform platform)
    {
        switch(platform)
        {
        case RuntimePlatform.Android:
            return "Android";
        case RuntimePlatform.IPhonePlayer:
            return "iOS";
        case RuntimePlatform.WindowsPlayer:
        case RuntimePlatform.WindowsEditor:
            return "Windows";
        case RuntimePlatform.OSXPlayer:
            return "OSX";
        default:
            return null;
        }
    }

    public class BundleGroup
    {
        public AssetBundleManifest Manifest = null;
        public Dictionary<string, AssetBundle> Bundles = new Dictionary<string, AssetBundle>();
        public void Unload(string name, bool unloadAllLoadedObjects = false)
        {
            AssetBundle outBundle = null;
            if(Bundles.TryGetValue(name, out outBundle))
            {
                outBundle.Unload(unloadAllLoadedObjects);
                Bundles.Remove(name);
            }
        }
        public void Clear(bool unloadAllLoadedObjects = false)
        {
            foreach(var i in Bundles.Keys)
            {
                Unload(i, unloadAllLoadedObjects);
            }
        }
    }

    Dictionary<string/*group*/, BundleGroup> mLoadedBundles = new Dictionary<string,BundleGroup>();

    public bool SysEnter()
    {
        if(!Directory.Exists(CacheRoot))
        {
            Directory.CreateDirectory(CacheRoot);
        }
        return true;
    }

    public override IEnumerator Init()
    {
        yield return GetBundle("ui/boot" + BundleConfig.BundlePostfix);
        yield return base.Init();
    }

    /// <summary>
    /// 同步方式加载资源, 用于加载少量小型资源
    /// </summary>
    public T GetAssetSync<T>(string assetSubPath) where T : UnityEngine.Object
    {
        var trim = new char[] { ' ', '.', '/' };
        assetSubPath = assetSubPath.upath().TrimStart(trim).TrimEnd(trim);
        var dirs = assetSubPath.Split('/');
        string bundleName = dirs[0] + '/' + dirs[1] + BundleConfig.BundlePostfix;
        var bundle = GetBundleSync(bundleName);
        T asset = null;
        if(bundle != null)
        {
            asset = bundle.LoadAsset<T>(BundleConfig.BundleResRoot + assetSubPath);
        }
        if(asset == null)
        {
            AppLog.w("[{0}({2}):{1}] not exist.", bundleName, BundleConfig.BundleResRoot + assetSubPath, bundle);
        }
        return asset;
    }

    public string GetBundlePath (string assetSubPath)
    {
        var dirs = assetSubPath.Split('/');
        string bundleName = dirs[0] + '/' + dirs[1] + BundleConfig.BundlePostfix;
        return bundleName;
    }

    /// <summary>
    /// 异步方式加载资源, 以加载后的 (Object)res 为参数调用 callBack 
    /// </summary>
    public IEnumerator GetAsset(string assetSubPath, Action<UnityEngine.Object> callBack = null)
    {
        yield return GetAsset<UnityEngine.Object>(assetSubPath, callBack);
    }

    public IEnumerator GetAsset<T>(string assetSubPath, Action<T> callBack = null) where T : UnityEngine.Object
    {
        UnityEngine.Object resObj = null;
#if UNITY_EDITOR
        if(BundleConfig.Instance().UseBundle)
#endif
        {
//            var trim = new char[] { ' ', '.', '/' };
            //assetSubPath = assetSubPath.upath().TrimStart(trim).TrimEnd(trim);
            var dirs = assetSubPath.Split('/');

            string manifestBundleName = dirs[0] + '/' + dirs[0];
            yield return GetBundle(manifestBundleName, (bundle) =>
            {
                var manifext = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                mLoadedBundles[dirs[0]].Manifest = manifext;
            });
            AppLog.d("load <Color=red>manifest</Color>: " + manifestBundleName);

            string bundleName = dirs[0] + '/' + dirs[1] + BundleConfig.BundlePostfix;
            yield return GetBundle(bundleName, (bundle) =>
            {
                // text
                var textPath = assetSubPath;
                if(textPath.IsText())
                {
                    if(textPath.EndsWith(".lua"))
                        textPath += ".txt";
                    var textAsset = bundle.LoadAsset<TextAsset>(BundleConfig.BundleResRoot + textPath);
                    resObj = new DataObject(textAsset.bytes);
                }
                else
                {
                    resObj = bundle.LoadAsset<T>(BundleConfig.BundleResRoot + assetSubPath);
                }
            });
            AppLog.d("from <Color=green>bundle</Color> : " + assetSubPath);
        }
#if UNITY_EDITOR
        // 编辑器从原始文件加载资源
        else
        {
            // text
            if(assetSubPath.IsText())
            {
                resObj = new DataObject(File.ReadAllBytes(BundleConfig.BundleResRoot + assetSubPath));
            }
            else
            {
                resObj = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(BundleConfig.BundleResRoot + assetSubPath);
            }
            AppLog.d("from <Color=green>file</Color>: " + assetSubPath);
        }
#endif
        if(callBack != null)
            callBack((T)resObj);
    }

    public AssetBundle GetBundleSync(string bundlePath)
    {
        if(string.IsNullOrEmpty(bundlePath))
        {
            AppLog.e("bundlePath [{0}] not correct.", bundlePath);
            return null;
        }

        string rootName = bundlePath.Substring(0, bundlePath.IndexOf('/'));
        BundleGroup bundleGroup = null;
        if(!mLoadedBundles.TryGetValue(rootName, out bundleGroup))
        {
            bundleGroup = mLoadedBundles[rootName] = new BundleGroup();
        }

        AssetBundle bundle = null;
        if(bundleGroup.Bundles.ContainsKey(bundlePath))
        {
            bundle = bundleGroup.Bundles[bundlePath];
        }
        else
        {
            var subPath = bundlePath;
            var cachePath = CacheRoot + "/" + subPath;

            bundle = AssetBundle.LoadFromFile(cachePath);
            bundleGroup.Bundles[bundlePath] = bundle;
            AppLog.w("GetBundleSync: {0}", bundlePath);
        }

        if(bundle == null)
        {
            AppLog.e("[{0}] did not download yet.", bundlePath);
        }
        return bundle;
    }

    /// <summary>
    /// AssetBundle 加载, 自动处理更新和依赖, 
    /// 以加载后的 AssetBundle 为参数调用 callBack 
    /// </summary>
    public IEnumerator GetBundle(string bundleName, Action<UnityEngine.AssetBundle> callBack = null)
    {
        string bundlePath = bundleName;
        if(string.IsNullOrEmpty(bundlePath))
        {
            AppLog.e("bundlePath [{0}] not correct.", bundlePath);
            yield break;
        }

        string groupName = bundlePath.Substring(0, bundlePath.IndexOf('/'));
        BundleGroup bundleGroup = null;
        if(!mLoadedBundles.TryGetValue(groupName, out bundleGroup))
        {
            bundleGroup = mLoadedBundles[groupName] = new BundleGroup();
        }

        AssetBundle bundle = null;
        if(bundleGroup.Bundles.ContainsKey(bundlePath))
        {
            bundle = bundleGroup.Bundles[bundlePath];
        }

        if(bundle != null)
        {
            if(callBack != null)
                callBack(bundle);
            yield break;
        }

        var version = BundleConfig.Instance().Version.ToString();
        var subPath = bundlePath;
        var cachePath = CacheRoot + subPath;
        var fileUrl = "file://" + cachePath;

        var isLocal = true;
        if(!File.Exists(cachePath)
        //|| UpdateSys.Instance.NeedUpdate(subPath)
        )
        {
            isLocal = false;
            fileUrl = HttpRoot + version + "/" + subPath + BundleConfig.CompressedExtension;
        }

        AppLog.d(fileUrl);
        yield return Www(fileUrl, (WWW www) =>
        {
            if(string.IsNullOrEmpty(www.error))
            {
                if(isLocal)
                {
                    bundleGroup.Bundles[bundlePath] = www.assetBundle;
                }
                else
                {
#if UNITY_EDITOR
                    AsyncSave(cachePath + BundleConfig.CompressedExtension, www.bytes, www.bytes.Length);
#endif
                    MemoryStream outStream = new MemoryStream();
                    BundleHelper.DecompressFileLZMA(new MemoryStream(www.bytes), outStream);

                    bundleGroup.Bundles[bundlePath] = AssetBundle.LoadFromMemory(outStream.GetBuffer());

                    AsyncSave(cachePath, outStream.GetBuffer(), outStream.Length);
                    //UpdateSys.Instance.Updated(subPath);
                }
            }
            else
            {
                AppLog.e(fileUrl + ": " + www.error);
            }
        });

        // Dependencies
        if(bundleGroup.Manifest != null)
        {
            var deps = bundleGroup.Manifest.GetAllDependencies(bundlePath);
            foreach(var i in deps)
            {
                AppLog.d("Dependencies: " + i);
                yield return GetBundle(i);
            }
        }

        if(callBack != null)
            callBack(bundleGroup.Bundles[bundlePath]);

        yield return null;
    }

    public static IEnumerator Www(string url, UnityAction<WWW> endCallback = null, UnityAction<float> progressCallback = null)
    {
        WWW www = new WWW(url);
        DateTime timeout = DateTime.Now + new TimeSpan(0, 0, 10);
        if(www != null)
        {
            while(!www.isDone && string.IsNullOrEmpty(www.error))
            {
                //yield return www;
                if(progressCallback != null)
                {
                    progressCallback(www.progress);
                    if(DateTime.Now > timeout && www.progress < 0.1f)
                    {
                        AppLog.d("<Colro=red>timeout: " + url + "</Color>");
                        break;
                    }
                }

                yield return null;
            }

            if(string.IsNullOrEmpty(www.error))
            {
                // 留给调用者选择是否存盘
                //if(url.Substring(0, 7) == "http://")
                //{
                //    AsyncSave(url.Replace(HttpRoot + "/" + CGameRoot.Instance.Version, CacheRoot), www.bytes);
                //}

                AppLog.d("loaded <Color=green>{0}</Color> OK", url);
                if(endCallback != null)
                {
                    endCallback(www);
                }
            }
            else
            {
                AppLog.e(url + ": " + www.error);
            }
        }
        www.Dispose();

        yield return null;
    }

    /// <summary>
    /// clean LoadedBundles, null group to clean all
    /// </summary>
    /// <param name="group"></param>
    public void UnloadGroup(string group = null, bool unloadAllLoadedObjects = false)
    {
        if(group == null)
        {
            foreach(var i in mLoadedBundles.Keys)
            {
                UnloadGroup(i, unloadAllLoadedObjects);
            }
        }
        else
        {
            BundleGroup outGroup = null;
            if(mLoadedBundles.TryGetValue(group, out outGroup))
                outGroup.Clear(unloadAllLoadedObjects);
        }
    }

    public void UnloadBundle(string path, bool unloadAllLoadedObjects = false)
    {
        var group = path.Substring(0, path.IndexOf('/'));
        BundleGroup outGroup = null;
        if(mLoadedBundles.TryGetValue(group, out outGroup) && outGroup != null)
        {
            outGroup.Unload(path, unloadAllLoadedObjects);
            AppLog.d("UnloadBundle: {0}, {1}", path, unloadAllLoadedObjects);
        }
    }

    // TODO: not complete
    public static T WwwSync<T>(string url) where T : UnityEngine.Object
    {
        WWW www = new WWW(url);
        while(!www.isDone && string.IsNullOrEmpty(www.error))
        {
            Thread.Sleep(1000);
        }
        UnityEngine.Object obj = new DataObject(www.bytes);
        www.Dispose();
        return (T)obj;
    }
    /// <summary>
    /// 在新线程中异步保存文件
    /// </summary>
    public static void AsyncSave(string fname, byte[] bytes, long Length)
    {
        var dir = Path.GetDirectoryName(fname);
        if(!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        FileStream writer = new FileStream(fname, FileMode.Create);
        writer.BeginWrite(bytes, 0, (int)Length, (IAsyncResult result) =>
        {
            FileStream stream = (FileStream)result.AsyncState;
            stream.EndWrite(result);
            stream.Close();
            stream.Dispose();
            AppLog.d("Saved:" + fname);
        }, writer);
    }
}
