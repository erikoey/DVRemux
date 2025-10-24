MKV to MP4 Converter - User Manual
Table of Contents
1. Introduction

2. System Requirements

3. The User Interface Explained

4. How to Use the Application

5. Features in Detail

1. Introduction
Welcome to the MKV to MP4 Converter! This application is designed to be a fast, efficient, and high-quality tool for converting .mkv video files into the more universally compatible .mp4 format.

The key feature of this application is that it performs a remux, not a re-encode. This means it intelligently copies the original video and audio streams into a new container without altering them, resulting in a conversion that is extremely fast and has zero loss in quality.

2. System Requirements
Before you begin, there is one essential piece of software you must have installed on your system.

FFmpeg: This application is a graphical interface for the powerful command-line tool FFmpeg. You must download it and make it accessible to the application.

Download: Get a build of FFmpeg from a trusted source (e.g., gyan.dev for Windows).

Extract: Unzip the file to a permanent location on your computer (e.g., C:\ffmpeg).

Add to PATH: Add the bin folder from your FFmpeg installation (e.g., C:\ffmpeg\bin) to your system's PATH environment variable. This allows the application to find and use ffmpeg.exe and ffprobe.exe.

If FFmpeg is not found, the application will show an error message on startup.

3. The User Interface Explained
The main window is organized into several key areas:

A. File & Folder Controls (Top)
Select .mkv Files: Opens a standard dialog to browse and select one or more .mkv files to add to the conversion queue.

Remove Selected: Removes any highlighted files from the list. You can select multiple files by holding Ctrl or Shift.

Clear List: Removes all files from the list after asking for confirmation.

Select Output Folder: Allows you to choose a single, specific folder where all converted .mp4 files will be saved. If you don't use this, each new file will be saved in the same folder as its original source file.

📂 Open Output Folder: This button becomes active after a successful conversion. Clicking it will open the designated output folder in your system's file explorer.

B. The File Table
This is the main area where your files are listed. When you add files, the application will quickly analyze them and display the following information:

(Row Number): The native row number, indicating the order of conversion.

Filename: The name of the source file.

Size: The file's size in a human-readable format (e.g., 7.45 GB).

Dolby Vision: Indicates if a Dolby Vision video track is present and, if possible, which profile (e.g., 8.1).

Progress: An individual progress bar for each file that shows its conversion status.

C. Conversion Controls (Bottom)
Start Conversion: Begins the process of converting all the files in the list, one by one.

Cancel: Becomes active during a conversion. Clicking it will stop the currently running conversion and cancel any pending files in the queue.

D. Status Bar (Very Bottom)
Status Label (Left): Provides real-time feedback on what the application is doing (e.g., "Analyzing files...", "Converting...", "Finalizing file...").

☕ Buy me a coffee (Right): A donate button

4. How to Use the Application
The workflow is simple and designed to be intuitive.

Step 1: Add Files to the Queue
You can add files in two ways:

Click the "Select .mkv Files" button.

Drag and drop any combination of .mkv files or folders directly onto the file table. The application will automatically scan any dropped folders and their subfolders for .mkv files.

Step 2: (Optional) Set an Output Folder
If you want all your converted files to go to one specific place, click the "Select Output Folder" button and choose a destination.

Step 3: Manage the List
Remove Files: Select one or more files in the table and click "Remove Selected" or press the Delete key.

Clear the List: Click "Clear List" to remove all files.

Step 4: Start the Conversion
Click the "Start Conversion" button. The buttons at the top will be disabled, and the conversion process will begin with the first file in the list.

Step 5: Monitor Progress
You can track the progress in several ways:

Yellow Row: The file currently being converted will be highlighted with a light yellow background.

Individual Progress Bar: The progress bar in each row will fill up from 0% to 100%.

Finalizing State: When a progress bar reaches ~99%, it may switch to an animated "busy" state with the text "Finalizing...". This is normal and indicates FFmpeg is writing the file's metadata, which can be slow for very large files.

Green Row: A file that has been successfully converted will have its row highlighted in green.

Red Row: A file that failed to convert will have its row highlighted in red.

Step 6: Handle Errors
If a file fails, its row will turn red. You can double-click anywhere on that red row to open a dialog box containing the full, copyable error message from FFmpeg. This is useful for troubleshooting.

Step 7: Access Your Files
Once the entire batch is complete, the "📂 Open Output Folder" button will become active. Click it to immediately access your newly created .mp4 files.

5. Features in Detail
Smart Subtitle Handling: The script automatically detects the type of subtitles in your file. It will convert and include any text-based subtitles but will automatically skip incompatible image-based subtitles to prevent errors.

High-Performance Processing: The application is designed to run the FFmpeg conversion at full speed by setting a high process priority, ensuring it's just as fast as running the command from a console.

Concurrent Processing (Set to 1): While the application is built to handle multiple conversions at once, it is currently set to process one file at a time. This provides the best performance for disk-heavy copy operations on most systems.