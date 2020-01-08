# NugetSync

This is a tool to help you upload your local nuget package to a nuget server. My scenario is that we use "docker gitlab runner" to build our dotnet project,
then it can't cache nuget packages to local because the context is in a Dockerfile. In china, we can't request nuget.org fast, so our building cost so much time.
The nuget sync can sync your local packages to your private nuget server, so we can use a private nuget server and this tools to make your build CI quickly.

## Install

dotnet tools install -g nuget-sync

## Usage

nuget-sync -k {apiKey} -s {nuget server}