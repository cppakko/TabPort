using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.TabPort.util;

public class WindowUtils
{
    private const string ChromiumClass = "Chrome_WidgetWin_1";
    private const string FirefoxClass = "MozillaWindowClass";

    private static readonly AndCondition SearchCondition = new(
        new OrCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane)),
        new OrCondition(
            new PropertyCondition(AutomationElement.ClassNameProperty, FirefoxClass),
            new PropertyCondition(AutomationElement.ClassNameProperty, ChromiumClass))
    );

    public static Dictionary<string, IntPtr> FetchWindows()
    {
        var result = new Dictionary<string, IntPtr>();
        var desktop = AutomationElement.RootElement;
        var windows = desktop.FindAll(TreeScope.Children, SearchCondition);

        foreach (AutomationElement window in windows)
        {
#if DEBUG
            Log.Info(
                $"[FetchWindows] Find {window.Current.Name} ({window.Current.ControlType.ProgrammaticName})({window.Current.NativeWindowHandle})",
                typeof(WindowUtils));
#endif
            if (window.Current.Name.EndsWith(" - Google Chrome") ||
                window.Current.Name.EndsWith(" - Opera") ||
                window.Current.Name.EndsWith(" — Mozilla Firefox"))
            {
                result[GetPureTitle(window.Current.Name)] = new IntPtr(window.Current.NativeWindowHandle);
            }
            else if (window.Current.Name.EndsWith(" - Microsoft\u200b Edge") ||
                     window.Current.Name.EndsWith(" - Microsoft Edge"))
            {
                var childResult = PrintChildren(window);
                if (childResult != null)
                {
                    result[GetPureTitle(childResult.Item1)] = new IntPtr(window.Current.NativeWindowHandle);
                }
            }
        }

        return result.Where(x => !RuntimeStaticData.SavedHWnd.ContainsKey(x.Value)).ToDictionary(x => x.Key, x => x.Value);
    }


    private static Tuple<string, IntPtr> PrintChildren(AutomationElement element)
    {
        var children = element.FindAll(TreeScope.Children, Condition.TrueCondition);
        foreach (AutomationElement child in children)
        {
            if (child.Current.Name.EndsWith(" - Microsoft\u200b Edge") ||
                child.Current.Name.EndsWith(" - Microsoft Edge"))
            {
#if DEBUG
                Log.Info(
                    $"[FetchWindows] Find {child.Current.Name} ({child.Current.ControlType.ProgrammaticName}) ({element.Current.NativeWindowHandle})",
                    typeof(WindowUtils));
#endif
                return Tuple.Create(child.Current.Name, new IntPtr(element.Current.NativeWindowHandle));
            }
        }

        return null;
    }

    private static string GetPureTitle(string windowTitle)
    {
        return windowTitle.EndsWith(" - Google Chrome") || windowTitle.EndsWith(" - Opera") ||
               windowTitle.EndsWith(" - Microsoft\u200b Edge") || windowTitle.EndsWith(" - Microsoft Edge")
            ? windowTitle[..windowTitle.LastIndexOf(" -", StringComparison.Ordinal)]
            : windowTitle[..windowTitle.LastIndexOf(" —", StringComparison.Ordinal)];
    }
}