CREATE SERVICE [InferenceService]
ON QUEUE [dbo].[InferenceQueue]
([InferenceJobContract]);