CREATE AGGREGATE dbo.ChainOfThoughtCoherence(@step NVARCHAR(MAX), @stepNumber INT)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ChainOfThoughtCoherence];