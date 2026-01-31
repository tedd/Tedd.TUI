# Execute Git Commit Script
#
# Purpose: Executes the git commit workflow: gathers information, stages files,
#          commits with message, releases lock, and verifies.
#
# Usage: .\.cursor\scripts\execute-git-commit.ps1 -Files @("file1", "file2") -Message "Commit message"
#        .\.cursor\scripts\execute-git-commit.ps1 -Files @("file1") -Message "First line" -AdditionalMessages @("Second line", "Third line")
#
# Exit codes:
#   0 - Commit completed successfully
#   1 - Error occurred (lock is still released)

param(
    # Array of file paths to stage
    [Parameter(Mandatory=$true)]
    [string[]]$Files,
    
    # Primary commit message (required)
    [Parameter(Mandatory=$true)]
    [string]$Message,
    
    # Additional commit message paragraphs (optional)
    [string[]]$AdditionalMessages = @(),
    
    # Lock file path (defaults to .git-commit.lock in current directory)
    [string]$LockFile = ".git-commit.lock"
)

$ErrorActionPreference = "Stop"

try {
    # Step 2: Gather Information (run in parallel where possible)
    Write-Host "`n=== Gathering Git Information ===" -ForegroundColor Cyan
    
    Write-Host "`n--- Git Status ---" -ForegroundColor Yellow
    git status
    
    Write-Host "`n--- Staged Changes ---" -ForegroundColor Yellow
    $stagedDiff = git diff --staged 2>&1
    if ($stagedDiff) {
        Write-Host $stagedDiff
    } else {
        Write-Host "(no staged changes)"
    }
    
    Write-Host "`n--- Unstaged Changes ---" -ForegroundColor Yellow
    $unstagedDiff = git diff 2>&1
    if ($unstagedDiff) {
        Write-Host $unstagedDiff
    } else {
        Write-Host "(no unstaged changes)"
    }
    
    Write-Host "`n--- Recent Commit History ---" -ForegroundColor Yellow
    git log --oneline -10
    
    # Step 4: Stage and Commit
    Write-Host "`n=== Staging Files ===" -ForegroundColor Cyan
    foreach ($file in $Files) {
        if (Test-Path $file -ErrorAction SilentlyContinue) {
            Write-Host "Staging: $file"
            git add $file
        } else {
            Write-Warning "File not found (may be deleted): $file"
            git add $file 2>&1 | Out-Null  # Try anyway for deleted files
        }
    }
    
    Write-Host "`n=== Creating Commit ===" -ForegroundColor Cyan
    
    # Build commit command with message(s)
    $commitArgs = @("commit")
    
    # Add primary message
    $commitArgs += "-m"
    $commitArgs += $Message
    
    # Add additional messages if provided
    foreach ($additionalMsg in $AdditionalMessages) {
        $commitArgs += "-m"
        $commitArgs += $additionalMsg
    }
    
    # Execute commit
    & git $commitArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Git commit failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "`nCommit created successfully!" -ForegroundColor Green
    
    # Step 5: Release Lock and Verify
    Write-Host "`n=== Releasing Lock and Verifying ===" -ForegroundColor Cyan
    .\.cursor\scripts\release-commit-lock.ps1
    
    Write-Host "`n--- Final Git Status ---" -ForegroundColor Yellow
    git status
    
    Write-Host "`n=== Commit Workflow Completed Successfully ===" -ForegroundColor Green
    exit 0
}
catch {
    Write-Error "Error during commit workflow: $_"
    
    # CRITICAL: Always release lock, even on error
    Write-Host "`nReleasing lock after error..." -ForegroundColor Yellow
    .\.cursor\scripts\release-commit-lock.ps1
    
    exit 1
}
