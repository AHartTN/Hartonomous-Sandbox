-- Nonclustered columnstore index for SVD-as-GEOMETRY analytical queries on TensorAtomCoefficients
-- CRITICAL for high-performance tensor decomposition queries
CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCI_TensorAtomCoefficients_SVD]
    ON [dbo].[TensorAtomCoefficients]
    (
        [ParentLayerId],
        [TensorAtomId],
        [Coefficient]
    );
