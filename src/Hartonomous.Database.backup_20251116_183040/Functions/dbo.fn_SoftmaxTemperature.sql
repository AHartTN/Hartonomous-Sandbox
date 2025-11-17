CREATE FUNCTION dbo.fn_SoftmaxTemperature(
    @logit FLOAT,
    @max_logit FLOAT,
    @temperature FLOAT
)
RETURNS FLOAT
AS
BEGIN
    RETURN EXP((@logit - @max_logit) / @temperature);
END;