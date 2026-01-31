# Acquire Git Commit Lock Script
# 
# Purpose: Acquires a mutex lock to prevent concurrent git commits from multiple agents.
#          This script waits for an existing lock to be released, removes stale locks,
#          and creates a new lock file with a timestamp.
#
# Usage: .\.cursor\scripts\acquire-commit-lock.ps1
#
# Exit codes:
#   0 - Lock acquired successfully
#   1 - Timeout waiting for lock (after 5 minutes)
#
# Lock file format: Contains ISO 8601 timestamp of when lock was acquired
# Lock file location: .git-commit.lock in repository root

param(
    # Lock file path (defaults to .git-commit.lock in current directory)
    [string]$LockFile = ".git-commit.lock",
    
    # Maximum wait time in seconds before timing out (default: 300 = 5 minutes)
    [int]$MaxWaitSeconds = 300,
    
    # Threshold in minutes for considering a lock stale (default: 5 minutes)
    [int]$StaleThresholdMinutes = 5
)

# Wait for existing lock to be released
$waited = 0
while (Test-Path $LockFile) {
    # Read the timestamp from the lock file
    $lockTime = Get-Content $LockFile -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if ($lockTime) {
        try {
            # Parse the timestamp and calculate age
            $lockAge = (Get-Date) - [DateTime]::Parse($lockTime)
            
            # If lock is older than threshold, consider it stale and remove it
            if ($lockAge.TotalMinutes -ge $StaleThresholdMinutes) {
                Write-Host "Removing stale lock (age: $([math]::Round($lockAge.TotalMinutes, 1)) minutes)"
                Remove-Item $LockFile -Force
                break
            }
        }
        catch {
            # If timestamp parsing fails, assume lock is stale and remove it
            Write-Host "Lock file contains invalid timestamp, removing stale lock"
            Remove-Item $LockFile -Force
            break
        }
    }
    
    # Wait and retry
    Write-Host "Waiting for commit lock... ($waited seconds)"
    Start-Sleep -Seconds 5
    $waited += 5
    
    # Check for timeout
    if ($waited -ge $MaxWaitSeconds) {
        Write-Error "Timeout waiting for commit lock after $MaxWaitSeconds seconds"
        exit 1
    }
}

# Acquire lock by writing current timestamp in ISO 8601 format
(Get-Date).ToString("o") | Out-File $LockFile -Force
Write-Host "Commit lock acquired"

exit 0
