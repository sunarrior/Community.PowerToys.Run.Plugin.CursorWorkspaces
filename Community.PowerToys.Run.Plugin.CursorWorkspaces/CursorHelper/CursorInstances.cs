// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;

using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.CursorWorkspaces.CursorHelper
{
    public static class CursorInstances
    {
        private static readonly string _userAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static List<CursorInstance> Instances { get; set; } = new List<CursorInstance>();

        private static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        public static Bitmap BitmapOverlayToCenter(Bitmap bitmap1, Bitmap overlayBitmap)
        {
            int bitmap1Width = bitmap1.Width;
            int bitmap1Height = bitmap1.Height;
            bitmap1.SetResolution(144, 144);
            using Bitmap overlayBitmapResized = new Bitmap(overlayBitmap, new Size(bitmap1Width / 2, bitmap1Height / 2));

            float marginLeft = (float)((bitmap1Width * 0.7) - (overlayBitmapResized.Width * 0.5));
            float marginTop = (float)((bitmap1Height * 0.7) - (overlayBitmapResized.Height * 0.5));

            Bitmap finalBitmap = new Bitmap(bitmap1Width, bitmap1Height);
            using (Graphics g = Graphics.FromImage(finalBitmap))
            {
                g.DrawImage(bitmap1, Point.Empty);
                g.DrawImage(overlayBitmapResized, marginLeft, marginTop);
            }

            return finalBitmap;
        }

        // Gets the executablePath and AppData foreach instance of Cursor
        public static void LoadCursorInstances()
        {
            var environmentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
            environmentPath += (environmentPath.Length > 0 && environmentPath.EndsWith(';') ? string.Empty : ";") + Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var paths = environmentPath
                .Split(';')
                .Distinct()
                .Where(x => x.Contains("cursor", StringComparison.OrdinalIgnoreCase)
                    || x.Contains("Cursor", StringComparison.OrdinalIgnoreCase)).ToArray();

            foreach (var path in paths)
            {
                if (!Directory.Exists(path))
                {
                    continue;
                }

                var files = Directory.GetFiles(path)
                    .Where(x => x.Contains("cursor", StringComparison.OrdinalIgnoreCase)
                        && x.EndsWith("cursor", StringComparison.OrdinalIgnoreCase)).ToArray();

                // Remove the trailing backslash to always get the correct path
                var iconPath = Path.GetDirectoryName(path.TrimEnd('\\'));

                if (files.Length == 0)
                {
                    continue;
                }

                var file = files[0];
                var version = string.Empty;

                var instance = new CursorInstance
                {
                    ExecutablePath = file,
                };

                if (file.EndsWith("cursor", StringComparison.OrdinalIgnoreCase))
                {
                    version = "Cursor";
                    instance.CursorVersion = CursorVersion.Stable;
                }

                if (string.IsNullOrEmpty(version))
                {
                    continue;
                }

                // For Portable Cursor
                var portableData = Path.Join(iconPath, "data");
                instance.AppData = Directory.Exists(portableData) ? Path.Join(portableData, "user-data") : Path.Combine(_userAppDataPath, version);
                var cursorIconPath = Path.Join(iconPath, "..", "..", $"{version}.exe");
                if (!File.Exists(cursorIconPath))
                {
                    continue;
                }

                var cursorIcon = Icon.ExtractAssociatedIcon(cursorIconPath);

                if (cursorIcon == null)
                {
                    continue;
                }

                using var cursorIconBitmap = cursorIcon.ToBitmap();

                // Workspace
                using var folderIcon = (Bitmap)Image.FromFile(Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Images//folder.png"));
                using var bitmapFolderIcon = BitmapOverlayToCenter(folderIcon, cursorIconBitmap);
                instance.WorkspaceIconBitMap = Bitmap2BitmapImage(bitmapFolderIcon);

                // Remote
                using var monitorIcon = (Bitmap)Image.FromFile(Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Images//monitor.png"));
                using var bitmapMonitorIcon = BitmapOverlayToCenter(monitorIcon, cursorIconBitmap);
                instance.RemoteIconBitMap = Bitmap2BitmapImage(bitmapMonitorIcon);

                Log.Info("can go here", typeof(CursorInstances));

                Instances.Add(instance);
            }
        }
    }
}
