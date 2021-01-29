#!/bin/env bash
echo "Update atc-coding-rules-updater tool to newest version"
dotnet tool update -g atc-coding-rules-updater

currentPath=$(pwd)
jsonPath="$currentPath/atc-coding-rules-updater.json"
echo "$jsonPath"

echo "Run atc-rules-updater to fetch updated rulesets and configurations"
atc-coding-rules-updater -r "$currentPath" --optionsPath "$jsonPath"  -v true