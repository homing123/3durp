using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Position : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI T_CamPos;

    private void Update()
    {
        T_CamPos.text = Camera.main.transform.position.ToString();
    }

}
