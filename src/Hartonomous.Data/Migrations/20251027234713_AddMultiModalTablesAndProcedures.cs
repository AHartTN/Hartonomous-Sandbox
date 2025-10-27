using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Hartonomous.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiModalTablesAndProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AudioData",
                schema: "dbo",
                columns: table => new
                {
                    audio_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    raw_data = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    sample_rate = table.Column<int>(type: "int", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    num_channels = table.Column<byte>(type: "tinyint", nullable: false),
                    format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    spectrogram = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    mel_spectrogram = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    waveform_left = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    waveform_right = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    global_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    global_embedding_dim = table.Column<int>(type: "int", nullable: true),
                    metadata = table.Column<string>(type: "JSON", nullable: true),
                    ingestion_date = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioData", x => x.audio_id);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                schema: "dbo",
                columns: table => new
                {
                    image_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    source_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    raw_data = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    width = table.Column<int>(type: "int", nullable: false),
                    height = table.Column<int>(type: "int", nullable: false),
                    channels = table.Column<int>(type: "int", nullable: false),
                    format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    pixel_cloud = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    edge_map = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    object_regions = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    saliency_regions = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    global_embedding = table.Column<SqlVector<float>>(type: "VECTOR(1536)", nullable: true),
                    global_embedding_dim = table.Column<int>(type: "int", nullable: true),
                    metadata = table.Column<string>(type: "JSON", nullable: true),
                    ingestion_date = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()"),
                    last_accessed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    access_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.image_id);
                });

            migrationBuilder.CreateTable(
                name: "TextDocuments",
                schema: "dbo",
                columns: table => new
                {
                    doc_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    source_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    raw_text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    char_count = table.Column<int>(type: "int", nullable: true),
                    word_count = table.Column<int>(type: "int", nullable: true),
                    global_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    global_embedding_dim = table.Column<int>(type: "int", nullable: true),
                    topic_vector = table.Column<SqlVector<float>>(type: "VECTOR(100)", nullable: true),
                    sentiment_score = table.Column<float>(type: "real", nullable: true),
                    toxicity = table.Column<float>(type: "real", nullable: true),
                    metadata = table.Column<string>(type: "JSON", nullable: true),
                    ingestion_date = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()"),
                    last_accessed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    access_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextDocuments", x => x.doc_id);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                schema: "dbo",
                columns: table => new
                {
                    video_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    raw_data = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    fps = table.Column<int>(type: "int", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    resolution_width = table.Column<int>(type: "int", nullable: false),
                    resolution_height = table.Column<int>(type: "int", nullable: false),
                    num_frames = table.Column<long>(type: "bigint", nullable: false),
                    format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    global_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    global_embedding_dim = table.Column<int>(type: "int", nullable: true),
                    metadata = table.Column<string>(type: "JSON", nullable: true),
                    ingestion_date = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.video_id);
                });

            migrationBuilder.CreateTable(
                name: "AudioFrames",
                schema: "dbo",
                columns: table => new
                {
                    audio_id = table.Column<long>(type: "bigint", nullable: false),
                    frame_number = table.Column<long>(type: "bigint", nullable: false),
                    timestamp_ms = table.Column<long>(type: "bigint", nullable: false),
                    amplitude_l = table.Column<float>(type: "real", nullable: true),
                    amplitude_r = table.Column<float>(type: "real", nullable: true),
                    spectral_centroid = table.Column<float>(type: "real", nullable: true),
                    spectral_rolloff = table.Column<float>(type: "real", nullable: true),
                    zero_crossing_rate = table.Column<float>(type: "real", nullable: true),
                    rms_energy = table.Column<float>(type: "real", nullable: true),
                    mfcc = table.Column<SqlVector<float>>(type: "VECTOR(13)", nullable: true),
                    frame_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioFrames", x => new { x.audio_id, x.frame_number });
                    table.ForeignKey(
                        name: "FK_AudioFrames_AudioData_audio_id",
                        column: x => x.audio_id,
                        principalSchema: "dbo",
                        principalTable: "AudioData",
                        principalColumn: "audio_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImagePatches",
                schema: "dbo",
                columns: table => new
                {
                    patch_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    image_id = table.Column<long>(type: "bigint", nullable: false),
                    patch_x = table.Column<int>(type: "int", nullable: false),
                    patch_y = table.Column<int>(type: "int", nullable: false),
                    patch_width = table.Column<int>(type: "int", nullable: false),
                    patch_height = table.Column<int>(type: "int", nullable: false),
                    patch_region = table.Column<Geometry>(type: "GEOMETRY", nullable: false),
                    patch_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    dominant_color = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    texture_features = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    mean_intensity = table.Column<float>(type: "real", nullable: true),
                    std_intensity = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagePatches", x => x.patch_id);
                    table.ForeignKey(
                        name: "FK_ImagePatches_Images_image_id",
                        column: x => x.image_id,
                        principalSchema: "dbo",
                        principalTable: "Images",
                        principalColumn: "image_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoFrames",
                schema: "dbo",
                columns: table => new
                {
                    frame_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    video_id = table.Column<long>(type: "bigint", nullable: false),
                    frame_number = table.Column<long>(type: "bigint", nullable: false),
                    timestamp_ms = table.Column<long>(type: "bigint", nullable: false),
                    pixel_cloud = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    object_regions = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    motion_vectors = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    optical_flow = table.Column<Geometry>(type: "GEOMETRY", nullable: true),
                    frame_embedding = table.Column<SqlVector<float>>(type: "VECTOR(768)", nullable: true),
                    perceptual_hash = table.Column<byte[]>(type: "varbinary(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoFrames", x => x.frame_id);
                    table.ForeignKey(
                        name: "FK_VideoFrames_Videos_video_id",
                        column: x => x.video_id,
                        principalSchema: "dbo",
                        principalTable: "Videos",
                        principalColumn: "video_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_duration",
                schema: "dbo",
                table: "AudioData",
                column: "duration_ms");

            migrationBuilder.CreateIndex(
                name: "idx_ingestion",
                schema: "dbo",
                table: "AudioData",
                column: "ingestion_date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_image_patches",
                schema: "dbo",
                table: "ImagePatches",
                columns: new[] { "image_id", "patch_x", "patch_y" });

            migrationBuilder.CreateIndex(
                name: "idx_dimensions",
                schema: "dbo",
                table: "Images",
                columns: new[] { "width", "height" });

            migrationBuilder.CreateIndex(
                name: "idx_ingestion",
                schema: "dbo",
                table: "Images",
                column: "ingestion_date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_timestamp",
                schema: "dbo",
                table: "VideoFrames",
                columns: new[] { "video_id", "timestamp_ms" });

            migrationBuilder.CreateIndex(
                name: "idx_video_frame",
                schema: "dbo",
                table: "VideoFrames",
                columns: new[] { "video_id", "frame_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ingestion",
                schema: "dbo",
                table: "Videos",
                column: "ingestion_date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_resolution",
                schema: "dbo",
                table: "Videos",
                columns: new[] { "resolution_width", "resolution_height" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioFrames",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ImagePatches",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TextDocuments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "VideoFrames",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AudioData",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Images",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Videos",
                schema: "dbo");
        }
    }
}
