// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Community.PowerToys.Run.Plugin.CursorWorkspaces.CursorHelper;
using Microsoft.Data.Sqlite;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.CursorWorkspaces.WorkspacesHelper
{
    public class CursorWorkspacesApi
    {
        public CursorWorkspacesApi()
        {
        }

        private CursorWorkspace ParseCursorUriAndAuthority(string uri, string authority, CursorInstance cursorInstance, bool isWorkspace = false)
        {
            if (uri is null)
            {
                return null;
            }

            var rfc3986Uri = Rfc3986Uri.Parse(Uri.UnescapeDataString(uri));
            if (rfc3986Uri is null)
            {
                return null;
            }

            var (workspaceEnv, machineName) = ParseCursorAuthority.GetWorkspaceEnvironment(authority ?? rfc3986Uri.Authority);
            if (workspaceEnv is null)
            {
                return null;
            }

            var path = rfc3986Uri.Path;

            // Remove preceding '/' from local (Windows) path
            if (workspaceEnv == WorkspaceEnvironment.Local)
            {
                path = path[1..];
            }

            if (!DoesPathExist(path, workspaceEnv.Value))
            {
                return null;
            }

            var folderName = Path.GetFileName(path);

            // Check we haven't returned '' if we have a path like C:\
            if (string.IsNullOrEmpty(folderName))
            {
                DirectoryInfo dirInfo = new(path);
                folderName = dirInfo.Name.TrimEnd(':');
            }

            return new CursorWorkspace()
            {
                Path = uri,
                WorkspaceType = isWorkspace ? WorkspaceType.WorkspaceFile : WorkspaceType.ProjectFolder,
                RelativePath = path,
                FolderName = folderName,
                ExtraInfo = machineName,
                WorkspaceEnvironment = workspaceEnv ?? default,
                CursorInstance = cursorInstance,
            };
        }

        private bool DoesPathExist(string path, WorkspaceEnvironment workspaceEnv)
        {
            if (workspaceEnv == WorkspaceEnvironment.Local)
            {
                return Directory.Exists(path) || File.Exists(path);
            }

            // If the workspace environment is not Local or WSL, assume the path exists
            return true;
        }

        public List<CursorWorkspace> Workspaces
        {
            get
            {
                var results = new List<CursorWorkspace>();

                foreach (var cursorInstance in CursorInstances.Instances)
                {
                    // storage.json contains opened Workspaces
                    var cursor_storage = Path.Combine(cursorInstance.AppData, "storage.json");

                    // User/globalStorage/state.vscdb - history.recentlyOpenedPathsList
                    var cursor_storage_db = Path.Combine(cursorInstance.AppData, "User/globalStorage/state.vscdb");

                    if (File.Exists(cursor_storage))
                    {
                        var storageResults = GetWorkspacesInJson(cursorInstance, cursor_storage);
                        results.AddRange(storageResults);
                    }

                    if (File.Exists(cursor_storage_db))
                    {
                        var storageDbResults = GetWorkspacesInVscdb(cursorInstance, cursor_storage_db);
                        results.AddRange(storageDbResults);
                    }
                }

                return results;
            }
        }

        private List<CursorWorkspace> GetWorkspacesInJson(CursorInstance cursorInstance, string filePath)
        {
            var storageFileResults = new List<CursorWorkspace>();

            var fileContent = File.ReadAllText(filePath);

            try
            {
                CursorStorageFile cursorStorageFile = JsonSerializer.Deserialize<CursorStorageFile>(fileContent);

                if (cursorStorageFile != null && cursorStorageFile.OpenedPathsList != null)
                {
                    // for previous versions of Cursor
                    if (cursorStorageFile.OpenedPathsList.Workspaces3 != null)
                    {
                        foreach (var workspaceUri in cursorStorageFile.OpenedPathsList.Workspaces3)
                        {
                            var workspace = ParseCursorUriAndAuthority(workspaceUri, null, cursorInstance);
                            if (workspace != null)
                            {
                                storageFileResults.Add(workspace);
                            }
                        }
                    }

                    // Cursor v?
                    if (cursorStorageFile.OpenedPathsList.Entries != null)
                    {
                        foreach (var entry in cursorStorageFile.OpenedPathsList.Entries)
                        {
                            bool isWorkspaceFile = false;
                            var uri = entry.FolderUri;
                            if (entry.Workspace != null && entry.Workspace.ConfigPath != null)
                            {
                                isWorkspaceFile = true;
                                uri = entry.Workspace.ConfigPath;
                            }

                            var workspace = ParseCursorUriAndAuthority(uri, entry.RemoteAuthority, cursorInstance, isWorkspaceFile);
                            if (workspace != null)
                            {
                                storageFileResults.Add(workspace);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var message = $"Failed to deserialize {filePath}";
                Log.Exception(message, ex, GetType());
            }

            return storageFileResults;
        }

        private List<CursorWorkspace> GetWorkspacesInVscdb(CursorInstance CursorInstance, string filePath)
        {
            var dbFileResults = new List<CursorWorkspace>();
            SqliteConnection sqliteConnection = null;
            try
            {
                sqliteConnection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly;");
                sqliteConnection.Open();

                if (sqliteConnection.State == System.Data.ConnectionState.Open)
                {
                    var sqlite_cmd = sqliteConnection.CreateCommand();
                    sqlite_cmd.CommandText = "SELECT value FROM ItemTable WHERE key LIKE 'history.recentlyOpenedPathsList'";

                    var sqlite_datareader = sqlite_cmd.ExecuteReader();

                    if (sqlite_datareader.Read())
                    {
                        string entries = sqlite_datareader.GetString(0);
                        if (!string.IsNullOrEmpty(entries))
                        {
                            CursorStorageEntries cursorStorageEntries = JsonSerializer.Deserialize<CursorStorageEntries>(entries);
                            if (cursorStorageEntries.Entries != null)
                            {
                                cursorStorageEntries.Entries = cursorStorageEntries.Entries.Where(x => x != null).ToList();
                                foreach (var entry in cursorStorageEntries.Entries)
                                {
                                    bool isWorkspaceFile = false;
                                    var uri = entry.FolderUri;
                                    if (entry.Workspace != null && entry.Workspace.ConfigPath != null)
                                    {
                                        isWorkspaceFile = true;
                                        uri = entry.Workspace.ConfigPath;
                                    }

                                    var workspace = ParseCursorUriAndAuthority(uri, entry.RemoteAuthority, CursorInstance, isWorkspaceFile);
                                    if (workspace != null)
                                    {
                                        dbFileResults.Add(workspace);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var message = $"Failed to retrieve workspaces from db: {filePath}";
                Log.Exception(message, e, GetType());
            }
            finally
            {
                sqliteConnection?.Close();
            }

            return dbFileResults;
        }
    }
}
