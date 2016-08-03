using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using task = System.Threading.Tasks.Task;

internal static class Logger
{
    private static string _name;
    private static Guid _guid = Guid.NewGuid();
    private static IVsOutputWindowPane _pane;
    private static IVsOutputWindow _outputWindow;

    public static void Initialize(IServiceProvider provider, string name)
    {
        _name = name;
        _outputWindow = (IVsOutputWindow)provider.GetService(typeof(SVsOutputWindow));
    }

    public static async task InitializeAsync(AsyncPackage package, string name)
    {
        _name = name;
        _outputWindow = await package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
    }

    public static void Log(object message)
    {
        try
        {
            if (EnsurePane())
            {
                ThreadHelper.Generic.BeginInvoke(() =>
                {
                    _pane.OutputStringThreadSafe(DateTime.Now + ": " + message + Environment.NewLine);
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Write(ex);
        }
    }

    public static void DeletePane()
    {
        if (_outputWindow != null)
            _outputWindow.DeletePane(_guid);
    }

    private static bool EnsurePane()
    {
        if (_pane == null)
        {
            ThreadHelper.Generic.Invoke(() =>
            {
                _outputWindow.CreatePane(ref _guid, _name, 1, 1);
                _outputWindow.GetPane(ref _guid, out _pane);
            });
        }

        return _pane != null;
    }
}