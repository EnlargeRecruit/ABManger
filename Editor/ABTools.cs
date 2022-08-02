using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class ABTools : Editor
{
    [MenuItem("Tools/ABTool")]
    static void Buid()
    {
        string outpath = Application.dataPath + "/ABTest/"; //打包路径
        string path = Application.dataPath + "/Res/";  //资源路径
        string[] filePaths = Directory.GetFiles(path, ".", SearchOption.AllDirectories); //获取资源下所有文件
        if (Directory.Exists(outpath))
        {
            Directory.Delete(outpath, true);
        }
        Directory.CreateDirectory(outpath);

        foreach (var file in filePaths)
        {
            if (Path.GetExtension(file).Contains(".meta")) continue; //如果后缀名是.meta就跳过

            string abName = string.Empty;

            string fileName = file.Replace(Application.dataPath, "Assets");

            AssetImporter assetImporter = AssetImporter.GetAtPath(fileName);
            abName = fileName.Replace("Assets/Res/", string.Empty);
            abName = abName.Replace("\\", "/");
            if (file.Contains("_Comm"))
            {
                abName = abName.Replace("/" + Path.GetFileName(file), string.Empty);
            }
            else
            {
                abName = abName.Replace(Path.GetExtension(file), string.Empty);
            }

            assetImporter.assetBundleName = abName; //设置打包名字

            //assetImporter.assetBundleVariant = "u3d"; //设置后缀名
        }
        BuildPipeline.BuildAssetBundles(outpath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);

        AssetDatabase.Refresh(); //刷新

    }

    static VersionData versionData = new VersionData();
    [MenuItem("Tools/MakeVersion")]
    static void MakeVersion()
    {
        //10.161.13.9
        versionData.downlocalUrl = "http://127.0.0.1/game/ABTest/";
        versionData.version = "1.0.0";
        versionData.versionCode = 1;

        if (versionData.assetDatas == null)
        {
            versionData.assetDatas = new List<AssetData>();
        }
        else
        {
            versionData.assetDatas.Clear();
        }

        string abPath = Application.dataPath + "/ABTest/";
        string[] filePaths = Directory.GetFiles(abPath, ".", SearchOption.AllDirectories);
        foreach (var file in filePaths)
        {
            if (Path.GetExtension(file).Contains(".meta") || Path.GetExtension(file).Contains(".manifest")) continue; //如果后缀名是.meta就跳过
            string abName = file.Replace("\\", "/");
            abName = abName.Replace(abPath, string.Empty);

            int len = File.ReadAllText(file).Length;
            string md5 = FileMD5(file);

            AssetData assetData = new AssetData();
            assetData.abName = abName;
            assetData.len = len;
            assetData.md5 = md5;

            versionData.assetDatas.Add(assetData);
        }
        string version = JsonConvert.SerializeObject(versionData);
        File.WriteAllText(abPath + "/version.txt", version);
    }
    static System.Text.StringBuilder sb = new System.Text.StringBuilder();  //字符串拼接 
    static string FileMD5(string filePath)
    {
        FileStream file = new FileStream(filePath, FileMode.Open);
        System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] bytes = md5.ComputeHash(file);
        file.Close(); //关流
        sb.Clear();
        for (int i = 0; i < bytes.Length; i++)
        {
            sb.Append(bytes[i].ToString("X2"));
        }
        return sb.ToString();
    }
    [MenuItem("Tools/CopyLua")]
    static void CopyLua()
    {
        string[] files = Directory.GetFiles(Application.dataPath + "/Lua/", ".", SearchOption.AllDirectories);//获取所有lua文件
        foreach (var file in files)
        {
            if (Path.GetFileName(file).Contains(".meta")) continue;
            string filepath = file.Replace(Application.dataPath, Application.dataPath + "/ABTest/");//目标路径
            string dir = Path.GetDirectoryName(filepath).Replace("\\", "/");//获取文件夹路径名字
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);//判断路径是否存在不存在的话创建
            byte[] datas = File.ReadAllBytes(file);//将lua文件加密
            for (int i = 0; i < datas.Length; i++)
            {
                datas[i] = (byte)~datas[i];
            }
            File.WriteAllBytes(filepath, datas);//拷贝
        }
    }

}
