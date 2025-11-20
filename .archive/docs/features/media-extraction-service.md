# Media Extraction Service - Feature Overview

A comprehensive media toolkit for extraction, analysis, visualization, and editing operations.

## 📹 Video Clipping & Editing

### Frame Extraction
- **`ExtractFramesAsync()`**: Extract frames with time range, FPS control, format options (PNG/JPEG/BMP), resize, JPEG quality
  
### Video Segments
- **`ExtractVideoSegmentAsync()`**: Fast stream copy for extracting clips (no re-encoding)
- **`CreateGifAsync()`**: Convert video segments to animated GIFs with FPS and size control

### Video Composition
- **`ConcatenateVideosAsync()`**: Merge multiple videos into single file
- **`CreateVideoGridAsync()`**: Multi-video collage/grid layout (e.g., 2x2, 3x3)
- **`GenerateContactSheetAsync()`**: Thumbnail grid/preview sheet from video

### Video Effects
- **`AddTextOverlayAsync()`**: Add text with positioning (topleft, center, bottomright, etc.), font size, color
- **`ChangeVideoSpeedAsync()`**: Speed up/slow down (handles extreme speeds via multi-stage processing)

## 🎵 Audio Extraction & Conversion

### Audio Clipping
- **`ExtractAudioAsync()`**: Extract audio with:
  - Time range (start + duration)
  - Format conversion (MP3, AAC, FLAC, OGG, WAV)
  - Bitrate control (128, 192, 320 kbps)
  - Sample rate selection
  - Mono/stereo conversion
  - Audio-only stripping from video

### Audio Effects
- **`NormalizeAudioAsync()`**: Volume normalization to target loudness (-16 LUFS default)
- **`FadeAudioAsync()`**: Fade in/out with configurable durations
- **`RemoveSilenceAsync()`**: Strip silent sections (configurable threshold and duration)

## 📊 Audio Analysis & Visualization

### Waveform Analysis
- **`ExtractWaveformAsync()`**: Extract amplitude data for waveform visualization
  - Configurable resolution (sample count)
  - Returns normalized amplitudes (-1.0 to 1.0)
  - Suitable for UI rendering

### Frequency Analysis
- **`AnalyzeFrequencySpectrumAsync()`**: FFT-based frequency spectrum analysis
  - Returns: frequencies, magnitudes, decibels
  - Configurable FFT window size (2048, 4096, etc.)
  - Time range selection
  - Hamming window applied for spectral leakage reduction

### Spectrogram Generation
- **`GenerateSpectrogramAsync()`**: Time-frequency heatmap
  - 2D array [timeFrame][frequencyBin]
  - Configurable FFT size and hop size
  - Returns frequency/time resolution metrics
  - Suitable for spectrogram visualization

### Audio Metrics
- **`AnalyzeAudioAsync()`**: Comprehensive audio analysis:
  - **Levels**: RMS (root mean square), peak amplitude (in linear and dB)
  - **Dynamic range**: Peak-to-RMS ratio
  - **Zero crossing rate**: Indicator of noise/brightness
  - **Metadata**: Duration, sample rate, channels, bit depth, codec, bitrate

## 🧮 DSP (Digital Signal Processing)

### FFT Implementation
- **Cooley-Tukey FFT algorithm**: Recursive implementation for efficient frequency analysis
- **Hamming window**: Applied to reduce spectral leakage
- **Complex number support**: Using System.Numerics.Complex

## 📦 Data Models

### FrequencySpectrum
- Frequencies, magnitudes, decibels
- Sample rate, FFT size, frequency resolution

### Spectrogram
- 2D time-frequency data in dB
- Time frames, frequency bins
- FFT size, hop size
- Frequency/time resolution

### AudioAnalysis
- Duration, sample rate, channels, bit depth
- Codec, bitrate
- RMS/peak levels (linear and dB)
- Zero crossing rate, dynamic range
- Total samples analyzed

## 🎯 Example Use Cases

### Video Editing
```csharp
var service = new MediaExtractionService();

// Extract 10-second clip
await service.ExtractVideoSegmentAsync(
    "input.mp4", "clip.mp4",
    startTime: TimeSpan.FromSeconds(30),
    duration: TimeSpan.FromSeconds(10));

// Create 2x speed timelapse
await service.ChangeVideoSpeedAsync("input.mp4", "fast.mp4", speedFactor: 2.0);

// Add watermark
await service.AddTextOverlayAsync("input.mp4", "output.mp4", 
    text: "© 2025", position: "bottomright", fontSize: 18);
```

### Audio Processing
```csharp
// Extract high-quality audio
await service.ExtractAudioAsync("video.mp4", "audio.flac",
    new AudioExtractionOptions {
        OutputFormat = "flac",
        StartTime = TimeSpan.FromMinutes(1),
        Duration = TimeSpan.FromMinutes(5)
    });

// Normalize and fade
await service.NormalizeAudioAsync("input.mp3", "normalized.mp3", targetLoudnessDb: -16);
await service.FadeAudioAsync("normalized.mp3", "final.mp3",
    fadeInDuration: TimeSpan.FromSeconds(2),
    fadeOutDuration: TimeSpan.FromSeconds(3));
```

### Audio Analysis
```csharp
// Analyze audio characteristics
var analysis = await service.AnalyzeAudioAsync("song.mp3");
Console.WriteLine($"Peak: {analysis.PeakLevelDb:F2} dB");
Console.WriteLine($"RMS: {analysis.RmsLevelDb:F2} dB");
Console.WriteLine($"Dynamic Range: {analysis.DynamicRange:F2} dB");

// Generate waveform for visualization
var waveform = await service.ExtractWaveformAsync("song.mp3", sampleCount: 500);
// Render waveform using waveform[i] values

// Frequency spectrum analysis
var spectrum = await service.AnalyzeFrequencySpectrumAsync("song.mp3",
    startTime: TimeSpan.FromSeconds(10),
    duration: TimeSpan.FromSeconds(1),
    fftSize: 4096);
// Plot frequencies vs magnitudes

// Generate spectrogram
var spectrogram = await service.GenerateSpectrogramAsync("song.mp3",
    fftSize: 2048, hopSize: 512);
// Render as heatmap: spectrogram.Data[time][frequency]
```

### Content Creation
```csharp
// Create video montage
await service.ConcatenateVideosAsync(
    new[] { "clip1.mp4", "clip2.mp4", "clip3.mp4" },
    "montage.mp4");

// Create 3-second GIF preview
await service.CreateGifAsync("video.mp4", "preview.gif",
    startTime: TimeSpan.FromSeconds(5),
    duration: TimeSpan.FromSeconds(3),
    fps: 15, width: 480);

// Generate thumbnail contact sheet
await service.GenerateContactSheetAsync("video.mp4", "thumbs.jpg",
    columns: 4, rows: 4);
```

## 🔧 Technical Details

- **FFmpeg Integration**: Uses FFMpegCore 5.4.0 for all media operations
- **Cross-platform**: Works on Windows, Linux, macOS
- **Async/await**: All operations support cancellation tokens
- **Temp file management**: Automatic cleanup of temporary files
- **Format support**: Extensive format compatibility via FFmpeg
- **Performance**: Stream copying (no re-encoding) where possible

## 📈 Performance Considerations

- Frame extraction limited by FFmpeg decode speed
- FFT operations are CPU-intensive (consider caching results)
- Spectrogram generation can be memory-intensive for long files
- Audio analysis reads entire PCM stream (suitable for files under ~10 minutes)
- Video concatenation uses stream copy (very fast)
- Format conversions involve re-encoding (slower but flexible)

## 🚀 Future Enhancements

Potential additions:
- Beat detection / tempo analysis
- Pitch detection
- Video stabilization
- Color grading / LUT application
- Subtitle extraction/embedding
- Multi-track audio mixing
- Real-time streaming support
