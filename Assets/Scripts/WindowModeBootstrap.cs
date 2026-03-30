using UnityEngine;

public static class WindowModeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void ForceWindowedMode()
    {
        var targetWidth = Mathf.Max(640, Screen.currentResolution.width / 2);
        var targetHeight = Mathf.Max(360, Screen.currentResolution.height / 2);

        Screen.SetResolution(targetWidth, targetHeight, FullScreenMode.Windowed);
    }
}
