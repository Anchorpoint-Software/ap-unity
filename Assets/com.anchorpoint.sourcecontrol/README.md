# Anchorpoint plugin for Unity

This plugin allows you to commit files and view status and locked files directly from the Unity editor. You must have the Anchorpoint desktop application installed, an active Anchorpoint account and a project. For best performance, keep the Anchorpoint project open in the background so that the Unity plugin can detect file changes more quickly.

## Features
- Commit and revert added, modified or deleted file changes from within Unity
- View file status such as modified, added, obsolete and locked in the project window
- Open Anchorpoint from within a file in Unity to either view the history of the individual file or lock it manually

## Basic usage

Import the URL as a Unity package. Then open the Anchorpoint UI from the Window menu in Unity. You will need to connect to the Anchorpoint desktop application by clicking on the connect button. Changes detection is pretty instant when your Unity project in Anchorpoint is kept open. If you have another Anchorpoint project open, you will need to manually click the Refresh button to see the updated changes. Check the [documentation](https://docs.anchorpoint.app/docs/version-control/first-steps/unity/) for further details.


## Support & contribution

We are happy to help with any issues you may have and welcome any kind of contribution via a pull request. Just fork this [GitHub repository](https://github.com/Anchorpoint-Software/ap-unity). If you have issues with the plugin, ideas for features or other improvements, please join our [Discord](https://discord.com/invite/ZPyPzvx) server. 