using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VersionData
{
    public string downlocalUrl;
    public string version;
    public int versionCode;
    public List<AssetData> assetDatas;
}
public class AssetData
{
    public string abName;
    public int len;
    public string md5;
}
