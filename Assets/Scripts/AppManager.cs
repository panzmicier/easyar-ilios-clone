using UnityEngine;

public class AppManager : MonoBehaviour
{
    [SerializeField] GameObject imageTracker;
    [SerializeField] GameObject menuScreen01;
    [SerializeField] GameObject menuScreen02;
    [SerializeField] GameObject menuScreen03;

    void Update()
    {
        if (menuScreen01.activeSelf || menuScreen02.activeSelf || menuScreen03.activeSelf)
            imageTracker.gameObject.SetActive(false);
        else imageTracker.gameObject.SetActive(true);
    }

    public void CloseApp()
    {
        Application.Quit();
        Debug.Log("Application Closed");
    }

    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }
}
