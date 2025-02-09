﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.CursorWorkspaces.WorkspacesHelper
{
    public class CursorStorageFile
    {
        [JsonPropertyName("openedPathsList")]
        public OpenedPathsList OpenedPathsList { get; set; }
    }
}
