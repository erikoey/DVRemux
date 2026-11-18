# MKV to MP4 Converter for LG TVs - User Manual

## Table of Contents
1. Introduction
2. System Requirements
3. The User Interface Explained
4. How to Use the Application
5. What Happens to Your File
6. Troubleshooting

## 1. Introduction
The MKV to MP4 Converter turns `.mkv` video files into `.mp4` files that play properly on LG webOS TVs (such as LG OLEDs), including **Dolby Vision**.

The application performs a **remux**, not a re-encode: the video stream is copied into the new container unchanged, so conversion is fast and there is zero loss in picture quality. Only the parts the TV can't play are changed: unsupported audio is converted to AC3 (Dolby Digital), and subtitles that MP4 can't hold are left out.

## 2. System Requirements
* **Windows 10 or 11 (64-bit)**.
* **FFmpeg**: The application is a graphical interface for the command-line tool FFmpeg. `ffmpeg.exe` and `ffprobe.exe` are **bundled** with the application (in the `ffmpeg` folder next to the program), so there is nothing extra to download or add to your PATH.

## 3. The User Interface Explained

### A. File & Folder Controls (Top)
* **Select .mkv Files**: Opens a dialog to choose one or more `.mkv` files to add to the queue.
* **Remove Selected**: Removes the highlighted files from the tab you are looking at (Queue or History). Hold `Ctrl` or `Shift` to select several files. You can also press `Delete`.
* **Clear List**: After asking for confirmation, empties both the Queue and the History.
* **Select Output Folder**: Chooses one folder where all converted `.mp4` files are saved. If you don't choose one, each `.mp4` is saved next to its source `.mkv`. The chosen folder is shown below the buttons.
* **📂 Open Output Folder**: Opens the chosen output folder in File Explorer. It is only available after you have selected an output folder (and not while files are being analyzed or converted). Without an output folder, use the right-click menu instead (see below).

### B. The Queue Tab
Files waiting to be converted. When you add files, they are analyzed first and then listed with:
* **#**: The position in the queue, which is the order of conversion.
* **Filename**: The source file name. A **⚠️ icon** means the file will be changed in a way you should know about; hover over it to see the details (see "Warnings" below).
* **Size**: The file size (e.g. 7.45 GB).
* **Dolby Vision**: `No`, or the Dolby Vision profile, e.g. `8.1`, `5` or `7`. `Error` means the file could not be analyzed.
* **Audio**: The audio formats found, and the largest channel layout, e.g. `DTS, AC3 (5.1 Surround)`.
* **Progress**: A progress bar for the file being converted.

The file being converted moves to the top of the queue and is highlighted in **yellow**.

### C. The History Tab
Finished files, newest first, with their **Status** and a row colour:
* **Green**: Converted successfully.
* **Red**: Failed. **Double-click** the row to see the reason (for example the FFmpeg error message).
* **Orange**: Cancelled.

### D. Right-Click Menu
Right-click a file in either tab for:
* **Open Source Folder**: Opens the folder that contains the `.mkv`.
* **Open Output Folder**: Opens the folder where the `.mp4` is (or will be) saved.
* **Remove Item(s)**: Removes the selected files from the list.

### E. Conversion Controls (Bottom)
* **Start Conversion**: Converts all files in the queue, one after another.
* **Cancel**: Stops the conversion that is running. The incomplete `.mp4` is deleted, and the remaining files stay in the queue so you can start again later.

### F. Status Bar (Very Bottom)
* **Status Label (Left)**: Shows what the application is doing, e.g. "Analyzing files..." or "Converting Movie.mkv...".
* **☕ Buy me a coffee (Right)**: A donate button.

## 4. How to Use the Application

**Step 1: Add Files to the Queue**
* Click "Select .mkv Files", or
* Drag and drop `.mkv` files or whole folders onto the queue. Dropped folders are searched for `.mkv` files, including all subfolders.

Files that are already in the Queue or History are not added twice. Files that can't be analyzed (damaged, locked, or without a video track) are not added to the queue; they go straight to History as Failed, and the status bar tells you how many there were.

**Step 2: Check the Warnings (Optional)**
Hover over any ⚠️ icon to see what will happen to that file's audio or subtitles.

**Step 3: (Optional) Choose an Output Folder**
Click "Select Output Folder" to save all `.mp4` files in one place.

**Step 4: Start the Conversion**
Click "Start Conversion". While it runs, you can't add files (by button or drag-and-drop), remove files, clear the list or change the output folder. Add more files once the conversion has finished.

**Step 5: Monitor Progress**
The progress bar fills up from 0% to 100%, based on how much of the video's running time has been processed. Near the end the bar switches to a moving animation: FFmpeg is now finalizing the file (moving the index to the front so the TV can start and seek quickly), which can take a little while for very large files.

**Step 6: Find Your Files**
Finished files move to the History tab. Use "📂 Open Output Folder", or right-click a file and choose "Open Output Folder".

## 5. What Happens to Your File

### Video
* The **first video track** is copied unchanged, so quality is identical to the source. Extra video tracks, such as embedded cover art, are left out.
* **Dolby Vision** information is kept, and HEVC video is tagged (`hvc1`) the way LG TVs expect, so the TV recognises HEVC and Dolby Vision.
* **Dolby Vision Profile 7** (UHD Blu-ray dual layer) is flagged with a warning: LG TVs don't play Profile 7 as Dolby Vision from MP4. Such files will most likely play as HDR10, or not at all. Profiles 5 and 8.1 are fine.

### Audio
Every audio track is checked individually:
* **Supported** tracks (AAC, AC3, E-AC3 including Atmos, MP3, MP2) are copied unchanged.
* **Unsupported** tracks (e.g. DTS, TrueHD, FLAC, PCM) are converted to **AC3**: 640 kbps for surround, 224 kbps for stereo and mono. AC3 supports at most 5.1, so 7.1 tracks are downmixed to 5.1.
* **Duplicate audio is removed**: many files contain the same audio twice, e.g. DTS plus AC3 in the same language. Then the unsupported track is simply dropped instead of converted. This only happens when a supported track exists with the **same language**, the **same kind** (main audio, commentary or audio description) and **at least as many channels** (up to 5.1), so other languages, commentaries and surround mixes are never lost.
* **Languages are kept**, so the TV can show the language of each track.

### Subtitles
* **Text subtitles** (SRT, ASS/SSA, WebVTT) are converted to the MP4 subtitle format, and their languages are kept. ASS/SSA styling (fonts, colours, positions) is lost in the process.
* **Image-based subtitles** (PGS from Blu-rays, DVD, DVB) and any other format MP4 can't hold are left out. Text subtitles in the same file are still kept.
* **Nearly empty subtitle tracks** (fewer than 50 subtitles or under 1 KB, typically "forced" tracks) are left out, as they can interfere with seeking on webOS.

### Other Content
* **Chapters** and **general metadata** (such as titles and tags) are removed, apart from the track languages.
* Attachments and data tracks (e.g. fonts embedded in MKV files) are not copied.

### Output File
* The `.mp4` gets the same name as the `.mkv`. **Existing files are never overwritten**: if `Movie.mp4` already exists (from an earlier conversion, or a file with the same name from another folder), the new file is saved as `Movie (1).mp4`, `Movie (2).mp4`, and so on.
* If a conversion fails or is cancelled, the incomplete `.mp4` is deleted.
* Timestamps are regenerated (`-fflags +genpts`) and the file index is placed at the start (`faststart`), which helps the TV start playback and fast-forward/rewind smoothly.

### Warnings (⚠️ icon)
The icon appears when a file contains:
* Audio that will be converted to AC3,
* Duplicate audio that will be removed,
* Subtitles that will be left out, or
* Dolby Vision Profile 7.

## 6. Troubleshooting
* **A file failed**: Double-click it in the History tab to see the reason.
* **Log file**: Each conversion's FFmpeg command and result are written to `log.txt` in the application folder. The log is cleared every time the application starts.
* **Dolby Vision doesn't show on the TV**: Check the Dolby Vision column. Profile 7 is not supported by LG TVs in MP4 (see above).
