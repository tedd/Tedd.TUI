#!/bin/bash
# Acquire Git Commit Lock Script
# 
# Purpose: Acquires a mutex lock to prevent concurrent git commits from multiple agents.
#          This script waits for an existing lock to be released, removes stale locks,
#          and creates a new lock file with a timestamp.
#
# Usage: ./.cursor/scripts/acquire-commit-lock.sh
#
# Exit codes:
#   0 - Lock acquired successfully
#   1 - Timeout waiting for lock (after 5 minutes)
#
# Lock file format: Contains ISO 8601 timestamp of when lock was acquired
# Lock file location: .git-commit.lock in repository root

# Default values (can be overridden via environment variables)
LOCK_FILE="${LOCK_FILE:-.git-commit.lock}"
MAX_WAIT="${MAX_WAIT:-300}"  # 5 minutes in seconds
STALE_MINUTES="${STALE_MINUTES:-5}"  # 5 minutes threshold for stale locks

waited=0

# Wait for existing lock to be released
while [ -f "$LOCK_FILE" ]; do
    # Read the timestamp from the lock file
    lock_time=$(head -1 "$LOCK_FILE" 2>/dev/null)
    
    if [ -n "$lock_time" ]; then
        # Try to parse the timestamp and calculate age
        # Support both GNU date (Linux) and BSD date (macOS)
        lock_epoch=$(date -d "$lock_time" +%s 2>/dev/null || date -j -f "%Y-%m-%dT%H:%M:%S" "$lock_time" +%s 2>/dev/null || echo "")
        
        if [ -n "$lock_epoch" ]; then
            now_epoch=$(date +%s)
            age_seconds=$((now_epoch - lock_epoch))
            age_minutes=$((age_seconds / 60))
            
            # If lock is older than threshold, consider it stale and remove it
            if [ "$age_minutes" -ge "$STALE_MINUTES" ]; then
                echo "Removing stale lock (age: ${age_minutes} minutes)"
                rm -f "$LOCK_FILE"
                break
            fi
        else
            # If timestamp parsing fails, assume lock is stale and remove it
            echo "Lock file contains invalid timestamp, removing stale lock"
            rm -f "$LOCK_FILE"
            break
        fi
    fi
    
    # Wait and retry
    echo "Waiting for commit lock... ($waited seconds)"
    sleep 5
    waited=$((waited + 5))
    
    # Check for timeout
    if [ "$waited" -ge "$MAX_WAIT" ]; then
        echo "Timeout waiting for commit lock after $MAX_WAIT seconds" >&2
        exit 1
    fi
done

# Acquire lock by writing current timestamp in ISO 8601 format
date -Iseconds > "$LOCK_FILE"
echo "Commit lock acquired"

exit 0
