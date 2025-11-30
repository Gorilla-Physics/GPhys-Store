# GPhys Store
Install GPhys, GPhys add-ons, and Gorilla Tag mods.

# Overview

GPhys Store is a desktop software based on Steam. It uses WPF. Its interface allows one to easily:
- Activate GPhys core using a product key.
- Add or delete GPhys add-ons.
- Locate the location of Gorilla Tag, Steam or Oculus.
- Display development during downloading and installing.

# Features

Checks against product key on the server.
Windows registry Locates Gorilla Tag in the Steam registry or the Oculus folders.
- in case it fails to do that, allow you to choose the folder.
- Displays a downloading progress bar.
- Downloads only required package of your version.

## Addon Management

- Displays a list of add-ons which listed by the server.
- Allows you to delete or add an add-on with a single click.
- Downloads your updates as it installs it or installs it off.
- Is aware of the add-ons that have been installed.

## User Interface

- Appeals visually as Steam, dark colours with blue isolations.
- has 2 tabs, GPhys Core and Add-ons.
- The design suits all screen sizes and presents mistakes in an easily comprehensible way.
- There is visual response to all the actions.

## Requirements

- Windows with .NET Framework 4.8
- Permission to search Windows registry.
- Gorilla Tag installed, either Steam or Oculus.
- Internet connection

## Installation

1. obtaining the code in the repo or by download.
2. Sln Opin Visual Studio.
3. Install the NuGet packages:
   - Newtonsoft.Json
4. Construction of project and commencement of the program.

# Usage

## First Launch

1. The program searches the Gorilla Tag folder separately.
2. In case it is not able to locate it, where GorillaTag.exe is can be selected.
3. In case you do not provide it even with a valid folder, the program shuts down.

## Installing GPhys

1. Go to the GPhys tab.
2. Enter your product key on the box.
3. Click Install GPhys.
4. Observes the progress bar and the messages.

## Managing Addons

1. Open the Addons tab.
2. List of add-ons Server.
3. Click Install or Uninstall to alter an add-on.
4. Click the refresh button on the press to refresh the list.

# Technical Details

#### Project Structure

- MainWindows.xaml - the graphical design.
-MainWindow.xaml.cs -the code that operates upon opening the app.
- App.xaml.cs - this is the launch program.
- GTLocator - a little utility that identifies the location of Gorilla Tag.
- AddonInfo- a data object indicating the information about an add-on and changing it when the information is altered.

#### Path Detection

- Ultraviolet search on the Windows registry to locate Steam.
- reads libraryfolders.vdf of other Steam libraries.
- Scans the default Oculus folder.
- In case no folder is available, allows you to browse.

## Download Management

- Background downloads are displayed and registered.
- You are shown and corrected on the errors.
- Files are verified and of the appropriate version.
- Folder of the files is created automatically.

## Addon System

- API provides optional data in the form of JSON.
- The program evaluates the existence of an add-on file in order to determine whether the file is present or not.
- UI does not require data to be manually updated.
- It is possible to install or uninstall several add-ons simultaneously.

## Dependencies

-Microsoft.Win32 -Read the registry and open file dialogs.
- Newtonsoft.Json -read and write JSON.
- System.Net.Http recipient of talk on the server on HTTP.
- System.Windows- WPF interface constructors.

## Error Handling

The app can warn about:
- Internet problems.
- Wrong product keys.
- Difficulties in reading or writing files.
- No Gorilla Tag found.
- Server not working.

## Styling

It has a custom Steam like appearance that has:

- Dark colours to the background.
- Blue highlights.
- Buttons that get changed when you hover on through them.
- Darkness to give something more depth.
- The Segoe UI font.