using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TextureMaker : MonoBehaviour
{
    public enum E_MapType
    {
        PerlinNoise,
        WorleyNoise,
        Grid
    }
    [SerializeField] Image m_Image;
    [SerializeField] bool m_AutoUpdate;
    [SerializeField] bool m_UseGPU;
    [SerializeField] bool m_ToNormalMap;
    [SerializeField] E_MapType m_MapType;
    bool m_LastUseGPU;
    bool m_LastToNormalMap;
    E_MapType m_LastMapType;

    [SerializeField] PerlinNoise.PerlinOption m_PerlinOption;
    PerlinNoise.PerlinOption m_LastPerlinOption;
    [SerializeField] WorleyNoise.WorleyOption m_WorleyOption;
    WorleyNoise.WorleyOption m_LastWorleyOption;

    [Space(10)]
    [SerializeField] string m_FilePath;
    [SerializeField] string m_FileName;

    private void Start()
    {
        SpriteUpdate();
    }
    private void Update()
    {
        if (m_AutoUpdate && isChanged())
        {
            SpriteUpdate();
        }
    }
    bool isChanged()
    {
        if((m_LastMapType != m_MapType) || (m_UseGPU!= m_LastUseGPU) || (m_ToNormalMap!= m_LastToNormalMap))
        {
            return true;
        }

        switch(m_MapType)
        {
            case E_MapType.PerlinNoise:
                return m_PerlinOption != m_LastPerlinOption;
            case E_MapType.WorleyNoise:
                return m_WorleyOption != m_LastWorleyOption;
        }

        return false;
    }
    void SpriteUpdate()
    {
        Texture2D tex2D = null;
        Color[] arr_Color = null;
        ComputeBuffer cbuffer = null;
        switch (m_MapType)
        {
            case E_MapType.PerlinNoise:
                tex2D = new Texture2D(m_PerlinOption.width, m_PerlinOption.height);
                arr_Color = new Color[m_PerlinOption.width * m_PerlinOption.height];
                if (m_UseGPU)
                {
                    cbuffer = PerlinNoise.PerlinNoiseGPU(m_PerlinOption, PerlinNoise.E_PerlinBufferType.Color);
                    cbuffer.GetData(arr_Color);
                }
                else
                {
                    arr_Color = PerlinNoise.PerlinNoiseCPU(m_PerlinOption);
                }
                break;
            case E_MapType.WorleyNoise:
                tex2D = new Texture2D(m_WorleyOption.width, m_WorleyOption.height);
                arr_Color = new Color[m_WorleyOption.width * m_WorleyOption.height];
                if (m_UseGPU)
                {
                    cbuffer = WorleyNoise.WorleyNoiseGPU(m_WorleyOption, WorleyNoise.E_WorleyBufferType.Color);
                    cbuffer.GetData(arr_Color);
                }
                else
                {
                    arr_Color = WorleyNoise.WorleyNoiseCPU(m_WorleyOption);
                }
                break;
        }

        if (m_ToNormalMap)
        {
            if (m_UseGPU)
            {
                ComputeBuffer normalColorBuffer = NormalMapMaker.HeightMapToNormalMapGPU(tex2D.width, tex2D.height, cbuffer, NormalMapMaker.E_NormalBufferType.ColorToColor, new Vector2(tex2D.width, tex2D.height), NormalMapMaker.E_NormalSideType.Clamp);
                cbuffer.Release();
                normalColorBuffer.GetData(arr_Color);
                normalColorBuffer.Release();
            }
            else
            {
                arr_Color = NormalMapMaker.HeightMapToNormalMapCPU(tex2D.width, tex2D.height, arr_Color, new Vector2(tex2D.width, tex2D.height));
            }
        }
        else
        {
            if(m_UseGPU)
            {
                cbuffer.Release();
            }
        }
        tex2D.SetPixels(arr_Color);
        tex2D.Apply();

        Sprite sprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0.5f, 0.5f));
        m_Image.rectTransform.sizeDelta = new Vector2(tex2D.width, tex2D.height);
        m_Image.sprite = sprite;

        LastOptionUpdate();
    }    

    void LastOptionUpdate()
    {
        m_LastMapType = m_MapType;
        m_LastUseGPU = m_UseGPU;
        m_LastToNormalMap = m_ToNormalMap;
        switch (m_MapType)
        {
            case E_MapType.PerlinNoise:
                m_LastPerlinOption = m_PerlinOption;
                break;
            case E_MapType.WorleyNoise:
                m_LastWorleyOption = m_WorleyOption;
                break;

        }
    }






    [ContextMenu("»ý¼º")]
    public void CreateTexture()
    {
        //TexInfo info = new TexInfo();
        //info.fileName = m_FileName;
        //info.width = 512;
        //info.height = 512;
        //info.format = TextureFormat.RGBA32;
        //info.type = E_TextureType.PNG;
        //info.buffer = new Color[info.width * info.height];
        //for (int y = 0; y < 512; y++)
        //{
        //    for (int x = 0; x < 512; x++)
        //    {
        //        int idx = x + y * 512;
        //        float value = PerlinNoise.PerlinNoise2D(x * 0.07f, y * 0.07f);
        //        info.buffer[idx] = new Color(value, value, value, 1);
        //    }
        //}
        //Create(info, m_FilePath);
    }

    public const string BasicPath = "Assets/Test";
    public enum E_TextureType
    {
        PNG,
        JPG
    }
    static Dictionary<E_TextureType, string> D_TypeName = new Dictionary<E_TextureType, string>() { { E_TextureType.PNG, ".png" }, { E_TextureType.JPG, ".jpg" } };

    public struct TexInfo
    {
        public string fileName;
        public E_TextureType type;
        public int width;
        public int height;
        public Color[] buffer;
        public TextureFormat format;
    }
    public static async void Create(TexInfo info, string directoryPath = BasicPath)
    {
        Texture2D texture = new Texture2D(info.width, info.height);
        texture.name = info.fileName;
        texture.SetPixels(0, 0, info.width, info.height, info.buffer);
        texture.Apply();

        string filePath = Path.Combine(directoryPath, info.fileName + D_TypeName[info.type]);

        Debug.Log($"filePath : {filePath}. Create Start");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }


        byte[] data = null;
        switch(info.type)
        {
            case E_TextureType.PNG:
                data = texture.EncodeToPNG();
                break;
            case E_TextureType.JPG:
                data = texture.EncodeToJPG();
                break;
        }

        await File.WriteAllBytesAsync(filePath, data);

        Debug.Log($"filePath : {filePath}. Create Complete");
    }
}