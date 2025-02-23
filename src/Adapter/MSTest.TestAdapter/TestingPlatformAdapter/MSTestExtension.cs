﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !WINDOWS_UWP
namespace Microsoft.Testing.Platform.Extensions.MSTest;

internal sealed class MSTestExtension : IExtension
{
    public string Uid => nameof(MSTestExtension);

    public string DisplayName => "MSTest";

    public string Version => "3.2.0"; // TODO: Decide whether to read from assembly or use hardcoded string.

    public string Description => "MSTest Framework for Microsoft Testing Platform";

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);
}
#endif
