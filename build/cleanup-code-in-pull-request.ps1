function EnsureSucceeded {
  if ($LastExitCode -ne 0) {
    throw "Command failed with exit code $LastExitCode."
  }
}

$targetBranch = $env:SYSTEM_PULLREQUEST_TARGETBRANCH
if (-not $targetBranch) {
  throw "Sorry, this pipeline can only be run from pull requests."
}

# We're going to push changes, so we need to be on the PR branch instead of the detached head containing the merge result.
Write-Host "Switching to branch $env:SYSTEM_PULLREQUEST_SOURCEBRANCH"
git checkout $env:SYSTEM_PULLREQUEST_SOURCEBRANCH
EnsureSucceeded

# Find the most-recent common ancestor to diff against. Using the target branch name is incorrect because this PR may be outdated.
$baseCommitHash = git merge-base origin/$env:SYSTEM_PULLREQUEST_SOURCEBRANCH origin/$env:SYSTEM_PULLREQUEST_TARGETBRANCH
EnsureSucceeded

$headCommitHash = git rev-parse "HEAD"
EnsureSucceeded
Write-Host "Using commit range for cleanup: $baseCommitHash - $headCommitHash"

dotnet tool restore
EnsureSucceeded

dotnet restore src
EnsureSucceeded

dotnet regitlint -s Steeltoe.All.sln --print-command --skip-tool-check --disable-jb-path-hack --jb-profile="Steeltoe Full Cleanup" --jb --properties:Configuration=Release --jb --verbosity=WARN -f commits -a $headCommitHash -b $baseCommitHash
EnsureSucceeded

git add -A
git diff-index --quiet HEAD --
$hasChangesToCommit = $LastExitCode -ne 0

if ($hasChangesToCommit) {
  Write-Host "Code cleanup resulted in changes."
  git config --global user.email "cibuild@steeltoe.com"
  git config --global user.name "steeltoe-cibuild"

  Write-Host "Committing changes"
  git commit -m "Automated code cleanup from cibuild"
  EnsureSucceeded

  #Write-Host "Pushing changes"
  #git push origin
  #EnsureSucceeded
}
else {
  Write-Host "Code cleanup did not change any files."
}

