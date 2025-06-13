# Anchorpoint plugin for Unity

This plugin allows you to commit files and view status and locked files directly from the Unity editor. You must have the Anchorpoint desktop application installed and an active project. For best performance, keep the Anchorpoint project open in the background so that the Unity plugin can detect file changes more quickly.

## Features
- Visual file tree with checkboxes for staging files and folders
- Commit and Revert buttons directly inside Unity
- Automatic handling of `.meta` files
- Git status tracking and real-time UI updates
- Integration with file locking (prevents editing locked files)

## Getting started
1. Download the plugin from the [Anchorpoint documentation](https://docs.anchorpoint.app/docs/general/integrations/unity/)
2. Go to Window > Anchorpoint to open the plugin window.
3. Click Connect to Anchorpoint to pair with your active Anchorpoint project.
4. Review changed files and either commit or revert.

Note: If you switch projects in Anchorpoint, click the Refresh button in Unity to resync.
Check the [documentation](https://docs.anchorpoint.app/docs/version-control/first-steps/unity/) for further details.

## Compatibility
The plugin is compatible with the latest Unity 2022 LTS and above. Unity 2021 is not supported.

## Logger support
To enable or disable detailed logs from the plugin, use the global logger flag. It helps in debugging or silent operation based on your development needs:

<pre lang="markdown">
// Enable or disable logging
AnchorpointLogger.IsEnabled = true; // Enable logging
AnchorpointLogger.IsEnabled = false; // Disable logging
</pre>

All critical steps like CLI communication, status refresh, command execution, and UI state changes are logged when enabled.

## Contribution

We appreciate any kind of contribution via a pull request. If you have other ideas for features or other improvements, please join our [Discord](https://discord.com/invite/ZPyPzvx) server.
