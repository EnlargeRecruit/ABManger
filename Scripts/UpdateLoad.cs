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
        //�ж���û��PĿ¼ û��PĿ¼����һ��
        if(!Directory.Exists(pPath))
        {
            Directory.CreateDirectory(pPath);
            StartCoroutine(Copy()); //��SĿ¼���ļ����Ƶ�PĿ¼
        }
        else
        {
            StartCoroutine(CheckUpdate()); //������� �Ϳ�ʼ����
        }
    }

    IEnumerator Copy()
    {
        string streamingAssetPathVersion = SPath + "version.txt"; //��ȡSĿ¼�µ�Version
        string versionContent = "";
        Debug.Log(streamingAssetPathVersion);
        //��׿��ȡversion·��
        //����ÿ���ļ��л��ļ�
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
        //����ȡ��Version�ļ������л�  ��ÿ���ļ���ȡ��
        VersionData versionData = JsonConvert.DeserializeObject<VersionData>(versionContent);
        
        for (int i = 0; i < versionData.assetDatas.Count; i++)
        {
            AssetData assetData=versionData.assetDatas[i]; 
            //Assets/StreamingAssets/ABTest/new material
            string sPath = SPath + assetData.abName; //S·�����ļ�������·��
            
            string p = pPath + assetData.abName;//PĿ¼�µ��ļ�·��
            string dir=Path.GetDirectoryName(p);
            
            if (!Directory.Exists(dir)) //�ж���û������ļ� ���û�оʹ���
            {
                Directory.CreateDirectory (dir);
            }

            //��S�ļ�Copy��P�ļ�
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
        File.WriteAllText(pPath + "/version.txt", versionContent); //��SĿ¼��version���Ƶ�PĿ¼

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
        string localVersion = pPath + "version.txt";//����PĿ¼��Version

        string localVersionContent=File.ReadAllText(localVersion); 
        VersionData localVersionData=JsonConvert.DeserializeObject<VersionData>(localVersionContent); //�����л�

        Dictionary<string,AssetData> versionDic=new Dictionary<string, AssetData>(); //��ű���PĿ¼�µ�Ab��
        for (int i = 0; i < localVersionData.assetDatas.Count; i++) 
        {
            //�������ļ���ӵ���ű���Ab�����ֵ���
            AssetData assetData = localVersionData.assetDatas[i];
            versionDic.Add(assetData.abName, assetData);
        }
        string remoteVersion = localVersionData.downlocalUrl + "version.txt";//��ȡҪ���µ�version·��

        string remoteVersionContent = "";
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(remoteVersion);//��ȡ������������ϵ�version����
        yield return unityWebRequest.SendWebRequest(); //��ʼ��Զ�̷�����ͨ��

        if (unityWebRequest.result!=UnityWebRequest.Result.ConnectionError) //�ж����ص�������û�б���
        {
            remoteVersionContent = unityWebRequest.downloadHandler.text; //��ʼ���� downloadHandler��������
        }
        //��Ҫ���µ�Version�����л�
        VersionData remoteVersionData = JsonConvert.DeserializeObject<VersionData>(remoteVersionContent); 
        List<AssetData> updateList=new List<AssetData>();//���Ҫ���µ�Ab��

        //�жϱ��صĺͷ������ϵİ汾�� ���Ƿ�Ҫ����
        if(localVersionData.versionCode < remoteVersionData.versionCode)
        {
            for (int i = 0; i < remoteVersionData.assetDatas.Count; i++)
            {
                AssetData assetData = remoteVersionData.assetDatas[i];
                //ÿһ�����ͱ��رȽ� �����Ƿ���Ҫ����
                if(versionDic.ContainsKey(assetData.abName))
                {
                    //������� ��md5�Ƿ�һ�� ��һ��ҲҪ����
                    if(versionDic[assetData.abName].md5 !=assetData.md5)
                    {
                        updateList.Add(assetData); //��ӵ�Ҫ���µļ�����
                    }
                }
                else
                {
                    //���������ҲҪ����
                    updateList.Add(assetData);//��ӵ�Ҫ���µļ�����
                }
            }
        }
        else
        {
            EnterGame();
            //�汾����Ȳ���Ҫ����
            yield return null;
        }

        //��ʼ����
        for (int i = 0; i < updateList.Count; i++)
        {
            string abName = updateList[i].abName; //��ȡҪ���µİ���
            UnityWebRequest updateAsset = UnityWebRequest.Get(remoteVersionData.downlocalUrl + abName);
            yield return updateAsset.SendWebRequest();
            if(updateAsset.result!=UnityWebRequest.Result.ConnectionError)//�������صĶ�����û�б���
            {
                string perPath = pPath + abName; //��ȡP�ļ����µ���Դ����
                string fileName=Path.GetFileName(perPath); //��ȡ�ļ�����
                string dir = Path.GetDirectoryName(perPath).Replace("\\", "/") + "/";
                if(!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir); //�ж��ļ��Ƿ���� �����ڴ���
                }
                File.WriteAllBytes(dir + fileName, updateAsset.downloadHandler.data); //���ļ���ֵ
            }
        }
        File.WriteAllText(pPath + "version.txt", remoteVersionContent);  //����P�ļ�·���µ�Version
        EnterGame();
    }
    Action action1;
    Action<string> sceneLoadFinish;
    //public Dropdown d;
    // Start is called before the first frame update

    private byte[] CustomLoader(ref string filepath) //���Խ���Lua����
    {
        string path = pPath + "/Lua/" + filepath + ".lua";

        byte[] datas = File.ReadAllBytes(path);//��lua�ļ�����
        for (int i = 0; i < datas.Length; i++)
        {
            datas[i] = (byte)~datas[i];
        }
        return datas;
       // return File.ReadAllBytes(path);
    }
    void EnterGame()  //��Ϸ���
    {
        DontDestroyOnLoad(gameObject);
        LuaEnv luaEnv = new LuaEnv(); //Lua���
        luaEnv.AddLoader(CustomLoader);
        luaEnv.AddBuildin("RapidJson", XLua.LuaDLL.Lua.LoadRapidJson);
        luaEnv.DoString("require 'GameMain'"); //����lua�Ľű�
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
    public static string SPath //S·��
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
    public static string pPath //P·��
    {
        get
        {
            return Application.persistentDataPath + "/ABTest/";
        }
    }
}
