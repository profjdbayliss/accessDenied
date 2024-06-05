using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using Unity.Collections;
using System;

public struct TextureUV
{
    public string location;
    public int nameID;
    public int row;
    public int column;
}

public class TextureAtlas
{
    public const int SIZE = 256;
    public List<TextureUV> textureUVs;
    public static TextureAtlas instance = new TextureAtlas();
    public static readonly int pixelWidth = SIZE;
    public static readonly int pixelHeight = SIZE;
    public static int atlasHeight = 0;
    public static int atlasWidth = 0;
    public Texture2D atlas;
    public List<string> names;

    public void CreateAtlasComponentData(string directoryName, string outputFileName)
    {
        // Get all file names in this directory
        directoryName = Application.streamingAssetsPath + "\\" + directoryName;
        names = new List<string>(Directory.GetFiles(directoryName));

        List<string> tempNames = new List<string>();
        for (int i = 0; i < names.Count; i++)
        {
            if (!names[i].Contains(".meta"))
            {
                tempNames.Add(names[i]);
            }
        }
        names = tempNames;

        // make the list of uvs
        textureUVs = new List<TextureUV>(names.Count);


        // Debug: make sure we see the files
        //foreach(string s in images)
        //{
        //    Debug.Log(s);
        // }

        // we're going to assume our images are a power of 2 so we just
        // need to get the sqrt of the number of images and round up

        int squareRoot = Mathf.CeilToInt(Mathf.Sqrt(names.Count));
        int squareRootH = squareRoot;
        atlasWidth = squareRoot * pixelWidth;
        atlasHeight = squareRootH * pixelHeight;

        if (squareRoot * (squareRoot - 1) > names.Count)
        {
            squareRootH = squareRootH - 1;
            atlasHeight = squareRootH * pixelHeight;
        }

        // allocate space for the atlas and file data
        atlas = new Texture2D(atlasWidth, atlasHeight);
        byte[][] fileData = new byte[names.Count][];

        // read the file data in parallel
        Parallel.For(0, names.Count,
            index =>
        {
            if (names[index].Contains("meta") == false)
            {
                fileData[index] = File.ReadAllBytes(names[index]);
            }
        });

        // Put all the images into the image file and write
        // all the texture data to the texture uv map list.
        int x1 = 0;
        int y1 = 0;
        Texture2D temp = new Texture2D(pixelWidth, pixelHeight);
        float pWidth = (float)pixelWidth;
        float pHeight = (float)pixelHeight;
        float aWidth = (float)atlas.width;
        float aHeight = (float)atlas.height;

        for (int i = 0; i < names.Count; i++)
        {
            TextureUV currentUVInfo = new TextureUV
            {
                location = names[i],
                nameID = i,
                row = y1,
                column = x1
            };
            //UnityEngine.Debug.Log(currentUVInfo.location);
            textureUVs.Add(currentUVInfo);

            //UnityEngine.Debug.Log(i);
            temp.LoadImage(fileData[i]);
            atlas.SetPixels(x1 * pixelWidth, y1 * pixelHeight, pixelWidth, pixelHeight, temp.GetPixels());

            x1 = (x1 + 1) % squareRoot;
            if (x1 == 0)
            {
                y1++;
            }


        }

        //atlas.alphaIsTransparency = true;
        atlas.Apply();

        // write the atlas out to a file
        File.WriteAllBytes(outputFileName, atlas.EncodeToPNG());
    }

    public void CreateAtlasFromFilenameList(string directoryName, string outputFileName, List<string> filenames)
    {


        // Get all file names in this directory
        directoryName = Application.streamingAssetsPath + "\\" + directoryName;
        names = filenames;

        // make the list of uvs
        textureUVs = new List<TextureUV>(names.Count);


        // Debug: make sure we see the files
        //foreach(string s in images)
        //{
        //    Debug.Log(s);
        // }

        // we're going to assume our images are a power of 2 so we just
        // need to get the sqrt of the number of images and round up

        int squareRoot = Mathf.CeilToInt(Mathf.Sqrt(names.Count));
        int squareRootH = squareRoot;
        atlasWidth = squareRoot * pixelWidth;
        atlasHeight = squareRootH * pixelHeight;

        if (squareRoot * (squareRoot - 1) > names.Count)
        {
            squareRootH = squareRootH - 1;
            atlasHeight = squareRootH * pixelHeight;
        }

        // allocate space for the atlas and file data
        atlas = new Texture2D(atlasWidth, atlasHeight);
        byte[][] fileData = new byte[names.Count][];

        // read the file data in parallel
        Parallel.For(0, names.Count,
            index =>
            {
                if (!names[index].Equals(string.Empty) && !names[index].Equals("") )
                {
                    Debug.Log("names is : " + names[index] + " done.");
                    fileData[index] = File.ReadAllBytes(Application.streamingAssetsPath + "/" + names[index]);
                }
            });

        // Put all the images into the image file and write
        // all the texture data to the texture uv map list.
        int x1 = 0;
        int y1 = 0;
        Texture2D temp = new Texture2D(pixelWidth, pixelHeight);
        float pWidth = (float)pixelWidth;
        float pHeight = (float)pixelHeight;
        float aWidth = (float)atlas.width;
        float aHeight = (float)atlas.height;

        for (int i = 0; i < names.Count; i++)
        {

            if (!names[i].Equals(string.Empty) && !names[i].Equals(""))
            {
                TextureUV currentUVInfo = new TextureUV
                {
                    location = names[i],
                    nameID = i,
                    row = y1,
                    column = x1
                };
                Debug.Log("name: " + names[i] + " col: " + x1 + " row: " + y1);
                //UnityEngine.Debug.Log(currentUVInfo.location);
                textureUVs.Add(currentUVInfo);

                //UnityEngine.Debug.Log(i);
                temp.LoadImage(fileData[i]);
                atlas.SetPixels(x1 * pixelWidth, y1 * pixelHeight, pixelWidth, pixelHeight, temp.GetPixels());

                x1 = (x1 + 1) % squareRoot;
                if (x1 == 0)
                {
                    y1++;
                }

            }
        }

        //atlas.alphaIsTransparency = true;
        atlas.Apply();

        // write the atlas out to a file
        File.WriteAllBytes(outputFileName, atlas.EncodeToPNG());
    }
}
