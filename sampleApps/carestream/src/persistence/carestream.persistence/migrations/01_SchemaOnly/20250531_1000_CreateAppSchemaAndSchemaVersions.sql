-- 20250531_1000_CreateAppSchemaAndSchemaVersions.sql
-- Creates the initial schema_versions table for DbUp tracking and the 'app' schema.

CREATE SCHEMA IF NOT EXISTS app;

CREATE TABLE IF NOT EXISTS public.schema_versions (
    schemaversionsid SERIAL PRIMARY KEY,
    scriptname VARCHAR(255) NOT NULL,
    applied TIMESTAMP NOT NULL
);