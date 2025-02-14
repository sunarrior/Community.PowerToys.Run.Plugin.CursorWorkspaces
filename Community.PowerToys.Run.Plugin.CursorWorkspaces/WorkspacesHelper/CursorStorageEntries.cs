﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.CursorWorkspaces.WorkspacesHelper
{
    // v1.64 uses AppData\Roaming\Code\User\globalStorage\state.vscdb - history.recentlyOpenedPathsList
    public class CursorStorageEntries
    {
        [JsonPropertyName("entries")]
        public List<CursorWorkspaceEntry> Entries { get; set; }
    }
}
