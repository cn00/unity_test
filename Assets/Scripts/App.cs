﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class App : SingleMono<App>
{
    // Use this for initialization
    public override IEnumerator Init()
    {
        AppLog.d("App.Init 0");
        yield return LuaSys.Instance.Init();

        AppLog.d("App.Init 1");
        yield return AssetSys.Instance.Init();

        AppLog.d("App.Init 2");
        yield return base.Init();

        AppLog.d("App.Init 2");
        gameObject.GetComponent<LuaMonoBehaviour>().enabled = true;
    }

    private void Awake()
    {
        AppLog.d("App.Awake 0");
        StartCoroutine(Init());
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
        }
        catch(Exception e)
        {
            AppLog.e(e);
        }
    }
}
