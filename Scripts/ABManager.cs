using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class ABManager : Singleton<ABManager>
{
    AssetBundleManifest assetBundleManifest;  //加载资源依赖项
    Dictionary<string, BundleData> dicBundles = new Dictionary<string, BundleData>(); //存放加载资源包
    Dictionary<int,string> dicGameObject=new Dictionary<int, string>(); //存放所有预制体资源
    string AbPath;
    public ABManager()
    {
        AbPath = UpdateLoad.pPath; //Application.dataPath + "/ABTest/"
        AssetBundle assetBundle = AssetBundle.LoadFromFile(AbPath + "ABTest");
        assetBundleManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");//获取所有资源依赖项
        assetBundle.Unload(false); //卸载资源包
    }
    /// 加载资源和依赖项
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="abName"></param>
    /// <returns></returns>
    public T[] LoadAsset<T>(string abName) where T : UnityEngine.Object
    {
        string[] dependencies = assetBundleManifest.GetAllDependencies(abName);// 获取资源的所有依赖项的名字

        foreach (var item in dependencies)
        {
            //加载资源的依赖项
            if(!dicBundles.ContainsKey(item)) //判断存不存在这个资源 如果不存在加入资源字典
            {
                AssetBundle assetBundle = AssetBundle.LoadFromFile(AbPath + item);
                BundleData bundleData = new BundleData(assetBundle);
                dicBundles.Add(item, bundleData);
            }
            else
            {
                //存在 计数器++
                dicBundles[item].count++;
            }
        }

        //加载资源
        if (!dicBundles.ContainsKey(abName)) //判断存不存在这个资源 如果不存在加入资源字典
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(AbPath + abName);
            BundleData bundleData = new BundleData(assetBundle);
            dicBundles.Add(abName, bundleData);
        }
        else
        {
            //存在 计数器++
            dicBundles[abName].count++;
        }
        return dicBundles[abName].ab.LoadAllAssets<T>(); //返回所有资源
    }
    /// <summary>
    /// 加载预制体
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public GameObject LoadGameObject(string abName,string assetName)
    {
        Object obj = LoadOtherAsset<GameObject>(abName, assetName); //加载资源
        GameObject go = GameObject.Instantiate(obj) as GameObject;  // 实例化资源
        dicGameObject.Add(go.GetInstanceID(), abName); //把资源添加到字典中
        return go;
    }
    /// <summary>
    /// 获取图集里的某一个图片
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="assetName"></param>
    /// <param name="spritename"></param>
    /// <returns></returns>
    public Sprite LoadSprite(string abName, string assetName, string spritename)
    {
        SpriteAtlas spriteAtlas = LoadOtherAsset<SpriteAtlas>(abName, assetName); //获取图集某一个的图片
        return spriteAtlas.GetSprite(spritename);
    }
    /// <summary>
    /// 获取图集所有图片
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="assetName"></param>
    /// <param name="spritename"></param>
    /// <returns></returns>
    public Sprite[] LoadSprites(string abName, string assetName)
    {
        SpriteAtlas spriteAtlas = LoadOtherAsset<SpriteAtlas>(abName, assetName); //获取图集某一个的图片
        Sprite[] sprites = new Sprite[spriteAtlas.spriteCount];//图片集合
        spriteAtlas.GetSprites(sprites);//把集合里的图片和图集结合
        return sprites;
    }
    /// <summary>
    /// 加载其他资源（材质球/图片之类的）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="abName"></param>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public T LoadOtherAsset<T>(string abName,string assetName) where T:Object
    {
        UnityEngine.Object[] objects = LoadAsset<T>(abName); //加载资源
        Object obj = null;
        foreach (var item in objects)
        {
            if(item.name==assetName) //判断资源名字一样 就返回
            {
                obj = item;
                break;
            }
        }
        return obj as T;
    }
    /// <summary>
    /// 删除预制体
    /// </summary>
    /// <param name="go"></param>
    public void DestoryGameObject(GameObject go)
    {
        int id = go.GetInstanceID(); //获取到要卸载的唯一id
        string abName = dicGameObject[id];
        GameObject.Destroy(go); //删除预制体
        dicGameObject[id].Remove(id); //删除字典里的资源
        UnLoadAB(abName);
    }
    /// <summary>
    /// 删除资源时，要删除他的依赖项和Ab包
    /// </summary>
    /// <param name="abName"></param>
    public void UnLoadAB(string abName)
    {
        //先找到所有依赖项
        string[] dependencies = assetBundleManifest.GetAllDependencies(abName);

        //删除依赖项
        foreach (var item in dependencies)
        {
            if (dicBundles.ContainsKey(item))
            {
                dicBundles[item].count--;//如果存在 引用计数器--
                if (dicBundles[item].count <= 0)
                {
                    dicBundles[item].UnLoad(); 
                }
            }
        }
        //删除ab包
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
    public AssetBundle ab; //Ab包
    public int count; //所有用过的数量

    public BundleData(AssetBundle ab) //每次初始化的时候先给他们赋值
    {
        this.ab = ab;
        count = 1;
    }

    //卸载资源
    public void UnLoad()
    {
        ab.Unload(true);
    }
}
