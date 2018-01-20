using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SandboxControler : MonoBehaviour {

    private int HEIGHT_KINECT = 424;
    private int WIDTH_KINECT = 512;
    static int MIN_DIMEN = 256;
    readonly int SKIP_FRAMES_MIN_MAX = 30;
    readonly int SKIP_FRAMES_MAPCOLOR = 10;
    readonly int SKIP_FRAMES_MAPHEIGHT = 5;
    readonly float NORMALIZE_RAW_DATA = 1500.0f;

    float[,] data;
    ushort[] DepthImage;
    private const int epsilon = 2;

    public int maxHeightMap = 100;
    public float heightOffset = 2.0f;
    private int checkToWriteFile = 200;
    private int countFrameMinMax = 0;
    private int countFrameMapColor = 0;
    private int countFrameMapHeight = 0;
    private float maxVal = 0;
    private float minVal = 0;
    private static int currentMax = 0;
    private GameObject mRain;
    private GameObject mWater;
    private float mMinYPosWater = -5f;
    private float mMaxYPosWater = 20f;
    private static bool isRaining;
    private const int DRAINAGE_INTERVAL = 20;
    private static int mDrainageCount = 0;
    private static int oldMax = 0;
    private static int oldMin = 0;
    private static bool isChanged = false;
    private static List<TreeInstance> treeList = new List<TreeInstance>(0);
    private static int placedTrees = 0;

    private KinectManager manager;
    private KinectInterop.SensorData sensorData;

    private void initKinectController()
    {
        manager = KinectManager.Instance;
        if (manager && manager.IsInitialized())
        {
            sensorData = manager.GetSensorData();
        }
    }
    // Use this for initialization
    void Start()
    {
        initKinectController();

        //MIN_DIMEN = Math.Min(HEIGHT_KINECT, WIDTH_KINECT);
        data = new float[MIN_DIMEN, MIN_DIMEN];
        /*mRain = this.gameObject.transform.GetChild(0).gameObject;
        mWater = this.gameObject.transform.GetChild(1).gameObject;*/
        //addTree(0, treeList, 0, 0, 256, 256);
        //GetComponent<Terrain>().terrainData.treeInstances = treeList.ToArray();
        //GetComponent<Terrain>().terrainData.SetHeights(0, 0, new float[,] { { } });
    }

    private void handleWater()
    {
        //mWater.transform.Translate(Vector3.up);

        /*if (mWater.transform.position.y > -5)
        {
            mWater.transform.Translate(-Vector3.up);
        }*/

        mDrainageCount = ++mDrainageCount % DRAINAGE_INTERVAL;
        float currentWaterY = mWater.transform.position.y;

        if (isRaining && currentWaterY < mMaxYPosWater)
        {
            if (mDrainageCount >= DRAINAGE_INTERVAL - 1)
            {
                mWater.transform.Translate(Vector3.up);
            }
        }
        if (!isRaining && currentWaterY > mMinYPosWater)
        {
            if (mDrainageCount >= DRAINAGE_INTERVAL - 1)
            {
                mWater.transform.Translate(-Vector3.up);
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        //handleWater();
        DepthImage = (ushort[]) sensorData.depthImage.Clone();
      
        loadDepthDataToTerrain(GetComponent<Terrain>().terrainData, DepthImage);
        Debug.Log("height: " + GetComponent<Terrain>().terrainData.heightmapHeight);
        Debug.Log("width: " + GetComponent<Terrain>().terrainData.heightmapWidth);
    }
    

    void loadDepthDataToTerrain(TerrainData tData, ushort[] rawData)
    {
        //int i = rawData.Length - 1;
        int i = 0;

        //Delay to update min max value
        if (countFrameMinMax == 0)
        {
            //maxVal = (float)rawData.Max() / NORMALIZE_RAW_DATA;
            minVal = (float)rawData.Min() / NORMALIZE_RAW_DATA;
            //Debug.Log("maxVal: " + maxVal + "minVal: " + minVal);
        }
        countFrameMinMax = (countFrameMinMax + 1) % SKIP_FRAMES_MIN_MAX;

        //Delay to render map
        if (countFrameMapHeight == 0)
        {
            for (int y = 0; y < MIN_DIMEN; y++) 
            {
                i = i + 170;
                for (int x = 0; x < MIN_DIMEN; x++)
                {
                    //flatten background
                    if (rawData[i] / NORMALIZE_RAW_DATA >= minVal && rawData[i] / NORMALIZE_RAW_DATA <= minVal + heightOffset)
                    {
                        data[y, x] = minVal;
                    } else
                    {
                        data[y,x] = 1 - (float)rawData[i] / NORMALIZE_RAW_DATA;
                    }
                    //data[y, x] = (y + x) / NORMALIZE_RAW_DATA;
                   
                    i++;
                }
                i = i + (512 - 256-170);
            }
            tData.size = new Vector3(MIN_DIMEN, maxHeightMap, MIN_DIMEN);
            tData.SetHeights(0, 0, data);
        }
        countFrameMapHeight = (countFrameMapHeight + 1) % SKIP_FRAMES_MAPHEIGHT;

        //WriteTerrainHeightMap(tData);
        //if (checkToWriteFile)
        //{
        //    checkToWriteFile = false;
        //    guestThresholdMapColor(tData);
        //}

        //Delay to render color
        if (countFrameMapColor == 0)
        {
            KeyValuePair<int, int> threshold = guestThresholdMapColor(tData);
            mapColor(tData, threshold);
        }
        countFrameMapColor = (countFrameMapColor + 1) % SKIP_FRAMES_MAPCOLOR;
        
    }

    private void mapColor(TerrainData terrainData, KeyValuePair<int, int> threshold)
    {
        int diffThreshold;


        /* if (currentMax != 0)
         {
             if (threshold.Value - currentMax <= epsilon)
             {
                 currentMax = threshold.Value;

                 //mRain.SetActive(false);
                 isRaining = false;
             }
             else
             {
                 //mRain.SetActive(true);
                 isRaining = true;
             }
             diffThreshold = currentMax - threshold.Key;
         }
         else
         {
             diffThreshold = threshold.Value - threshold.Key;
             currentMax = threshold.Value;
         }*/
        diffThreshold = threshold.Value - threshold.Key;
        Debug.Log("min: " + threshold.Key + "max: " + threshold.Value);

        if ((oldMax < threshold.Value - 2) || (oldMax > threshold.Value + 2) || (oldMin < threshold.Key - 3) || (oldMin > threshold.Key + 3) )
        {
            isChanged = true;
            oldMax = threshold.Value;
            oldMin = threshold.Key;
            if (treeList != null && treeList.Count != 0)
            {
                deleteAllTree();
            }
        }
        else
        {
            isChanged = false;
        }

        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Normalise x/y coordinates to range 0-1 
                float y_01 = (float)y / (float)terrainData.alphamapHeight;
                float x_01 = (float)x / (float)terrainData.alphamapWidth;

                float height = terrainData.GetHeight(y, x);

                // Setup an array to record the mix of texture weights at this point
                float[] splatWeights = new float[terrainData.alphamapLayers];

                handleSetWeights(height - threshold.Key, diffThreshold, 5, splatWeights, placedTrees, treeList, x, y, terrainData.heightmapHeight, terrainData.heightmapWidth);

                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                float z = splatWeights.Sum();

                // Loop through each terrain texture
                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {
                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= z;

                    // Assign this point to the splatmap array
                    splatmapData[x, y, i] = splatWeights[i];
                }
            }
        }

        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);
        terrainData.treeInstances = treeList.ToArray();
        terrainData.SetHeights(0, 0, new float[,] { { } });
    }

    private float[] handleSetWeights(double height, double diffThreshold, int nLayers, float[] splatWeights, int placedTrees, List<TreeInstance> treeList, int x, int y, int mapHeight, int mapWidth)
    {
        double a = height * 1.0f / (diffThreshold / 5.0f);

        for (int i = nLayers - 1; i >= 0; i--)
        {
            if (a >= i * 1.0f)
            {
                if (i != 0)
                {
                    double tmp = a - i * 1.0f;
                    splatWeights[i] = (float)tmp;
                    splatWeights[i - 1] = (float)(1.0 - tmp);
                    if (i == 2 && splatWeights[i] > 0.8 && isChanged)
                    {
                        addTree(placedTrees, treeList, x, y, mapHeight, mapWidth);
                    }
                }
                else
                {
                    splatWeights[i] = 1.0f;
                }
                break;
            }
        }
        return splatWeights;
    }

    void WriteTerrainHeightMap(TerrainData terrainData)
    {
        bool checkToWriteFile = false;
        if (checkToWriteFile)
        {
            checkToWriteFile = false;
            using (FileStream fs = new FileStream("a.txt", FileMode.CreateNew, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {

                for (int i = 0; i < terrainData.heightmapHeight; i++)
                {
                    for (int j = 0; j < terrainData.heightmapWidth; j++)
                    {
                        sw.Write(terrainData.GetHeight(i, j));
                        sw.Write(" ");
                    }
                    sw.WriteLine();
                }
                sw.Close();
            }
        }
    }

    private KeyValuePair<int, int> guestThresholdMapColor(TerrainData tData)
    {
        int[] mappingVal = new int[200];
        int min = 0, max = 0;

        for (int i = 0; i < tData.heightmapHeight; i++)
        {
            for (int j = 0; j < tData.heightmapWidth; j++)
            {
                float tmp = tData.GetHeight(i, j);
                mappingVal[(int)tmp]++;
            }
        }

        for (int i = 1; i < 200; i++)
        {
            if (mappingVal[i] != 0)
            {
                min = i;
                break;
            }
        }
        for (int i = 199; i > 0; i--)
        {
            if (mappingVal[i] != 0)
            {
                max = i;
                break;
            }
        }
        //using (FileStream fs = new FileStream("a.txt", FileMode.CreateNew, FileAccess.Write))
        //using (StreamWriter sw = new StreamWriter(fs))
        //{

        //    for (int i = 0; i < mappingVal.Length; i++)
        //    {
        //            sw.WriteLine(i + "   " + mappingVal[i]);   
        //    }
        //    sw.Close();
        //}
        return new KeyValuePair<int, int>(min, max);
    }

    private void deleteAllTree()
    {
        treeList.Clear();
        placedTrees = 0;
        GetComponent<Terrain>().terrainData.treeInstances = treeList.ToArray();
    }

    private void renderTree(TerrainData terrainData)
    {
        List<TreeInstance> treeList = new List<TreeInstance>(0);

        int placedTrees = 0;

        for (int i = 0; i < terrainData.heightmapHeight / 10; i++)
        {
            for (int j = 0; j < terrainData.heightmapWidth / 10; j++)
            {
                float percent = UnityEngine.Random.value;
                if (percent > 0.99f)
                {
                    placedTrees++;

                    //Vector3 treePos = new Vector3(0.0f + placedTrees / terrainData.heightmapWidth, 0.0f, 0.0f + placedTrees / terrainData.heightmapHeight);
                    Vector3 treePos = new Vector3(percent, 0.0f, percent);
                    TreeInstance tree = new TreeInstance();

                    tree.position = treePos;
                    tree.prototypeIndex = 0;
                    tree.color = new UnityEngine.Color(1, 1, 1);
                    tree.lightmapColor = new UnityEngine.Color(1, 1, 1);
                    tree.heightScale = 1;
                    tree.widthScale = 1;

                    treeList.Add(tree);
                }
            }
        }
        //run after the loop
        terrainData.treeInstances = treeList.ToArray();
        terrainData.SetHeights(0, 0, new float[,] { { } });
    }

    private List<TreeInstance> addTree(int placedTrees, List<TreeInstance> treeList, int x, int y, int mapHeight, int mapWidth)
    {
        float percent = UnityEngine.Random.value;
        if(percent > 0.99f)// (percent > 0.99f)
        {
            placedTrees++;

            Vector3 treePos = new Vector3(y * 1.0f / mapHeight, 0.0f, x * 1.0f / mapWidth);
            TreeInstance tree = new TreeInstance();

            tree.position = treePos;
            tree.prototypeIndex = 0;
            tree.color = new UnityEngine.Color(1, 1, 1);
            tree.lightmapColor = new UnityEngine.Color(1, 1, 1);
            tree.heightScale = 1;
            tree.widthScale = 1;

            treeList.Add(tree);
        }

        return treeList;
    }
}
