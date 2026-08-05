# Sets required Elastic Beanstalk environment properties for NomoAI.API.
# Requires AWS CLI configured for the same account/region as the environment.

param(
    [Parameter(Mandatory = $true)]
    [string]$ServiceKey,

    [string]$EnvironmentName = "Nomo-APIs-env",
    [string]$Region = "us-east-1",
    [string]$BaseUrl = "http://191.218.161.183"
)

$ErrorActionPreference = "Stop"

$options = @(
    @{
        Namespace  = "aws:elasticbeanstalk:application:environment"
        OptionName = "ASPNETCORE_ENVIRONMENT"
        Value      = "Production"
    },
    @{
        Namespace  = "aws:elasticbeanstalk:application:environment"
        OptionName = "AiService__BaseUrl"
        Value      = $BaseUrl
    },
    @{
        Namespace  = "aws:elasticbeanstalk:application:environment"
        OptionName = "AiService__ServiceKey"
        Value      = $ServiceKey
    },
    @{
        Namespace  = "aws:elasticbeanstalk:application:environment"
        OptionName = "AiService__TimeoutSeconds"
        Value      = "180"
    },
    @{
        Namespace  = "aws:elasticbeanstalk:application:environment"
        OptionName = "AiService__HealthTimeoutSeconds"
        Value      = "10"
    },
    @{
        Namespace  = "aws:elasticbeanstalk:application:environment"
        OptionName = "AiService__MaxRetryAttempts"
        Value      = "2"
    }
)

$tempFile = Join-Path $env:TEMP ("nomo-eb-options-" + [guid]::NewGuid().ToString("N") + ".json")
$options | ConvertTo-Json -Depth 5 | Set-Content -Path $tempFile -Encoding utf8

try {
    Write-Host "Updating environment properties on $EnvironmentName ($Region) ..."
    aws elasticbeanstalk update-environment `
        --region $Region `
        --environment-name $EnvironmentName `
        --option-settings "file://$tempFile"

    if ($LASTEXITCODE -ne 0) {
        throw "aws elasticbeanstalk update-environment failed."
    }

    Write-Host "Update submitted. Wait until environment status becomes Ready, then open /swagger."
}
finally {
    Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
}
