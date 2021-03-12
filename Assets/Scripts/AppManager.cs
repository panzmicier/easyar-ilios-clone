using UnityEngine;
using easyar;

public class AppManager : MonoBehaviour
{
    [SerializeField] GameObject imageTracker;
    [SerializeField] GameObject imageTarget;
    [SerializeField] GameObject menuScreen01;
    [SerializeField] GameObject menuScreen02;
    [SerializeField] GameObject menuScreen03;
    [SerializeField] GameObject menuScreen04;
    [SerializeField] GameObject homepageButton;

    void Update()
    {
        if (menuScreen01.activeSelf || menuScreen02.activeSelf || menuScreen03.activeSelf)
            imageTracker.gameObject.SetActive(false);
        else imageTracker.gameObject.SetActive(true);

        if (imageTarget.activeSelf)
        {
            menuScreen04.gameObject.SetActive(false);
            homepageButton.gameObject.SetActive(true);
        }
        else if (!menuScreen01.activeSelf && !menuScreen02.activeSelf && !menuScreen03.activeSelf)
        {
            menuScreen04.gameObject.SetActive(true);
            homepageButton.gameObject.SetActive(false);
        }
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
