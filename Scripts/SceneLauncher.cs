using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLauncher : MonoBehaviour
{
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public void LaunchNewUser()
    {
        StartCoroutine(AsyncLoad());
        IEnumerator AsyncLoad()
        {

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1);

            // Wait until the asynchronous scene fully loads
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            var ui = GameObject.FindObjectOfType<UIManager>();
            ui.Load(-1);
        }
    }

    public void LaunchExistingUser(InputField inputField)
    {
        StartCoroutine(AsyncLoad());
        IEnumerator AsyncLoad()
        {
            int id = -1;
            try
            {
                id = int.Parse(inputField.text);
            }
            catch
            {
                yield break;
            }
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1);

            // Wait until the asynchronous scene fully loads
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            var ui = GameObject.FindObjectOfType<UIManager>();
            ui.Load(id);
        }
    }
}
