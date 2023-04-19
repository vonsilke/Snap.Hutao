﻿using Microsoft.Web.WebView2.Core;
using Snap.Hutao.Web.Bridge.Model;

namespace Snap.Hutao.Web.Bridge;

/// <summary>
/// HoYoLAB 签到页面JS桥
/// </summary>
[HighQuality]
internal sealed class SignInJSInterfaceOversea : MiHoYoJSInterface
{
    private const string RemoveRotationWarningScript = """
        let landscape = document.getElementById('mihoyo_landscape');
        landscape.remove();
        """;

    private readonly ILogger<MiHoYoJSInterface> logger;

    /// <inheritdoc cref="MiHoYoJSInterface(CoreWebView2, IServiceProvider)"/>
    public SignInJSInterfaceOversea(CoreWebView2 webView, IServiceProvider serviceProvider)
        : base(webView, serviceProvider)
    {
        logger = serviceProvider.GetRequiredService<ILogger<MiHoYoJSInterface>>();
        webView.DOMContentLoaded += OnDOMContentLoaded;
    }

    /// <inheritdoc/>
    public override JsResult<Dictionary<string, string>> GetHttpRequestHeader(JsParam param)
    {
        return new()
        {
            Data = new Dictionary<string, string>()
            {
                { "x-rpc-client_type", "2" },
                { "x-rpc-device_id",  Core.CoreEnvironment.HoyolabDeviceId },
                { "x-rpc-app_version", Core.CoreEnvironment.HoyolabOsXrpcVersion },
            },
        };
    }

    private void OnDOMContentLoaded(CoreWebView2 coreWebView2, CoreWebView2DOMContentLoadedEventArgs args)
    {
        // 移除“请旋转手机”提示所在的HTML元素
        coreWebView2.ExecuteScriptAsync(RemoveRotationWarningScript).AsTask().SafeForget(logger);
    }
}
