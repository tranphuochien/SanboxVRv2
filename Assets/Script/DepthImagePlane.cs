using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthImagePlane : MonoBehaviour {


    private Texture2D tex;
    // the KinectManager instance
    private KinectManager manager;
    private KinectInterop.SensorData sensorData;

    // Use this for initialization
    void Start()
    {
        manager = KinectManager.Instance;
        if (manager && manager.IsInitialized())
        {
            sensorData = manager.GetSensorData();

        }
        tex = new Texture2D(manager.GetDepthImageWidth(), manager.GetDepthImageHeight(), TextureFormat.ARGB32, false);
        GetComponent<Renderer>().material.mainTexture = tex;
    }

    // Update is called once per frame
    void Update()
    {
        tex.SetPixels32(convertDepthToColor(manager.GetRawDepthMap()));

        tex.Apply(false);
    }

    private Color32[] convertDepthToColor(ushort[] depthBuf)
    {
        Color32[] img = new Color32[depthBuf.Length];
        for (int pix = 0; pix < depthBuf.Length; pix++)
        {
            img[pix].r = (byte)(depthBuf[pix] / 32);
            img[pix].g = (byte)(depthBuf[pix] / 32);
            img[pix].b = (byte)(depthBuf[pix] / 32);
        }
        return img;
    }

    private Color32[] convertPlayersToCutout(bool[,] players)
    {
        Color32[] img = new Color32[320 * 240];
        for (int pix = 0; pix < 320 * 240; pix++)
        {
            if (players[0, pix] | players[1, pix] | players[2, pix] | players[3, pix] | players[4, pix] | players[5, pix])
            {
                img[pix].a = (byte)255;
            }
            else
            {
                img[pix].a = (byte)0;
            }
        }
        return img;
    }
}
