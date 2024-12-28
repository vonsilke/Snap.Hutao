// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Model;
using Snap.Hutao.Win32.Foundation;
using Snap.Hutao.Win32.UI.Input.KeyboardAndMouse;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using static Snap.Hutao.Win32.Kernel32;
using static Snap.Hutao.Win32.Macros;
using static Snap.Hutao.Win32.User32;

namespace Snap.Hutao.UI.Input.HotKey;

[Injection(InjectAs.Singleton)]
internal sealed partial class HotKeyOptions : ObservableObject, IDisposable
{
    private static readonly WaitCallback RunMouseClickRepeatForever = MouseClickRepeatForever;
    private static readonly WaitCallback RunKeyPressRepeatForever = KeyPressRepeatForever;

    private readonly HotKeyMessageWindow hotKeyMessageWindow;

    private bool isDisposed;

    public HotKeyOptions(IServiceProvider serviceProvider)
    {
        hotKeyMessageWindow = HotKeyMessageWindow.Create(OnHotKeyPressed);

        HWND hwnd = hotKeyMessageWindow.Hwnd;

        // The registration logic of hotkeys is done in this class
        // However, the key combination & state is stored in different combination classes.
        // If different combination classes have same key combination, this will cause several issues.
        // Registration/Unregistration is performed by the combination class.
        MouseClickRepeatForeverKeyCombination = new(
            serviceProvider,
            hwnd,
            SH.ViewPageSettingKeyShortcutAutoClickingHeader,
            SettingKeys.HotKeyMouseClickRepeatForever,
            100000);
        KeyPressRepeatForeverKeyCombination = new(
            serviceProvider,
            hwnd,
            SH.ViewPageSettingKeyShortcutAutoPressingHeader,
            SettingKeys.HotKeyKeyPressRepeatForever,
            100001);
    }

    public ImmutableArray<NameValue<VIRTUAL_KEY>> VirtualKeys { get; } = Input.VirtualKeys.HotKeyValues;

    public ImmutableArray<NameValue<VIRTUAL_KEY>> AllVirtualKeys { get; } = Input.VirtualKeys.Values;

    [ObservableProperty]
    public partial HotKeyCombination MouseClickRepeatForeverKeyCombination { get; set; }

    [ObservableProperty]
    public partial HotKeyCombination KeyPressRepeatForeverKeyCombination { get; set; }

    public void RegisterAll()
    {
        MouseClickRepeatForeverKeyCombination.Register();
        KeyPressRepeatForeverKeyCombination.Register();
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        MouseClickRepeatForeverKeyCombination.Dispose();
        KeyPressRepeatForeverKeyCombination.Dispose();

        hotKeyMessageWindow.Dispose();
    }

    private static INPUT CreateInputForMouseEvent(MOUSE_EVENT_FLAGS flags)
    {
        INPUT input = default;
        input.type = INPUT_TYPE.INPUT_MOUSE;
        input.Anonymous.mi.dwFlags = flags;
        return input;
    }

    private static INPUT CreateInputForKeyEvent(KEYBD_EVENT_FLAGS flags, VIRTUAL_KEY key)
    {
        INPUT input = default;
        input.type = INPUT_TYPE.INPUT_KEYBOARD;
        input.Anonymous.ki.dwFlags = flags;
        input.Anonymous.ki.wVk = key;
        return input;
    }

    [SuppressMessage("", "SH007")]
    private static unsafe void MouseClickRepeatForever(object? state)
    {
        CancellationToken token = (CancellationToken)state!;

        // We want to use this thread for a long time
        while (!token.IsCancellationRequested)
        {
            INPUT[] inputs =
            [
                CreateInputForMouseEvent(MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN),
                CreateInputForMouseEvent(MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP),
            ];

            if (SendInput(inputs.AsSpan(), sizeof(INPUT)) is 0)
            {
                Marshal.ThrowExceptionForHR(HRESULT_FROM_WIN32(GetLastError()));
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            Thread.Sleep(Random.Shared.Next(100, 150));
        }
    }

    [SuppressMessage("", "SH007")]
    private static unsafe void KeyPressRepeatForever(object? state)
    {
        CancellationToken token = (CancellationToken)state!;

        // We want to use this thread for a long time
        while (!token.IsCancellationRequested)
        {
            INPUT[] inputs =
            [
                CreateInputForKeyEvent(default, VIRTUAL_KEY.VK_F),
                CreateInputForKeyEvent(KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP, VIRTUAL_KEY.VK_F),
            ];

            if (SendInput(inputs.AsSpan(), sizeof(INPUT)) is 0)
            {
                Marshal.ThrowExceptionForHR(HRESULT_FROM_WIN32(GetLastError()));
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            Thread.Sleep(Random.Shared.Next(100, 150));
        }
    }

    private void OnHotKeyPressed(HotKeyParameter parameter)
    {
        if (parameter.Key is VIRTUAL_KEY.VK__none_)
        {
            // We have user reported issue that the key is exactly VK__none_.
            // Under normal circumstances, this should not happen.
            return;
        }

        if (MouseClickRepeatForeverKeyCombination.CanToggle(parameter))
        {
            MouseClickRepeatForeverKeyCombination.Toggle(RunMouseClickRepeatForever);
            return;
        }

        if (KeyPressRepeatForeverKeyCombination.CanToggle(parameter))
        {
            KeyPressRepeatForeverKeyCombination.Toggle(RunKeyPressRepeatForever);
            return;
        }
    }
}