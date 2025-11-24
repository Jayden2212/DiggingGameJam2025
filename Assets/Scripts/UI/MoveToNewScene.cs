using UnityEngine;

public class MoveToNewScene : MonoBehaviour
{
    public GameObject[] obj;

    void Start()
    {
        for (int i = 0; i < obj.Length; i++)
        {
            DontDestroyOnLoad(obj[i]);
        }
    }
}
