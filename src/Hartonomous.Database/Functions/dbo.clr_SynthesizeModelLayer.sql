CREATE FUNCTION dbo.clr_SynthesizeModelLayer(
    @queryShape GEOMETRY,
    @parentLayerId BIGINT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorOperations.ModelSynthesis].clr_SynthesizeModelLayer;