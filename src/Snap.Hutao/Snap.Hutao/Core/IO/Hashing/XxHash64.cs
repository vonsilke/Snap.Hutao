// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.IO;

namespace Snap.Hutao.Core.IO.Hashing;

internal static class XxHash64
{
    public static async ValueTask<string> HashAsync(Stream stream, CancellationToken token = default)
    {
        System.IO.Hashing.XxHash64 xxHash64 = new();
        await xxHash64.AppendAsync(stream, token).ConfigureAwait(false);
        byte[] bytes = xxHash64.GetHashAndReset();
        return Convert.ToHexString(bytes);
    }

    public static async ValueTask<string> HashFileAsync(string path, CancellationToken token = default)
    {
        using (FileStream stream = File.OpenRead(path))
        {
            return await HashAsync(stream, token).ConfigureAwait(false);
        }
    }
}