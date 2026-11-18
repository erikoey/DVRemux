# Changelog

All notable changes to this project will be documented in this file.

## [0.8.5] - 2026-11-18

### Changed
- **User Manual**: The README was rewritten to describe the current behaviour in detail (Queue/History tabs, warnings, right-click menu, and what happens to video, audio, subtitles, chapters and metadata), with a new troubleshooting section.

### Fixed
- Dropping files during a conversion re-enabled all buttons, including Start, once their analysis finished. Drag-and-drop is now ignored while converting or analyzing.
- The `Delete` key and the right-click "Remove Item(s)" no longer remove files during a conversion.
- "Open Output Folder" is now available right after choosing an output folder, instead of only after the next analysis or conversion.

## [0.8.4] - 2026-11-18

### Fixed
- **No Overwriting**: Existing `.mp4` files are never overwritten (previously `-y` replaced them, and a failed conversion then deleted them). Name clashes get a numbered suffix, e.g. `Movie (1).mp4`, which also covers same-named files from different folders going to one output folder.
- **Failed Analysis**: Files that ffprobe can't read (or that have no video stream) are no longer converted, which could silently produce MP4s without audio. They go to History as Failed with the reason. One unreadable file no longer stops the rest of a drop from being added.
- **Subtitle Formats**: Only known text subtitle formats are converted to mov_text. DVB, XSUB and other non-text tracks are skipped instead of making the whole conversion fail.

## [0.8.3] - 2026-11-18

### Added
- **Duplicate Audio Removal**: An unsupported audio track (e.g. DTS) is dropped instead of converted when a supported track (e.g. AC3) with the same language, role (main/commentary/description) and at least as many channels (up to 5.1) already exists. Removed tracks are listed in the warning tooltip.

### Fixed
- **Track Languages**: Audio and subtitle tracks keep their language in the MP4 (previously all were "und" because `-map_metadata -1` strips stream metadata), so the TV can label them.

## [0.8.2] - 2026-11-18

### Added
- **Dolby Vision Profile 7 Warning**: Files with DV Profile 7 are flagged, since LG TVs don't play it as Dolby Vision from MP4.
- **Logging**: The ffmpeg command line and the result of each conversion are written to `log.txt`.

### Changed
- **Per-Track Audio Handling**: Only unsupported audio tracks are converted to AC3. Supported tracks (e.g. E-AC3 / Atmos) are copied untouched.
- **Accurate Progress**: Progress is now based on the encoded timestamp vs. the file's duration instead of the output file size.

### Fixed
- Text subtitles are no longer dropped when a file also contains image-based (PGS) subtitles.
- File analysis no longer fails ("Error") when a subtitle stream lacks the `NUMBER_OF_FRAMES` tag; language-suffixed tags (e.g. `NUMBER_OF_FRAMES-eng`) are recognised.
- 7.1 audio tracks are downmixed to 5.1 when converting to AC3, which supports at most 5.1.
- Partial `.mp4` files are deleted when a conversion is cancelled or fails.
- The queue is no longer read from a background thread.
- The installer version now matches the application version.

## [0.8.0] - 2026-06-23

### Added
- **Smart Audio Transcoding**: The application now detects audio codecs that are unsupported by LG WebOS (e.g., DTS, TrueHD, FLAC).
- **Dynamic Audio Bitrate**: Automatically assigns the appropriate AC3 bitrate based on channel count (640kbps for 5.1/7.1 surround, 224kbps for stereo) to prevent file bloat.
- **Audio Info Display**: Added a new "Audio" column to the Main Window (Queue and History tabs) to display the original audio codec and channel layout.
- **App Versioning**: Added version number (v0.8) to the main application window title.

### Fixed
- **LG TV Seeking Bug**: Fixed an issue where converted MP4 files would stutter or fail to fast-forward/rewind on LG TVs by forcing FFmpeg to generate missing timestamps (`-fflags +genpts`).