CREATE SERVICE [InferenceService]
ON QUEUE [InferenceQueue]
([InferenceJobContract]);
GO