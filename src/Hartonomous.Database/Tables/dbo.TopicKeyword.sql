CREATE TABLE dbo.TopicKeywords (
    keyword_id INT PRIMARY KEY IDENTITY(1,1),
    topic_name NVARCHAR(50),
    keyword NVARCHAR(100),
    weight FLOAT DEFAULT 1.0
);