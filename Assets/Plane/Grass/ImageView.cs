using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ImageView : MonoBehaviour
{
    [SerializeField] Material m_mat;
    public static ImageView Ins;
    private void Awake()
    {
        Ins = this;
        m_mat = GetComponent<Renderer>().material;
    }
    public void SetImageViewBuffer(ComputeBuffer buffer)
    {
        m_mat.SetBuffer("_ImageBuffer", buffer);
    }
    void Start()
    {
            
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
