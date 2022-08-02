using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class ABManager : Singleton<ABManager>
{
    AssetBundleManifest assetBundleManifest;  //������Դ������
    Dictionary<string, BundleData> dicBundles = new Dictionary<string, BundleData>(); //��ż�����Դ��
    Dictionary<int,string> dicGameObject=new Dictionary<int, string>(); //�������Ԥ������Դ
    string AbPath;
    public ABManager()
    {
        AbPath = UpdateLoad.pPath; //Application.dataPath + "/ABTest/"
        AssetBundle assetBundle = AssetBundle.LoadFromFile(AbPath + "ABTest");
        assetBundleManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");//��ȡ������Դ������
        assetBundle.Unload(false); //ж����Դ��
    }
    /// ������Դ��������
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="abName"></param>
    /// <returns></returns>
    public T[] LoadAsset<T>(string abName) where T : UnityEngine.Object
    {
        string[] dependencies = assetBundleManifest.GetAllDependencies(abName);// ��ȡ��Դ�����������������

        foreach (var item in dependencies)
        {
            //������Դ��������
            if(!dicBundles.ContainsKey(item)) //�жϴ治���������Դ ��������ڼ�����Դ�ֵ�
            {
                AssetBundle assetBundle = AssetBundle.LoadFromFile(AbPath + item);
                BundleData bundleData = new BundleData(assetBundle);
                dicBundles.Add(item, bundleData);
            }
            else
            {
                //���� ������++
                dicBundles[item].count++;
            }
        }

        //������Դ
        if (!dicBundles.ContainsKey(abName)) //�жϴ治���������Դ ��������ڼ�����Դ�ֵ�
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(AbPath + abName);
            BundleData bundleData = new BundleData(assetBundle);
            dicBundles.Add(abName, bundleData);
        }
        else
        {
            //���� ������++
            dicBundles[abName].count++;
        }
        return dicBundles[abName].ab.LoadAllAssets<T>(); //����������Դ
    }
    /// <summary>
    /// ����Ԥ����
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public GameObject LoadGameObject(string abName,string assetName)
    {
        Object obj = LoadOtherAsset<GameObject>(abName, assetName); //������Դ
        GameObject go = GameObject.Instantiate(obj) as GameObject;  // ʵ������Դ
        dicGameObject.Add(go.GetInstanceID(), abName); //����Դ��ӵ��ֵ���
        return go;
    }
    /// <summary>
    /// ��ȡͼ�����ĳһ��ͼƬ
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="assetName"></param>
    /// <param name="spritename"></param>
    /// <returns></returns>
    public Sprite LoadSprite(string abName, string assetName, string spritename)
    {
        SpriteAtlas spriteAtlas = LoadOtherAsset<SpriteAtlas>(abName, assetName); //��ȡͼ��ĳһ����ͼƬ
        return spriteAtlas.GetSprite(spritename);
    }
    /// <summary>
    /// ��ȡͼ������ͼƬ
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="assetName"></param>
    /// <param name="spritename"></param>
    /// <returns></returns>
    public Sprite[] LoadSprites(string abName, string assetName)
    {
        SpriteAtlas spriteAtlas = LoadOtherAsset<SpriteAtlas>(abName, assetName); //��ȡͼ��ĳһ����ͼƬ
        Sprite[] sprites = new Sprite[spriteAtlas.spriteCount];//ͼƬ����
        spriteAtlas.GetSprites(sprites);//�Ѽ������ͼƬ��ͼ�����
        return sprites;
    }
    /// <summary>
    /// ����������Դ��������/ͼƬ֮��ģ�
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="abName"></param>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public T LoadOtherAsset<T>(string abName,string assetName) where T:Object
    {
        UnityEngine.Object[] objects = LoadAsset<T>(abName); //������Դ
        Object obj = null;
        foreach (var item in objects)
        {
            if(item.name==assetName) //�ж���Դ����һ�� �ͷ���
            {
                obj = item;
                break;
            }
        }
        return obj as T;
    }
    /// <summary>
    /// ɾ��Ԥ����
    /// </summary>
    /// <param name="go"></param>
    public void DestoryGameObject(GameObject go)
    {
        int id = go.GetInstanceID(); //��ȡ��Ҫж�ص�Ψһid
        string abName = dicGameObject[id];
        GameObject.Destroy(go); //ɾ��Ԥ����
        dicGameObject[id].Remove(id); //ɾ���ֵ������Դ
        UnLoadAB(abName);
    }
    /// <summary>
    /// ɾ����Դʱ��Ҫɾ�������������Ab��
    /// </summary>
    /// <param name="abName"></param>
    public void UnLoadAB(string abName)
    {
        //���ҵ�����������
        string[] dependencies = assetBundleManifest.GetAllDependencies(abName);

        //ɾ��������
        foreach (var item in dependencies)
        {
            if (dicBundles.ContainsKey(item))
            {
                dicBundles[item].count--;//������� ���ü�����--
                if (dicBundles[item].count <= 0)
                {
                    dicBundles[item].UnLoad(); 
                }
            }
        }
        //ɾ��ab��
        if (dicBundles.ContainsKey(abName))
        {
            dicBundles[abName].count--;

            if (dicBundles[abName].count <= 0)
            {
                dicBundles[abName].UnLoad();
            }
        }
    }
}
public class BundleData
{
    public AssetBundle ab; //Ab��
    public int count; //�����ù�������

    public BundleData(AssetBundle ab) //ÿ�γ�ʼ����ʱ���ȸ����Ǹ�ֵ
    {
        this.ab = ab;
        count = 1;
    }

    //ж����Դ
    public void UnLoad()
    {
        ab.Unload(true);
    }
}
