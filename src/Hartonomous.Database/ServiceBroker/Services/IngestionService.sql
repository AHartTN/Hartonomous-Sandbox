CREATE SERVICE [IngestionService]
ON QUEUE [dbo].[IngestionQueue]
([IngestionContract]);
