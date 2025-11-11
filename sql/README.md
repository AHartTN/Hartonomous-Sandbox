# SQL Scripts Directory (Transitional)

The canonical relational schema now lives in the SDK-style database project at `src/Hartonomous.Database`. The loose scripts under this `sql/` tree remain only as a staging mirror while we consolidate existing deployment workflows. When you change any table, view, or procedure, update the corresponding script in the database project first and then mirror the change here until the legacy pipelines are retired.

> Short term rule: treat everything under `sql/` as read-only output that must match the database project definitions byte-for-byte. Once the older PowerShell deploy paths are removed, this folder will collapse into generated artifacts.
