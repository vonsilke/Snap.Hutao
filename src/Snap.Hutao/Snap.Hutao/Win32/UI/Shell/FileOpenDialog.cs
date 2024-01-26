﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Win32.UI.Shell;

[Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
internal readonly struct FileOpenDialog
{
    internal static unsafe ref readonly Guid CLSID
    {
        get
        {
            ReadOnlySpan<byte> data = [0x9C, 0x5A, 0x1C, 0xDC, 0x8A, 0xE8, 0xDE, 0x4D, 0xA5, 0xA1, 0x60, 0xF8, 0x2A, 0x20, 0xAE, 0xF7];
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }
}