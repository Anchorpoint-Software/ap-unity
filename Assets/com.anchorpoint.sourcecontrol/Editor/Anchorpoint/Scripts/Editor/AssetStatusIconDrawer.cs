using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Anchorpoint.Parser;
using Anchorpoint.Constants;
using Anchorpoint.Events;
using Anchorpoint.Wrapper;

namespace Anchorpoint.Editor
{

    [InitializeOnLoad]
    public class AssetStatusIconDrawer
    {
        private static Dictionary<string, string> stagedFiles;
        private static Dictionary<string, string> notStagedFiles;
        private static Dictionary<string, string> lockedFiles;
        private static HashSet<string> outdatedFiles;

        private const string addIcon = "2354d2ae9e6644355b13247fe7bcf803";
        private const string modifyIcon = "f0d42548e9ac042fe8debb31f645886d";
        private const string lockMeIcon = "2b87ed061126442709ab8f989fdf1783";
        private const string outdatedIcon = "952dff34c8a314eebbaecdab2e41d89e";
        private const string conflictIcon = "5b2f35ebbd6964f6da7714f1096d5ff3";
        private const string modifiedOutdatedIcon = "92a029a406112a040ab98b0df6ac7cd7";
        private const string fallbackIcon = "813203bb4c32349fa8d051a2183c2ae1";

        private static string rootRelativePath;

        static AssetStatusIconDrawer()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;
            AnchorpointEvents.OnStatusUpdated += OnStatusUpdated;

            // Continuously access the PersistentReferences to keep them alive
            EditorApplication.update += KeepReferencesAlive;
        }

        private static void OnProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            if (!PluginInitializer.IsConnected)
            {
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            string commitPath = GetCommitPath(path);

            IconCache.Icons.TryGetValue(commitPath, out var existingIcon);

            // Always fetch and compare the latest icon
            CacheIconForCommitPath(commitPath);
            IconCache.Icons.TryGetValue(commitPath, out var latestIcon);

            // Update icon if it has changed
            if (existingIcon != latestIcon)
            {
                IconCache.Icons[commitPath] = latestIcon;
                EditorApplication.RepaintProjectWindow();
            }

            float bottomPadding = IsOneColumnLayout(selectionRect) ? 0f : 14f;
            float sidePadding = IsOneColumnLayout(selectionRect) ? 8f : 0f;

            if (latestIcon != null)
            {
                const float iconSize = 16f;
                Rect iconRect = new Rect(
                    selectionRect.x + selectionRect.width - iconSize - sidePadding,
                    selectionRect.y + selectionRect.height - iconSize - bottomPadding,
                    iconSize,
                    iconSize
                );

                GUI.DrawTexture(iconRect, latestIcon);
            }
        }

        private static void CacheIconForCommitPath(string commitPath)
        {
            string status = null;
            if (stagedFiles != null && stagedFiles.TryGetValue(commitPath, out var stagedStatus))
            {
                status = stagedStatus;
            }
            else if (notStagedFiles != null && notStagedFiles.TryGetValue(commitPath, out var notStagedStatus))
            {
                status = notStagedStatus;
            }

            if (status == "C")
            {
                CacheIcon(commitPath, LoadIcon(conflictIcon));
            }
            else if (outdatedFiles != null && outdatedFiles.Contains(commitPath))
            {
                // File is both outdated and modified
                CacheIcon(commitPath, status == "M" ? LoadIcon(modifiedOutdatedIcon): LoadIcon(outdatedIcon));
            }
            else if (lockedFiles != null && lockedFiles.TryGetValue(commitPath, out var lockingUserEmail))
            {
                string currentUserEmail = DataManager.GetCurrentUser()?.Email;
                if (!string.IsNullOrEmpty(currentUserEmail))
                {
                    if (string.Equals(lockingUserEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        // Locked by current user
                        CacheIcon(commitPath, LoadIcon(lockMeIcon));
                    }
                    else
                    {
                        // Locked by someone else
                        var fallback = LoadIcon(fallbackIcon);
                        CacheIcon(commitPath, fallback);

                        // Fetch user picture asynchronously
                        DataManager.GetUserPicture(lockingUserEmail, (texture) =>
                        {
                            if (texture != null)
                            {
                                // Make persistent and update cache
                                texture = MakeTexturePersistent(texture);
                                CacheIcon(commitPath, texture);
                            }
                            else
                            {
                                // Keep fallback if no picture
                                CacheIcon(commitPath, fallback);
                            }
                            EditorApplication.RepaintProjectWindow();
                        });
                    }
                }
            } 
            else if (status == "M")
            {
                CacheIcon(commitPath, LoadIcon(modifyIcon));
            }
            else if (status == "A")
            {
                CacheIcon(commitPath, LoadIcon(addIcon));
            }
        }

        private static void CacheIcon(string key, Texture2D icon)
        {
            if (icon == null) return;
            icon.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
            IconCache.Icons[key] = icon;
            if (!IconCache.PersistentReferences.Contains(icon))
            {
                IconCache.PersistentReferences.Add(icon);
            }
        }

        private static Texture2D LoadIcon(string GUID)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(GUID);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return tex;
        }

        private static Texture2D MakeTexturePersistent(Texture2D original)
        {
            // Do not recreate the texture. Just set hideFlags.
            if (original != null)
            {
                original.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
            }
            return original;
        }
        
        private static void OnStatusUpdated()
        {
            EditorApplication.delayCall += () =>
            {
                RefreshStatusData();
                EditorApplication.RepaintProjectWindow();
            };
        }

        private static void RefreshStatusData()
        {
            CLIStatus status = DataManager.GetStatus();
            
            if (status != null)
            {
                stagedFiles = status.Staged;
                notStagedFiles = status.NotStaged;
                lockedFiles = status.LockedFiles;
                outdatedFiles = DataManager.GetOutdatedFiles();

                var updatedFiles = new HashSet<string>(stagedFiles.Keys);
                updatedFiles.UnionWith(notStagedFiles.Keys);
                updatedFiles.UnionWith((IEnumerable<string>)lockedFiles.Keys ?? new HashSet<string>());
                updatedFiles.UnionWith(outdatedFiles ?? new HashSet<string>());
                
                var keysToRemove = new List<string>();

                foreach (var key in IconCache.Icons.Keys)
                {
                    if (!updatedFiles.Contains(key))
                    {
                        keysToRemove.Add(key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    if (IconCache.Icons.TryGetValue(key, out var tex))
                    {
                        IconCache.PersistentReferences.Remove(tex);
                    }
                    IconCache.Icons.Remove(key);
                }
            }
            else
            {
                stagedFiles = null;
                notStagedFiles = null;
                lockedFiles = null;
                outdatedFiles = null;
                IconCache.PersistentReferences.Clear();
                IconCache.Icons.Clear();
            }
        }

        private static bool IsOneColumnLayout(Rect selectionRect)
        {
            return selectionRect.width > 100;
        }

        private static string GetCommitPath(string path)
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            if (string.IsNullOrEmpty(rootRelativePath))
            {
                rootRelativePath = projectPath.Substring(CLIConstants.WorkingDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
            }

            string combinedPath = Path.Combine(rootRelativePath, path);
            string normalizedPath = combinedPath.Replace("\\", "/");
            return normalizedPath.TrimStart('/');
        }

        private static void KeepReferencesAlive()
        {
            if (!PluginInitializer.IsConnected)
            {
                return;
            }
            foreach (var tex in IconCache.PersistentReferences) {  }
        }
        
        // This function will do the optimistic update of the icons
        public static void MarkAssetAsModified(string commitPath)
        {
            // optimisticUpdateKeys.Add(commitPath);
            CacheIcon(commitPath, LoadIcon(modifyIcon));
            EditorApplication.RepaintProjectWindow();
        }
    }
}