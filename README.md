# MKV to MP4 Converter - User Manual

## Table of Contents
1. Introduction
2. System Requirements
3. The User Interface Explained
4. How to Use the Application
5. Features in Detail

## 1. Introduction
Welcome to the MKV to MP4 Converter! This application is designed to be a fast, efficient, and high-quality tool for converting `.mkv` video files into the more universally compatible `.mp4` format, specifically optimized for LG WebOS TVs.

The key feature of this application is that it primarily performs a **remux**, not a full re-encode. It intelligently copies the original video stream into a new container without altering it, resulting in a lightning-fast conversion with zero loss in video quality. It also features smart audio transcoding to ensure your media plays perfectly on strict players.

## 2. System Requirements
Before you begin, there is one essential piece of software you must have installed on your system.

**FFmpeg**: This application is a graphical interface for the powerful command-line tool FFmpeg. You must download it and make it accessible to the application.
* **Download**: Get a build of FFmpeg from a trusted source (e.g., gyan.dev for Windows).
* **Extract**: Unzip the file to a permanent location on your computer (e.g., `C:\ffmpeg`).
* **Add to PATH**: Add the `bin` folder from your FFmpeg installation (e.g., `C:\ffmpeg\bin`) to your system's PATH environment variable. This allows the application to find and use `ffmpeg.exe` and `ffprobe.exe`.

*Note: The application will guide you through this process on startup if FFmpeg is not found.*

## 3. The User Interface Explained
The main window is organized into several key areas:

### A. File & Folder Controls (Top)
* **Select .mkv Files**: Opens a standard dialog to browse and select one or more `.mkv` files to add to the conversion queue.
* **Remove Selected**: Removes any highlighted files from the list. You can select multiple files by holding `Ctrl` or `Shift`.
* **Clear List**: Removes all files from the list after asking for confirmation.
* **Select Output Folder**: Allows you to choose a single, specific folder where all converted `.mp4` files will be saved. If you don't use this, each new file will be saved in the same folder as its original source file.
* **📂 Open Output Folder**: This button becomes active after a successful conversion. Clicking it will open the designated output folder in your system's file explorer.

### B. The File Table
This is the main area where your files are listed. When you add files, the application will quickly analyze them and display the following information:
* **(Row Number)**: The native row number, indicating the order of conversion.
* **Filename**: The name of the source file.
* **Size**: The file's size in a human-readable format (e.g., 7.45 GB).
* **Dolby Vision**: Indicates if a Dolby Vision video track is present and, if possible, which profile (e.g., 8.1).
* **Audio**: Displays the detected audio codec and channel layout (e.g., DTS (5.1 Surround)).
* **Progress / Status**: An individual progress bar or status indicator for each file.

### C. Conversion Controls (Bottom)
* **Start Conversion**: Begins the process of converting all the files in the list, one by one.
* **Cancel**: Becomes active during a conversion. Clicking it will stop the currently running conversion and cancel any pending files in the queue.

### D. Status Bar (Very Bottom)
* **Status Label (Left)**: Provides real-time feedback on what the application is doing (e.g., "Analyzing files...", "Converting...", "Finalizing file...").
* **☕ Buy me a coffee (Right)**: A donate button.

## 4. How to Use the Application
The workflow is simple and designed to be intuitive.

**Step 1: Add Files to the Queue**
You can add files in two ways:
* Click the "Select .mkv Files" button.
* Drag and drop any combination of `.mkv` files or folders directly onto the file table. The application will automatically scan any dropped folders and their subfolders for `.mkv` files.

**Step 2: (Optional) Set an Output Folder**
If you want all your converted files to go to one specific place, click the "Select Output Folder" button and choose a destination.

**Step 3: Manage the List**
* **Remove Files**: Select one or more files in the table and click "Remove Selected" or press the `Delete` key.
* **Clear the List**: Click "Clear List" to remove all files.

**Step 4: Start the Conversion**
Click the "Start Conversion" button. The buttons at the top will be disabled, and the conversion process will begin with the first file in the list.

**Step 5: Monitor Progress**
* **Yellow Row**: The file currently being converted.
* **Individual Progress Bar**: Fills up from 0% to 100%.
* **Finalizing State**: When a progress bar reaches ~99%, it may switch to "Finalizing...". This indicates FFmpeg is writing the file's metadata, which can be slow for very large files.
* **Green Row**: Successfully converted.
* **Red Row**: Failed to convert. (Double-click the row to view the FFmpeg error log).

**Step 6: Access Your Files**
Once complete, the "📂 Open Output Folder" button becomes active. Click it to immediately access your newly created `.mp4` files.

## 5. Features in Detail

* **Perfect Video Remuxing**: The app perfectly copies the original video stream without altering its quality, ensuring native Dolby Vision layers remain intact for compatible LG TVs.
* **Smart Audio Transcoding**: Not all audio formats are supported by strict built-in players (like LG WebOS, which struggles with DTS, TrueHD, and FLAC). The application automatically detects unsupported codecs and seamlessly transcodes them to universally supported AC3 (Dolby Digital) on the fly.
* **Dynamic Audio Bitrate**: When converting audio, the app assigns the bitrate based on the channel count (e.g., 640kbps for 5.1/7.1 Surround, and 224kbps for 2.0 Stereo) to preserve high audio quality without causing unnecessary file bloat.
* **Flawless Seeking & Scrubbing**: Remuxing MKV to MP4 can sometimes break video scrubbing (fast-forwarding/rewinding) on smart TVs due to timestamp irregularities. This app forces perfect timestamp generation (`-fflags +genpts`) to guarantee smooth playback controls.
* **Smart Subtitle Handling**: The script automatically detects the type of subtitles. It includes text-based subtitles but safely skips incompatible image-based subtitles (like PGS) to prevent conversion failures.
* **High-Performance Processing**: The application runs the FFmpeg conversion at full speed, ensuring it's just as fast as running commands from a console.