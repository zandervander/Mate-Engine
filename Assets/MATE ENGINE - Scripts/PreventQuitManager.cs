using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class PreventQuitManager : MonoBehaviour
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    private const int GWL_WNDPROC = -4;
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WndProcDelegate windowProcedure = null;
    private IntPtr oldWndProc = IntPtr.Zero;
    private IntPtr gameWindowHandle = IntPtr.Zero;

    private const uint WM_QUERYENDSESSION = 0x0011;
    private const uint WM_ENDSESSION = 0x0016;
    private const uint WM_CLOSE = 0x0010;

    void Start()
    {
        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        SetupWindowInterception();
        #endif
    }

    private void SetupWindowInterception()
    {
        try
        {
            gameWindowHandle = FindWindow("UnityWndClass", null);
            
            if (gameWindowHandle == IntPtr.Zero)
            {
                Debug.LogWarning("Failed to find Unity window handle");
                return;
            }

            windowProcedure = new WndProcDelegate(WindowProcedure);
            oldWndProc = GetWindowLongPtr(gameWindowHandle, GWL_WNDPROC);
            SetWindowLongPtr(gameWindowHandle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(windowProcedure));
            
            Debug.Log("Window interception setup complete - app is now protected from Task Manager close");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to setup window interception: " + e.Message);
        }
    }

    private IntPtr WindowProcedure(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_CLOSE:
            case WM_QUERYENDSESSION:
            case WM_ENDSESSION:
                // Block all close attempts
                Debug.Log("Close attempt blocked");
                return IntPtr.Zero;
            
            default:
                // Call original window procedure
                return CallWindowProc(oldWndProc, hWnd, msg, wParam, lParam);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    void OnDestroy()
    {
        // Restore original window procedure if needed
        if (gameWindowHandle != IntPtr.Zero && oldWndProc != IntPtr.Zero)
        {
            try
            {
                SetWindowLongPtr(gameWindowHandle, GWL_WNDPROC, oldWndProc);
            }
            catch { }
        }
    }
}
