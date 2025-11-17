using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;
using OpenNGS.Windows;
using OpenNGS.Windows.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RawInputListenService
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    const uint WM_INPUT = 0x00FF;

    static NativeWindow nativeWindow;

    public delegate void KeyboardEvent(KeyCode keyCode);

    public static event KeyboardEvent OnKeyUp;
    public static event KeyboardEvent OnKeyDown;

    public delegate void MouseEvent(MouseButton button, int x, int y);
    public static event MouseEvent OnMouseUp;
    public static event MouseEvent OnMouseDown;

    public delegate void MouseWheelEvent(int delta);
    public static event MouseWheelEvent OnMouseWheel;
    public static event MouseWheelEvent OnMouseHorizontalWheel;

    public delegate void MouseMoveEvent(Vector2 delta);
    public static event MouseMoveEvent OnMouseMove;

    public delegate void HidInputEvent(RawInputHidData hid);
    public static event HidInputEvent OnHidInput;

    public delegate void GamepadButtonEvent(int button);
    public static event GamepadButtonEvent OnGamepadButtonDown;
    public static event GamepadButtonEvent OnGamepadButtonUp;

    delegate void GamepadEvent();

    private static Dictionary<int, KeyCode> keymapping = new Dictionary<int, KeyCode>();

    private static bool serviceEnabled = false;

    public static void Start()
    {
        if (serviceEnabled) return;
        Debug.Log("RawInputListenService.Start()");
        nativeWindow = NativeWindow.CreateWindow("NativeWindow", "NativeWindow");
        NativeWindow.OnMessage += NativeWindow_OnMessage;
        Debug.Log("RawInputListenService.CreateWindow");

        KeyCodes.SetMappings(keymapping);

        var devices = RawInputDevice.GetDevices();
        var keyboards = devices.OfType<RawInputKeyboard>();
        foreach (var d in keyboards)
        {
            Debug.Log($"Keyboard:{d.DeviceType}{d.ManufacturerName}{d.ProductName}");
        }
        var mice = devices.OfType<RawInputMouse>();
        foreach (var d in mice)
        {
            Debug.Log($"Mouse:{d.DeviceType}{d.ManufacturerName}{d.ProductName}");
        }
        var pad = devices.OfType<RawInputHid>();
        foreach (var d in pad)
        {
            if (d != null)
                Debug.Log($"Gamepad:{d.DeviceType}{d.DevicePath}");
        }

        try
        {
            // Register the HidUsageAndPage to watch any device.
            RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard,
                RawInputDeviceFlags.InputSink, nativeWindow.m_Hwnd);
            RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse,
                RawInputDeviceFlags.InputSink, nativeWindow.m_Hwnd);
            RawInputDevice.RegisterDevice(HidUsageAndPage.GamePad,
                RawInputDeviceFlags.InputSink, nativeWindow.m_Hwnd);

            serviceEnabled = true;
        }
        catch (Exception ex)
        {
            Stop();
            Debug.LogException(ex);
        }
        Debug.Log("RawInputListenService.Start() Done");
    }

    static void NativeWindow_OnMessage(uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WM_INPUT)
        {
            ProcessRawInputMessage(lParam);
        }
    }

    static bool[] hidButtonStates = new bool[100];
    static bool[] hidKeytates = new bool[0xFF];

    static int oldX = int.MaxValue;
    static int oldY = int.MaxValue;
    static void ProcessRawInputMessage(IntPtr lParam)
    {
        var data = RawInputData.FromHandle(lParam);
        try
        {
            switch (data)
            {

                case RawInputMouseData mouse:

                    if ((mouse.Mouse.Flags & RawMouseFlags.MoveAbsolute) == RawMouseFlags.MoveAbsolute)
                    {
                        Rect rect = new Rect();
                        if ((mouse.Mouse.Flags & RawMouseFlags.VirtualDesktop) == RawMouseFlags.VirtualDesktop)
                        {
                            rect.xMin = Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN);
                            rect.yMin = Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);
                            rect.xMax = Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN);
                            rect.yMax = Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN);
                        }
                        else
                        {
                            rect.xMin = 0;
                            rect.yMin = 0;
                            rect.xMax = Screen.width;
                            rect.yMax = Screen.height;
                        }
                        int absoluteX = (int)(mouse.Mouse.LastX * (rect.xMax / Native.USHRT_MAX)) + (int)rect.xMin;
                        int absoluteY = (int)(mouse.Mouse.LastY * (rect.yMax / Native.USHRT_MAX)) + (int)rect.yMin;

                        if (oldX != int.MinValue && oldY != int.MinValue)
                        {
                            OnMouseMove?.Invoke(new Vector2(absoluteX - oldX, absoluteY - oldY));
                        }
                        oldX = absoluteX;
                    }
                    else
                    {
                        OnMouseMove?.Invoke(new Vector2(mouse.Mouse.LastX, mouse.Mouse.LastY));
                    }
                    if (mouse.Mouse.Buttons != RawMouseButtonFlags.None)
                    {
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.LeftButtonDown) == RawMouseButtonFlags.LeftButtonDown) OnMouseDown?.Invoke(MouseButton.Left, mouse.Mouse.LastX, mouse.Mouse.LastY);
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.LeftButtonUp) == RawMouseButtonFlags.LeftButtonUp) OnMouseUp?.Invoke(MouseButton.Left, mouse.Mouse.LastX, mouse.Mouse.LastY);
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.RightButtonDown) == RawMouseButtonFlags.RightButtonDown) OnMouseDown?.Invoke(MouseButton.Right, mouse.Mouse.LastX, mouse.Mouse.LastY);
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.RightButtonUp) == RawMouseButtonFlags.RightButtonUp) OnMouseUp?.Invoke(MouseButton.Right, mouse.Mouse.LastX, mouse.Mouse.LastY);
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.MiddleButtonDown) == RawMouseButtonFlags.MiddleButtonDown) OnMouseDown?.Invoke(MouseButton.Middle, mouse.Mouse.LastX, mouse.Mouse.LastY);
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.MiddleButtonUp) == RawMouseButtonFlags.MiddleButtonUp) OnMouseUp?.Invoke(MouseButton.Middle, mouse.Mouse.LastX, mouse.Mouse.LastY);
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.Button4Down) == RawMouseButtonFlags.Button4Down) OnMouseDown?.Invoke(MouseButton.Forward, mouse.Mouse.LastX, mouse.Mouse.LastY);
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.Button4Up) == RawMouseButtonFlags.Button4Up) OnMouseUp?.Invoke(MouseButton.Forward, mouse.Mouse.LastX, mouse.Mouse.LastY);
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.Button5Down) == RawMouseButtonFlags.Button5Down) OnMouseDown?.Invoke(MouseButton.Back, mouse.Mouse.LastX, mouse.Mouse.LastY);
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.Button5Up) == RawMouseButtonFlags.Button5Up) OnMouseUp?.Invoke(MouseButton.Back, mouse.Mouse.LastX, mouse.Mouse.LastY);
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.MouseWheel) == RawMouseButtonFlags.MouseWheel) OnMouseWheel?.Invoke(mouse.Mouse.ButtonData);
                        if ((mouse.Mouse.Buttons & RawMouseButtonFlags.MouseHorizontalWheel) == RawMouseButtonFlags.MouseHorizontalWheel) OnMouseHorizontalWheel?.Invoke(mouse.Mouse.ButtonData);
                    }
                    break;
                case RawInputKeyboardData keyboard:

                    int vk = keyboard.Keyboard.VirutalKey;
                    if (!keymapping.TryGetValue(vk, out var code))
                    {
                        code = (KeyCode)vk;
                    }

                    if ((keyboard.Keyboard.Flags & RawKeyboardFlags.Up) == RawKeyboardFlags.Up)
                    {
                        hidKeytates[vk] = false;
                        OnKeyUp?.Invoke(code);
                        break;
                    }
                    if ((keyboard.Keyboard.Flags & RawKeyboardFlags.None) == RawKeyboardFlags.None)
                    {
                        if (!hidKeytates[vk])
                        {
                            hidKeytates[vk] = true;
                            OnKeyDown?.Invoke(code);
                        }
                    }
                    break;
                case RawInputHidData hid:
                    {
                        OnHidInput?.Invoke(hid);
                        foreach (var bss in hid.ButtonSetStates)
                        {
                            foreach (var b in bss)
                            {
                                int btn = b.Button.UsageAndPage.Usage;
                                if (hidButtonStates[btn] != b.IsActive)
                                {
                                    hidButtonStates[btn] = b.IsActive;
                                    if (b.IsActive)
                                        OnGamepadButtonDown?.Invoke(btn);
                                    else
                                        OnGamepadButtonUp?.Invoke(btn);
                                }
                            }
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }


    public static void Stop()
    {
        if (!serviceEnabled) return;
        serviceEnabled = false;
        Debug.Log("RawInputListenService.Stop()");
        NativeWindow.OnMessage -= NativeWindow_OnMessage;
        nativeWindow.DestroyWindow();
        Debug.Log("RawInputListenService.Stop() Done");
    }

#endif // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
}