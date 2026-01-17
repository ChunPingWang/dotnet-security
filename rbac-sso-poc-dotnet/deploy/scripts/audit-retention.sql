-- =============================================================================
-- Audit Log Retention Policy SQL Script
-- 審計日誌保留策略 SQL 腳本
-- =============================================================================
-- This script creates a PostgreSQL function and scheduled job for
-- automatically deleting audit logs older than 90 days.
--
-- Prerequisites:
-- 1. PostgreSQL with pg_cron extension enabled
-- 2. Superuser privileges to create extension and schedule jobs
-- =============================================================================

-- Enable pg_cron extension (requires superuser)
CREATE EXTENSION IF NOT EXISTS pg_cron;

-- Grant usage to the application user (adjust as needed)
-- GRANT USAGE ON SCHEMA cron TO app_user;

-- =============================================================================
-- Function: delete_old_audit_logs
-- Description: Deletes audit logs older than retention period
-- =============================================================================
CREATE OR REPLACE FUNCTION delete_old_audit_logs(retention_days INTEGER DEFAULT 90)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    deleted_count INTEGER;
    cutoff_date TIMESTAMP WITH TIME ZONE;
BEGIN
    -- Calculate cutoff date
    cutoff_date := NOW() - (retention_days || ' days')::INTERVAL;

    -- Delete old records and get count
    WITH deleted AS (
        DELETE FROM audit_logs
        WHERE timestamp < cutoff_date
        RETURNING *
    )
    SELECT COUNT(*) INTO deleted_count FROM deleted;

    -- Log the cleanup operation
    RAISE NOTICE 'Audit log retention: Deleted % records older than %',
                 deleted_count, cutoff_date;

    RETURN deleted_count;
END;
$$;

-- =============================================================================
-- Schedule: Daily audit log cleanup at 3:00 AM
-- =============================================================================
-- Remove existing job if present
SELECT cron.unschedule('audit_log_retention') WHERE EXISTS (
    SELECT 1 FROM cron.job WHERE jobname = 'audit_log_retention'
);

-- Create new scheduled job
SELECT cron.schedule(
    'audit_log_retention',           -- Job name
    '0 3 * * *',                     -- Cron expression: 3:00 AM daily
    $$SELECT delete_old_audit_logs(90)$$  -- 90-day retention
);

-- =============================================================================
-- Index: Optimize timestamp-based queries for retention
-- =============================================================================
CREATE INDEX IF NOT EXISTS ix_audit_logs_retention
ON audit_logs (timestamp)
WHERE timestamp < NOW() - INTERVAL '90 days';

-- =============================================================================
-- View: Audit retention status
-- =============================================================================
CREATE OR REPLACE VIEW audit_retention_status AS
SELECT
    COUNT(*) AS total_logs,
    COUNT(*) FILTER (WHERE timestamp < NOW() - INTERVAL '90 days') AS logs_pending_deletion,
    MIN(timestamp) AS oldest_log,
    MAX(timestamp) AS newest_log,
    pg_size_pretty(pg_total_relation_size('audit_logs')) AS table_size
FROM audit_logs;

-- =============================================================================
-- Manual execution example:
-- SELECT delete_old_audit_logs(90);
-- SELECT * FROM audit_retention_status;
-- =============================================================================

COMMENT ON FUNCTION delete_old_audit_logs IS
'Deletes audit log entries older than the specified retention period (default: 90 days).
Returns the number of deleted records.
Scheduled to run daily at 3:00 AM via pg_cron.';
