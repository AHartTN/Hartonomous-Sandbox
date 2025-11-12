using System;
using System.IO;

namespace Hartonomous.Core.Pipelines.Ingestion
{
    /// <summary>
    /// Detects audio and video file formats using magic number (file signature) analysis.
    /// Replaces hardcoded MIME types with actual content-based detection.
    /// </summary>
    public static class MediaFormatDetector
    {
        /// <summary>
        /// Detects audio format from raw bytes using magic number analysis.
        /// </summary>
        /// <param name="data">First 16 bytes of the audio file</param>
        /// <returns>MIME type (e.g., "audio/mpeg", "audio/wav")</returns>
        public static string DetectAudioFormat(byte[] data)
        {
            if (data == null || data.Length < 12)
            {
                throw new ArgumentException("Audio data must be at least 12 bytes for format detection");
            }

            // MP3: ID3v2 tag (0x49 0x44 0x33) or MPEG frame sync (0xFF 0xFB, 0xFF 0xF3, 0xFF 0xF2)
            if (data.Length >= 3 && data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)
            {
                return "audio/mpeg"; // ID3v2 tag
            }
            if (data.Length >= 2 && data[0] == 0xFF && (data[1] == 0xFB || data[1] == 0xF3 || data[1] == 0xF2))
            {
                return "audio/mpeg"; // MPEG-1 Layer 3 frame sync
            }

            // AAC: ADTS header (0xFF 0xF1 or 0xFF 0xF9)
            if (data.Length >= 2 && data[0] == 0xFF && (data[1] == 0xF1 || data[1] == 0xF9))
            {
                return "audio/aac";
            }

            // WAV: RIFF header "RIFF....WAVE"
            if (data.Length >= 12 &&
                data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 && // "RIFF"
                data[8] == 0x57 && data[9] == 0x41 && data[10] == 0x56 && data[11] == 0x45)   // "WAVE"
            {
                return "audio/wav";
            }

            // FLAC: "fLaC" (0x66 0x4C 0x61 0x43)
            if (data.Length >= 4 &&
                data[0] == 0x66 && data[1] == 0x4C && data[2] == 0x61 && data[3] == 0x43)
            {
                return "audio/flac";
            }

            // OGG Vorbis: "OggS" (0x4F 0x67 0x67 0x53)
            if (data.Length >= 4 &&
                data[0] == 0x4F && data[1] == 0x67 && data[2] == 0x67 && data[3] == 0x53)
            {
                return "audio/ogg";
            }

            // M4A/AAC in MP4 container: ftyp box with M4A brand
            if (data.Length >= 12 &&
                data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70 && // "ftyp"
                data[8] == 0x4D && data[9] == 0x34 && data[10] == 0x41)                      // "M4A"
            {
                return "audio/mp4";
            }

            // AIFF: "FORM....AIFF"
            if (data.Length >= 12 &&
                data[0] == 0x46 && data[1] == 0x4F && data[2] == 0x52 && data[3] == 0x4D && // "FORM"
                data[8] == 0x41 && data[9] == 0x49 && data[10] == 0x46 && data[11] == 0x46)   // "AIFF"
            {
                return "audio/aiff";
            }

            // WMA: ASF header (0x30 0x26 0xB2 0x75 0x8E 0x66 0xCF 0x11)
            if (data.Length >= 8 &&
                data[0] == 0x30 && data[1] == 0x26 && data[2] == 0xB2 && data[3] == 0x75 &&
                data[4] == 0x8E && data[5] == 0x66 && data[6] == 0xCF && data[7] == 0x11)
            {
                return "audio/x-ms-wma";
            }

            return "application/octet-stream"; // Unknown audio format
        }

        /// <summary>
        /// Detects video format from raw bytes using magic number analysis.
        /// </summary>
        /// <param name="data">First 16 bytes of the video file</param>
        /// <returns>MIME type (e.g., "video/mp4", "video/webm")</returns>
        public static string DetectVideoFormat(byte[] data)
        {
            if (data == null || data.Length < 12)
            {
                throw new ArgumentException("Video data must be at least 12 bytes for format detection");
            }

            // MP4/M4V: ftyp box (0x66 0x74 0x79 0x70 at offset 4)
            // Common brands: isom, mp41, mp42, M4V, avc1
            if (data.Length >= 8 && data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70)
            {
                // Check brand at offset 8
                if (data.Length >= 12)
                {
                    // M4V brand
                    if (data[8] == 0x4D && data[9] == 0x34 && data[10] == 0x56)
                    {
                        return "video/x-m4v";
                    }
                    // Default to MP4 for isom, mp41, mp42, avc1, etc.
                    return "video/mp4";
                }
                return "video/mp4";
            }

            // AVI: RIFF header "RIFF....AVI "
            if (data.Length >= 12 &&
                data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 && // "RIFF"
                data[8] == 0x41 && data[9] == 0x56 && data[10] == 0x49 && data[11] == 0x20)   // "AVI "
            {
                return "video/x-msvideo";
            }

            // WebM: EBML header (0x1A 0x45 0xDF 0xA3)
            if (data.Length >= 4 &&
                data[0] == 0x1A && data[1] == 0x45 && data[2] == 0xDF && data[3] == 0xA3)
            {
                return "video/webm";
            }

            // MKV: EBML header (same as WebM, need to check DocType)
            // WebM is a subset of MKV - check for "matroska" DocType at variable offset
            if (data.Length >= 4 &&
                data[0] == 0x1A && data[1] == 0x45 && data[2] == 0xDF && data[3] == 0xA3)
            {
                // Default to MKV if EBML detected but not confirmed as WebM
                return "video/x-matroska";
            }

            // FLV: "FLV" (0x46 0x4C 0x56)
            if (data.Length >= 3 &&
                data[0] == 0x46 && data[1] == 0x4C && data[2] == 0x56)
            {
                return "video/x-flv";
            }

            // MOV: QuickTime "moov" or "mdat" atom (variable position, often at offset 4)
            if (data.Length >= 8 &&
                ((data[4] == 0x6D && data[5] == 0x6F && data[6] == 0x6F && data[7] == 0x76) || // "moov"
                 (data[4] == 0x6D && data[5] == 0x64 && data[6] == 0x61 && data[7] == 0x74)))  // "mdat"
            {
                return "video/quicktime";
            }

            // MPEG-PS: 0x00 0x00 0x01 0xBA (MPEG-2 Program Stream)
            if (data.Length >= 4 &&
                data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x01 && data[3] == 0xBA)
            {
                return "video/mpeg";
            }

            // MPEG-TS: 0x47 at offset 0 (sync byte), repeats every 188 bytes
            if (data.Length >= 1 && data[0] == 0x47)
            {
                // Check for second sync byte at offset 188 if available
                // For now, just check first byte (may have false positives)
                return "video/mp2t";
            }

            // WMV: ASF header (same as WMA)
            if (data.Length >= 8 &&
                data[0] == 0x30 && data[1] == 0x26 && data[2] == 0xB2 && data[3] == 0x75 &&
                data[4] == 0x8E && data[5] == 0x66 && data[6] == 0xCF && data[7] == 0x11)
            {
                return "video/x-ms-wmv";
            }

            // 3GP/3G2: ftyp box with 3gp or 3g2 brand
            if (data.Length >= 12 &&
                data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70) // "ftyp"
            {
                if (data[8] == 0x33 && data[9] == 0x67 && data[10] == 0x70) // "3gp"
                {
                    return "video/3gpp";
                }
                if (data[8] == 0x33 && data[9] == 0x67 && data[10] == 0x32) // "3g2"
                {
                    return "video/3gpp2";
                }
            }

            return "application/octet-stream"; // Unknown video format
        }

        /// <summary>
        /// Detects media format from a file stream.
        /// Reads only the first 16 bytes for efficient detection.
        /// </summary>
        public static string DetectMediaFormat(Stream stream, bool isVideo)
        {
            if (stream == null || !stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable");
            }

            var buffer = new byte[16];
            var originalPosition = stream.Position;
            
            try
            {
                stream.Position = 0;
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                
                if (bytesRead < 12)
                {
                    throw new InvalidDataException("File too small for format detection (< 12 bytes)");
                }

                return isVideo ? DetectVideoFormat(buffer) : DetectAudioFormat(buffer);
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Detects media format from a file path.
        /// </summary>
        public static string DetectMediaFormat(string filePath, bool isVideo)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return DetectMediaFormat(stream, isVideo);
            }
        }
    }
}
