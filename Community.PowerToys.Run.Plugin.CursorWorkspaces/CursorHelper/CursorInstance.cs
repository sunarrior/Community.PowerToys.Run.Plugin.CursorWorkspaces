// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Community.PowerToys.Run.Plugin.CursorWorkspaces.CursorHelper
{
    public enum CursorVersion
    {
        Stable = 1,
    }

    public class CursorInstance
    {
        public CursorVersion CursorVersion { get; set; }

        public string ExecutablePath { get; set; } = string.Empty;

        public string AppData { get; set; } = string.Empty;

        public ImageSource WorkspaceIcon() => WorkspaceIconBitMap;

        public ImageSource RemoteIcon() => RemoteIconBitMap;

        public BitmapImage WorkspaceIconBitMap { get; set; }

        public BitmapImage RemoteIconBitMap { get; set; }
    }
}
