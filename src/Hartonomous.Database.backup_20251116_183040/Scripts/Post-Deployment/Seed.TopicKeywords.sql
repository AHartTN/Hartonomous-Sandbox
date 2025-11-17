/*
Post-Deployment Script: Seed TopicKeywords
*/
PRINT 'Seeding dbo.TopicKeywords...';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.TopicKeywords WHERE topic_name = 'technical')
BEGIN
    INSERT INTO dbo.TopicKeywords (topic_name, keyword, weight) VALUES
    ('technical', 'database', 1.0),
    ('technical', 'sql', 1.0),
    ('technical', 'server', 0.8),
    ('technical', 'query', 0.9),
    ('technical', 'index', 0.9),
    ('technical', 'algorithm', 1.0),
    ('technical', 'programming', 1.0),
    ('technical', 'code', 0.9),
    ('technical', 'function', 0.8),
    ('technical', 'api', 0.9),
    ('technical', 'system', 0.7),
    ('technical', 'software', 0.9),
    ('technical', 'data', 0.8),
    ('technical', 'computer', 0.8),
    ('technical', 'network', 0.9),
    ('business', 'revenue', 1.0),
    ('business', 'profit', 1.0),
    ('business', 'customer', 0.9),
    ('business', 'market', 0.9),
    ('business', 'sales', 0.9),
    ('business', 'strategy', 0.8),
    ('business', 'management', 0.8),
    ('business', 'finance', 1.0),
    ('business', 'investment', 0.9),
    ('business', 'business', 1.0),
    ('business', 'enterprise', 0.8),
    ('business', 'commerce', 0.9),
    ('scientific', 'research', 1.0),
    ('scientific', 'theory', 0.9),
    ('scientific', 'experiment', 1.0),
    ('scientific', 'hypothesis', 1.0),
    ('scientific', 'scientific', 1.0),
    ('scientific', 'analysis', 0.8),
    ('scientific', 'study', 0.8),
    ('scientific', 'method', 0.7),
    ('scientific', 'evidence', 0.9),
    ('scientific', 'quantum', 1.0),
    ('scientific', 'physics', 1.0),
    ('scientific', 'biology', 1.0),
    ('scientific', 'chemistry', 1.0),
    ('creative', 'design', 1.0),
    ('creative', 'art', 1.0),
    ('creative', 'creative', 1.0),
    ('creative', 'aesthetic', 0.9),
    ('creative', 'visual', 0.8),
    ('creative', 'style', 0.7),
    ('creative', 'artistic', 1.0),
    ('creative', 'beautiful', 0.8),
    ('creative', 'imagine', 0.8),
    ('creative', 'inspire', 0.9);
END
GO
