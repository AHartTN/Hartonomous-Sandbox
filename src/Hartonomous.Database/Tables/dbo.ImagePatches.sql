CREATE TABLE [dbo].[ImagePatches] (
    [PatchId]       BIGINT         NOT NULL IDENTITY,
    [ImageId]       BIGINT         NOT NULL,
    [PatchX]        INT            NOT NULL,
    [PatchY]        INT            NOT NULL,
    [PatchWidth]    INT            NOT NULL,
    [PatchHeight]   INT            NOT NULL,
    [PatchRegion]   GEOMETRY       NOT NULL,
    [PatchEmbedding]VARBINARY(MAX) NULL,
    [DominantColor] GEOMETRY       NULL,
    [MeanIntensity] REAL           NULL,
    [StdIntensity]  REAL           NULL,
    CONSTRAINT [PK_ImagePatches] PRIMARY KEY CLUSTERED ([PatchId] ASC),
    CONSTRAINT [FK_ImagePatches_Images_ImageId] FOREIGN KEY ([ImageId]) REFERENCES [dbo].[Images] ([ImageId]) ON DELETE CASCADE
);
