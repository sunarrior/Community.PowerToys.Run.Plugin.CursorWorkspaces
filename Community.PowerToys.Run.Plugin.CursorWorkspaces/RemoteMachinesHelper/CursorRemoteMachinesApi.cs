// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Community.PowerToys.Run.Plugin.CursorWorkspaces.SshConfigParser;
using Community.PowerToys.Run.Plugin.CursorWorkspaces.CursorHelper;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.CursorWorkspaces.RemoteMachinesHelper
{
    public class CursorRemoteMachinesApi
    {
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        public CursorRemoteMachinesApi()
        {
        }

        public List<CursorRemoteMachine> Machines
        {
            get
            {
                var results = new List<CursorRemoteMachine>();

                foreach (var cursorInstance in CursorInstances.Instances)
                {
                    // settings.json contains path of ssh_config
                    var cursor_settings = Path.Combine(cursorInstance.AppData, "User\\settings.json");

                    if (File.Exists(cursor_settings))
                    {
                        var fileContent = File.ReadAllText(cursor_settings);

                        try
                        {
                            JsonElement cursorSettingsFile = JsonSerializer.Deserialize<JsonElement>(fileContent, _serializerOptions);
                            if (cursorSettingsFile.TryGetProperty("remote.SSH.configFile", out var pathElement))
                            {
                                var path = pathElement.GetString();

                                if (File.Exists(path))
                                {
                                    foreach (SshHost h in SshConfig.ParseFile(path))
                                    {
                                        var machine = new CursorRemoteMachine();
                                        machine.Host = h.Host;
                                        machine.CursorInstance = cursorInstance;
                                        machine.HostName = h.HostName ?? string.Empty;
                                        machine.User = h.User ?? string.Empty;

                                        results.Add(machine);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var message = $"Failed to deserialize ${cursor_settings}";
                            Log.Exception(message, ex, GetType());
                        }
                    }
                }

                return results;
            }
        }
    }
}
