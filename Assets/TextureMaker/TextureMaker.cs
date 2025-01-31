using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TextureMaker : MonoBehaviour
{
    public enum E_MapType
    {
        PerlinNoise,
        Voronoi
    }
    [SerializeField] Image m_Image;
    [SerializeField] bool m_AutoUpdate;

    [SerializeField] int m_Width;
    [SerializeField] int m_Height;
    int m_LastFrameWidth;
    int m_LastFrameHeight;
    [SerializeField] E_MapType m_MapType;
    [SerializeField] PerlinNoise.PerlinOption m_PerlinOption;
    PerlinNoise.PerlinOption m_LastFramePerlinOption;

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
        if((m_Width != m_LastFrameWidth) || (m_Height != m_LastFrameHeight))
        {
            return true;
        }
        switch(m_MapType)
        {
            case E_MapType.PerlinNoise:
                return m_PerlinOption.isChanged(m_LastFramePerlinOption);
            case E_MapType.Voronoi:
                return false;
        }
        return false;
    }
    void SpriteUpdate()
    {
        if (m_Width * m_Height <= 0)
        {
            return;
        }

        Texture2D tex2D = new Texture2D(m_Width, m_Height);
        Color[] arr_Color = null;
        switch (m_MapType)
        {
            case E_MapType.PerlinNoise:
                arr_Color = PerlinNoise.CreatePerlinNoise2DBuffer(m_Width, m_Height, m_PerlinOption);
                m_LastFramePerlinOption = m_PerlinOption;
                break;
            case E_MapType.Voronoi:
                break;
        }
        
        tex2D.SetPixels(arr_Color);
        tex2D.Apply();

        Sprite sprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0.5f, 0.5f));
        m_Image.rectTransform.sizeDelta = new Vector2(m_Width, m_Height);
        m_Image.sprite = sprite;

        m_LastFrameWidth = m_Width;
        m_LastFrameHeight = m_Height;
    }    







    [ContextMenu("»ý¼º")]
    public void CreateTexture()
    {
        TexInfo info = new TexInfo();
        info.fileName = m_FileName;
        info.width = m_Width;
        info.height = m_Height;
        info.format = TextureFormat.RGBA32;
        info.type = E_TextureType.PNG;
        info.buffer = new Color[info.width * info.height];
        for (int y = 0; y < m_Height; y++)
        {
            for (int x = 0; x < m_Width; x++)
            {
                int idx = x + y * m_Width;
                float value = PerlinNoise.PerlinNoise2D(x * 0.07f, y * 0.07f);
                info.buffer[idx] = new Color(value, value, value, 1);
            }
        }
        Create(info, m_FilePath);
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
