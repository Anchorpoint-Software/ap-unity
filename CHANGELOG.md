# 1.0.5

- Fix the issue with dangling ap processes after domain reload

# 1.0.4

- Add a warning about the Play Mode

# 1.0.3

- Refresh opened scene or prefab after a revert

# 1.0.2

Add loading screen for startup state

# 1.0.1

Updating the license and preparing the package for FAB.

# 1.0.0

Initial version of the plugin, including connection to the Anchorpoint desktop application via a CLI, a dedicated commit window and the option to display file status in the project window.

- Open project state is not detected by Unity after importing files
- Added optimistic update for the status in the Project Window
- Show Modified-Outdated properly
- Handle conflict state
- Fix multi selection in the Anchorpoint Window
- Refresh the UI once it's unblocked during the commit process
- Remove console warnings and add a dedicated parameter for debug output
- Fix the refresh button, which did not changed the state when there are no changed files
- Fix the UI clipping issue and the text box size
- Add info about meta files
- Fix the wrong number of changed files
- Show a placeholder text when no changed files are being shown
- Do a connection check all 30 seconds
- Handle edge case for only modified meta files
- Disable the UI when committing and reverting
- Keep the refresh button enabled all the time
- Refresh Anchorpoint UI after a solved conflict state
