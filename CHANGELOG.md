# Changelog

All notable changes to this project will be documented in this file.

## [0.8.0] - 2026-06-23

### Added
- **Smart Audio Transcoding**: The application now detects audio codecs that are unsupported by LG WebOS (e.g., DTS, TrueHD, FLAC).
- **Dynamic Audio Bitrate**: Automatically assigns the appropriate AC3 bitrate based on channel count (640kbps for 5.1/7.1 surround, 224kbps for stereo) to prevent file bloat.
- **Audio Info Display**: Added a new "Audio" column to the Main Window (Queue and History tabs) to display the original audio codec and channel layout.
- **App Versioning**: Added version number (v0.8) to the main application window title.

### Fixed
- **LG TV Seeking Bug**: Fixed an issue where converted MP4 files would stutter or fail to fast-forward/rewind on LG TVs by forcing FFmpeg to generate missing timestamps (`-fflags +genpts`).