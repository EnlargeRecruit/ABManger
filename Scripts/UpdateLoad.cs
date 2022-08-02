using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Networking;
using UnityEngine.UI;
using XLua;
using System;
using UnityEngine.SceneManagement;

public class UpdateLoad : MonoBehaviour
{
   
    // Start is called before the first frame update
    void Start()
    {
        //判断有没有P目录 没有P目录创建一个
        if(!Directory.Exists(pPath))
        {
            Directory.CreateDirectory(pPath);
            StartCoroutine(Copy()); //将S目录的文件复制到P目录
        }
        else
        {
            StartCoroutine(CheckUpdate()); //如果有了 就开始更新
        }
    }

    IEnumerator Copy()
    {
        string streamingAssetPathVersion = SPath + "version.txt"; //获取S目录下的Version
        string versionContent = "";
        Debug.Log(streamingAssetPathVersion);
        //安卓获取version路径
        //创建每个文件夹或文件
#if UNITY_ANDROID
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(streamingAssetPathVersion);
        yield return unityWebRequest.SendWebRequest();
        if (unityWebRequest.result !=UnityWebRequest.Result.ConnectionError)
        {
            versionContent = unityWebRequest.downloadHandler.text;
        }
        else
        {
            Debug.Log(unityWebRequest.error);
        }

#else
        versionContent = File.ReadAllText(streamingAssetPathVersion); 
#endif
        //将获取的Version文件反序列化  将每个文件获取到
        VersionData versionData = JsonConvert.DeserializeObject<VersionData>(versionContent);
        
        for (int i = 0; i < versionData.assetDatas.Count; i++)
        {
            AssetData assetData=versionData.assetDatas[i]; 
            //Assets/StreamingAssets/ABTest/new material
            string sPath = SPath + assetData.abName; //S路径下文件的整个路径
            
            string p = pPath + assetData.abName;//P目录下的文件路径
            string dir=Path.GetDirectoryName(p);
            
            if (!Directory.Exists(dir)) //判断有没有这个文件 如果没有就创建
            {
                Directory.CreateDirectory (dir);
            }

            //将S文件Copy到P文件
#if UNITY_ANDROID
            UnityWebRequest unityWebRequest1 = UnityWebRequest.Get(sPath);
            yield return unityWebRequest1.SendWebRequest();
            if (unityWebRequest1.result != UnityWebRequest.Result.ConnectionError)
            {
                File.WriteAllBytes(p, unityWebRequest1.downloadHandler.data);
            }
            else
            {
                Debug.Log(unityWebRequest1.error);
            }

#else
            File.Copy(sPath, p);
#endif
        }
        File.WriteAllText(pPath + "/version.txt", versionContent); //将S目录的version复制到P目录

        yield return null;
        StartCoroutine(CheckUpdate());
    }

    // Update is called once per frame
    void Update()
    {
        action1?.Invoke();
    }
    IEnumerator CheckUpdate()
    {
        string localVersion = pPath + "version.txt";//本地P目录的Version

        string localVersionContent=File.ReadAllText(localVersion); 
        VersionData localVersionData=JsonConvert.DeserializeObject<VersionData>(localVersionContent); //反序列化

        Dictionary<string,AssetData> versionDic=new Dictionary<string, AssetData>(); //存放本地P目录下的Ab包
        for (int i = 0; i < localVersionData.assetDatas.Count; i++) 
        {
            //将本地文件添加到存放本地Ab包的字典中
            AssetData assetData = localVersionData.assetDatas[i];
            versionDic.Add(assetData.abName, assetData);
        }
        string remoteVersion = localVersionData.downlocalUrl + "version.txt";//获取要更新的version路径

        string remoteVersionContent = "";
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(remoteVersion);//获取阿帕奇服务器上的version内容
        yield return unityWebRequest.SendWebRequest(); //开始与远程服务器通信

        if (unityWebRequest.result!=UnityWebRequest.Result.ConnectionError) //判断下载的内容有没有报错
        {
            remoteVersionContent = unityWebRequest.downloadHandler.text; //开始下载 downloadHandler管理下载
        }
        //将要更新的Version反序列化
        VersionData remoteVersionData = JsonConvert.DeserializeObject<VersionData>(remoteVersionContent); 
        List<AssetData> updateList=new List<AssetData>();//存放要更新的Ab包

        //判断本地的和服务器上的版本号 看是否要更新
        if(localVersionData.versionCode < remoteVersionData.versionCode)
        {
            for (int i = 0; i < remoteVersionData.assetDatas.Count; i++)
            {
                AssetData assetData = remoteVersionData.assetDatas[i];
                //每一个都和本地比较 看看是否需要更新
                if(versionDic.ContainsKey(assetData.abName))
                {
                    //如果存在 看md5是否一样 不一样也要更新
                    if(versionDic[assetData.abName].md5 !=assetData.md5)
                    {
                        updateList.Add(assetData); //添加到要更新的集合中
                    }
                }
                else
                {
                    //如果不存在也要更新
                    updateList.Add(assetData);//添加到要更新的集合中
                }
            }
        }
        else
        {
            EnterGame();
            //版本号相等不需要更新
            yield return null;
        }

        //开始更新
        for (int i = 0; i < updateList.Count; i++)
        {
            string abName = updateList[i].abName; //获取要更新的包名
            UnityWebRequest updateAsset = UnityWebRequest.Get(remoteVersionData.downlocalUrl + abName);
            yield return updateAsset.SendWebRequest();
            if(updateAsset.result!=UnityWebRequest.Result.ConnectionError)//网络下载的东西有没有报错
            {
                string perPath = pPath + abName; //获取P文件的下的资源名字
                string fileName=Path.GetFileName(perPath); //获取文件名字
                string dir = Path.GetDirectoryName(perPath).Replace("\\", "/") + "/";
                if(!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir); //判断文件是否存在 不存在创建
                }
                File.WriteAllBytes(dir + fileName, updateAsset.downloadHandler.data); //给文件赋值
            }
        }
        File.WriteAllText(pPath + "version.txt", remoteVersionContent);  //更新P文件路径下的Version
        EnterGame();
    }
    Action action1;
    Action<string> sceneLoadFinish;
    //public Dropdown d;
    // Start is called before the first frame update

    private byte[] CustomLoader(ref string filepath) //可以进行Lua解密
    {
        string path = pPath + "/Lua/" + filepath + ".lua";

        byte[] datas = File.ReadAllBytes(path);//将lua文件加密
        for (int i = 0; i < datas.Length; i++)
        {
            datas[i] = (byte)~datas[i];
        }
        return datas;
       // return File.ReadAllBytes(path);
    }
    void EnterGame()  //游戏入口
    {
        DontDestroyOnLoad(gameObject);
        LuaEnv luaEnv = new LuaEnv(); //Lua入口
        luaEnv.AddLoader(CustomLoader);
        luaEnv.AddBuildin("RapidJson", XLua.LuaDLL.Lua.LoadRapidJson);
        luaEnv.DoString("require 'GameMain'"); //加载lua的脚本
        Action game = luaEnv.Global.Get<Action>("GameMain");
        game();
        action1 = luaEnv.Global.Get<Action>("Update");
        sceneLoadFinish = luaEnv.Global.Get<Action<string>>("SceneLoadFinish");
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }
    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        sceneLoadFinish?.Invoke(arg0.name);
    }
    public static string SPath //S路径
    {
        get
        {
#if UNITY_ANDROID
            return "jar:file://" + Application.dataPath + "!assets/ABTest/";
#else
        return Application.streamingAssetsPath + "/ABTest/";
#endif

        }
    }
    public static string pPath //P路径
    {
        get
        {
            return Application.persistentDataPath + "/ABTest/";
        }
    }
}
