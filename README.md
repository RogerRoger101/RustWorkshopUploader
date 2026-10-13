# Rust Workshop Uploader

<img width="527" height="506" alt="R_WIU_image1" src="https://github.com/user-attachments/assets/06040b35-dc87-4e0f-be58-20e12bba0388" />

Windows program for publishing a Rust skin image to the Steam Workshop. Version 1.4.22. Re-coded October 2026 by Gattaca.

# Description
Multiple languages added just click on your languages.  <br>
Application will remember the language you picked. <br>
Windows program for publishing a Rust skin image to the Steam Workshop. <br>
ALL my code will be publish via a LLM for uploading.  Why upload when you have AI agents? <br>
My code was double checked through two different LLM for any mistakes.
<br><br>
Steam has to be installed. The program looks for the Steam client on every fixed drive, including installs that are not in `C:\Program Files (x86)\Steam`. <br>
If Steam is not already running, it starts `steam.exe` and waits for it. You need to be signed in to the Steam account the workshop item should belong to.

## Run

The folder that contains `RustWorkshopUploader.exe` also needs these files:

- `RustWorkshopUploader.exe.config`
- `Facepunch.Steamworks.Win64.dll`
- `Newtonsoft.Json.dll`
- `steam_api64.dll`
- `steam_api_init.dll`
- `steam_api64_real.dll`
- `Images\logo.png`
- `Images\picture-01-512.png`
- `Images\favicon.ico`

Double-click `RustWorkshopUploader.exe`. The first launch creates a `CustomSkins` folder in the working directory. Each image you choose is stored there with its workshop name, description, and Workshop ID.

Requires 64-bit Windows and .NET Framework 4.7.2.

## How to use

1. Click **Browse** and choose a PNG image. The image must be 512 x 512.
2. Enter a Workshop Item name/title and a Workshop Item description.
3. Click **Upload**. When the upload finishes, you can open the Steam Workshop page.
4. To change an item you already uploaded, enter its Workshop ID. The button changes to **Update**. The ID is saved with the image, so choosing that image again fills it in.
5. Click **N** to clear the Workshop ID and upload the image as a new item.
6. Double-click the Workshop ID to open that item on Steam.

**Info** on the menu bar opens the same instructions inside the program.

The item is published to the Rust Workshop (app 252490) as a public skin, with the tags `Version3` and `Skin`. The chosen image is the workshop preview.

## Images

The banner and window icon are loaded from the `Images` folder next to the exe, not from inside the exe.

- `Images\logo.png` is the banner at the top.
- `Images\favicon.ico` is the window icon.
- `Images\picture-01-512.png` is the placeholder shown before an image is chosen.

Replace a file, then start the program again. A rebuild is not required.

## Theme

The sun and moon switch at the right of the menu bar uses the same pill style as the language switches. Click the pill to switch between light and dark. The sun or moon inside the pill shows the current mode.

The choice is saved in `%APPDATA%\RustWorkshopUploader\nightmode.txt`.

## Language

The three language switches sit above the progress bar at the bottom of the window: English or Spanish, Russian or Ukrainian, and German or French. Click a language code, or the side of a switch, to choose that language. The menus, labels, buttons, the How to use window, and the message boxes change at once.

The choice is saved in `%APPDATA%\RustWorkshopUploader\language.txt`.

The window position is saved when you quit and restored the next time you open the program (`%APPDATA%\RustWorkshopUploader\position.txt`).

## Build

Open `RustWorkshopUploader.sln` in Visual Studio and build **Release | x64**. The project targets .NET Framework 4.7.2. The solution configurations are `Debug|x64` and `Release|x64`.

From a Visual Studio developer prompt:

```bat
msbuild RustWorkshopUploader.sln /t:Restore
msbuild RustWorkshopUploader.sln /p:Configuration=Release /p:Platform=x64
```

The build output is `bin\Release\`. The post-build step copies `steam_api64.dll`, `steam_api_init.dll`, and `steam_api64_real.dll` from `Dependencies` into that folder. `Facepunch.Steamworks.Win64.dll` and `Newtonsoft.Json.dll` are copied by the project references. Keep an `Images` folder beside the exe you run.

`Native\` holds the assembler sources for the Steam API forwarder. Rebuild those only if the Steam startup shim itself has to change.
