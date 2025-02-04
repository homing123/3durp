using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    
    public enum InputMode
    { 
        Character,
        Camera
    }
    public static InputMode m_InputMode = InputMode.Character;

    public void ChangeInputMode(InputMode inputmode)
    {
        m_InputMode = inputmode;
    }
}


