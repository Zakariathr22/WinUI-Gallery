using Microsoft.Windows.ApplicationModel.WindowsAppRuntime;

namespace WinUIGallery.Helpers;
internal static class VersionHelper
{
    public static string WinAppSdkDetails => 
        $"Windows App SDK {ReleaseInfo.Major}.{ReleaseInfo.Minor}";

    public static string WinAppSdkRuntimeDetails => 
        WinAppSdkDetails + $", Windows App Runtime {RuntimeInfo.AsString}";  
}
