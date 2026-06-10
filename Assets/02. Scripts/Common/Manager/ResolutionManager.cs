using UnityEngine;

public class ResolutionManager : SingletonBehaviour<ResolutionManager>
{
    public int targetWidth = 1920;
    public int targetHeight = 1080;

    private bool isFullscreen = true;

    protected override void Init()
    {
        Screen.SetResolution(targetWidth, targetHeight, isFullscreen);
        base.Init();
    }

    private void Update()
    {
        if (Screen.fullScreen != isFullscreen)
        {
            isFullscreen = Screen.fullScreen;

            Screen.SetResolution(targetWidth, targetHeight, isFullscreen);
        }
    }

    public void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;
        Screen.SetResolution(targetWidth, targetHeight, isFullscreen);
    }
}