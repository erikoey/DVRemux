# Changelog

All notable changes to this project will be documented in this file.

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