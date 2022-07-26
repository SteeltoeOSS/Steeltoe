function EnsureSucceeded {
  if ($LastExitCode -ne 0) {
    throw "Command failed with exit code $LastExitCode."
  }
}

$jsonBody = @"
{
  "comments": [
    {
      "parentCommentId": 0,
      "content": "Example comment from pipeline.",
      "commentType": 2
    }
  ],
  "status": 0
}
"@

try {
  Write-Host "SYSTEM_ACCESSTOKEN = $env:SYSTEM_ACCESSTOKEN"

  $url = "$($env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI)$env:SYSTEM_TEAMPROJECTID/_apis/git/repositories/$($env:BUILD_REPOSITORY_NAME)/pullRequests/$($env:SYSTEM_PULLREQUEST_PULLREQUESTID)/threads?api-version=6.0"
  Write-Host "Posting PR comment to $url with body $jsonBody"
  $response = Invoke-RestMethod -Uri $url -Method POST -Headers @{Authorization = "Bearer $env:SYSTEM_ACCESSTOKEN"} -Body $jsonBody -ContentType application/json
  if ($response -eq $Null) {
    Write-Host "Failed to post PR comment."
  }
}
catch {
  Write-Host "Comment post failed."
  Write-Error $_
  Write-Error $_.Exception.Message
}
