---
description: The logs tell a story
globs: **/*.cs
alwaysApply: true
---
Use logging to tell a story of what is happening. Avoid excessive logging, for example inside of loops, and keep non-essential logging to LogTrace. LogInformation should tell a short story of intent and what was done. LogWarning and LogError are used for events that should bubble up to users.