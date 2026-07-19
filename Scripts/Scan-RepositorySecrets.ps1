[CmdletBinding()]
param(
    [switch]$WorkingTree,
    [switch]$History,
    [int]$MaxFileBytes = 1MB
)

$ErrorActionPreference = "Stop"

if (-not $WorkingTree -and -not $History) {
    $WorkingTree = $true
    $History = $true
}

$bareState = (& git rev-parse --is-bare-repository 2>$null).Trim()
$isBareRepository = ($bareState -eq "true")
$repositoryRoot = if ($isBareRepository) {
    (& git rev-parse --absolute-git-dir 2>$null).Trim()
}
else {
    (& git rev-parse --show-toplevel 2>$null).Trim()
}
if ([string]::IsNullOrWhiteSpace($repositoryRoot)) {
    throw "Run this script from inside the Nyansapo Git repository."
}

if ($WorkingTree -and $isBareRepository) {
    throw "Working-tree scanning is unavailable for a bare Git repository."
}

$textExtensions = @(
    ".config", ".cs", ".env", ".ini", ".json", ".md", ".props", ".ps1",
    ".settings", ".sql", ".targets", ".txt", ".xml", ".yaml", ".yml"
)

$excludedPathPatterns = @(
    "(^|/)(\.git|\.vs|artifacts|bin|obj|packages|TestResults|tmp|scratch)/",
    "(^|/)(node_modules|\.nuget|\.dotnet|\.appdata|\.localappdata|\.userprofile)/"
)

$placeholderValues = @(
    "NeverStoreThisInSource",
    "YourStrongPasswordHere",
    "YourSecurePassword",
    "StrongPassword123",
    "Secure123",
    "use-a-long-unique-random-key",
    "Arkesel-or-smtp-secret-123!",
    "plain-old-secret",
    "RETIRED_SECRET_REMOVED",
    "REPLACE_ME",
    "CHANGE_ME",
    "changeme",
    "password",
    "secret",
    "example"
)

$rules = @(
    [pscustomobject]@{
        Name = "sql-connection-password"
        Pattern = [regex]::new('(?i)(?:^|;)\s*(?:Password|Pwd)\s*=\s*(?<secret>[^;''"&<>\s]+)')
    },
    [pscustomobject]@{
        Name = "quoted-secret-assignment"
        Pattern = [regex]::new('(?im)^\s*(?:(?:const|readonly|static|var|string)\s+)*(?:["''])?(?<key>[A-Za-z0-9_.-]*(?:password|secret|api[_-]?key|access[_-]?token|private[_-]?key)[A-Za-z0-9_.-]*)(?:["''])?\s*[:=]\s*["''](?<secret>[^"''\r\n]+)["'']')
    },
    [pscustomobject]@{
        Name = "credential-bearing-uri"
        Pattern = [regex]::new('(?i)(?:sqlserver|postgres(?:ql)?|mysql)://[^:/\s]+:(?<secret>[^@/\s]+)@')
    },
    [pscustomobject]@{
        Name = "stripe-secret-key"
        Pattern = [regex]::new('(?<![A-Za-z0-9])(?<secret>sk_(?:live|test)_[A-Za-z0-9]{16,})')
    },
    [pscustomobject]@{
        Name = "private-key"
        Pattern = [regex]::new('-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----')
    }
)

function Test-ExcludedPath {
    param([string]$Path)

    $normalized = $Path.Replace("\", "/")
    foreach ($pattern in $excludedPathPatterns) {
        if ($normalized -match $pattern) {
            return $true
        }
    }
    return $false
}

function Test-ScannablePath {
    param([string]$Path)

    if (Test-ExcludedPath -Path $Path) {
        return $false
    }

    $extension = [IO.Path]::GetExtension($Path)
    return [string]::IsNullOrWhiteSpace($extension) -or $textExtensions -contains $extension.ToLowerInvariant()
}

function Test-Placeholder {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $true
    }

    $candidate = $Value.Trim()
    if ($placeholderValues -contains $candidate) {
        return $true
    }

    if ($candidate -match '(?i)(your[_ -]|use-a-|placeholder|example)') {
        return $true
    }

    return $candidate -match '^(\$[A-Za-z_{(]|%[A-Za-z0-9_]+%|<[^>]+>|\{\{.+\}\})'
}

function Get-Fingerprint {
    param([string]$Value)

    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($Value)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace("-", "").Substring(0, 12)
    }
    finally {
        $sha.Dispose()
    }
}

$findings = New-Object System.Collections.Generic.List[object]
$seen = New-Object 'System.Collections.Generic.HashSet[string]'

function Inspect-Content {
    param(
        [string]$Scope,
        [string]$ObjectId,
        [string]$Path,
        [string]$Content
    )

    foreach ($rule in $rules) {
        foreach ($match in $rule.Pattern.Matches($Content)) {
            if ($match.Groups["key"].Success -and
                $match.Groups["key"].Value -match '(?i)\.(?:Text|PasswordChar|UseSystemPasswordChar|PlaceholderText|HeaderText|Name)$') {
                continue
            }

            $secret = if ($match.Groups["secret"].Success) {
                $match.Groups["secret"].Value.Trim()
            }
            else {
                $match.Value
            }

            if (Test-Placeholder -Value $secret) {
                continue
            }

            $prefix = $Content.Substring(0, $match.Index)
            $line = ([regex]::Matches($prefix, "`n")).Count + 1
            $fingerprint = Get-Fingerprint -Value $secret
            $key = "$Scope|$ObjectId|$Path|$($rule.Name)|$fingerprint|$line"
            if ($seen.Add($key)) {
                $findings.Add([pscustomobject]@{
                    Scope = $Scope
                    Object = $ObjectId
                    Path = $Path
                    Line = $line
                    Rule = $rule.Name
                    Fingerprint = $fingerprint
                })
            }
        }
    }
}

if ($WorkingTree) {
    $paths = @(& git -C $repositoryRoot ls-files --cached --others --exclude-standard)
    foreach ($relativePath in $paths) {
        if (-not (Test-ScannablePath -Path $relativePath)) {
            continue
        }

        $fullPath = Join-Path $repositoryRoot $relativePath
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            continue
        }

        $item = Get-Item -LiteralPath $fullPath
        if ($item.Length -gt $MaxFileBytes) {
            continue
        }

        $content = [IO.File]::ReadAllText($item.FullName)
        if ($content.IndexOf([char]0) -ge 0) {
            continue
        }

        Inspect-Content -Scope "working-tree" -Object "current" -Path $relativePath -Content $content
    }
}

if ($History) {
    $objectPaths = @{}
    foreach ($entry in @(& git -C $repositoryRoot rev-list --objects --all)) {
        $separator = $entry.IndexOf(" ")
        if ($separator -lt 1) {
            continue
        }

        $objectId = $entry.Substring(0, $separator)
        $path = $entry.Substring($separator + 1)
        if (-not (Test-ScannablePath -Path $path)) {
            continue
        }

        if (-not $objectPaths.ContainsKey($objectId)) {
            $objectPaths[$objectId] = $path
        }
    }

    function Read-BatchLine {
        param([IO.Stream]$Stream)

        $bytes = New-Object System.Collections.Generic.List[byte]
        while ($true) {
            $value = $Stream.ReadByte()
            if ($value -lt 0 -or $value -eq 10) {
                break
            }
            if ($value -ne 13) {
                $bytes.Add([byte]$value)
            }
        }
        return [Text.Encoding]::ASCII.GetString($bytes.ToArray())
    }

    $temporaryBase = Join-Path ([IO.Path]::GetTempPath()) ("nyansapo-secret-scan-" + [Guid]::NewGuid().ToString("N"))
    $inputPath = $temporaryBase + ".in"
    $outputPath = $temporaryBase + ".out"
    $errorPath = $temporaryBase + ".err"
    [IO.File]::WriteAllLines($inputPath, [string[]]$objectPaths.Keys, [Text.Encoding]::ASCII)

    try {
        $startInfo = New-Object Diagnostics.ProcessStartInfo
        $startInfo.FileName = (Get-Command git).Source
        $startInfo.Arguments = "cat-file --batch"
        $startInfo.WorkingDirectory = $repositoryRoot
        $startInfo.UseShellExecute = $false
        $startInfo.RedirectStandardInput = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $process = New-Object Diagnostics.Process
        $process.StartInfo = $startInfo
        [void]$process.Start()

        $input = [IO.File]::OpenRead($inputPath)
        $batchOutput = [IO.File]::Create($outputPath)
        try {
            $inputCopy = $input.CopyToAsync($process.StandardInput.BaseStream)
            $outputCopy = $process.StandardOutput.BaseStream.CopyToAsync($batchOutput)
            $errorRead = $process.StandardError.ReadToEndAsync()

            [void]$inputCopy.GetAwaiter().GetResult()
            $process.StandardInput.Close()
            $process.WaitForExit()
            [void]$outputCopy.GetAwaiter().GetResult()
            $errorMessage = $errorRead.GetAwaiter().GetResult()
        }
        finally {
            $input.Dispose()
            $batchOutput.Dispose()
        }

        if ($process.ExitCode -ne 0) {
            throw "Git cat-file failed: $errorMessage"
        }

        $output = [IO.File]::OpenRead($outputPath)
        foreach ($objectId in $objectPaths.Keys) {
            $header = Read-BatchLine -Stream $output
            $parts = $header.Split(" ")
            if ($parts.Length -lt 3 -or $parts[1] -ne "blob") {
                continue
            }

            $size = 0L
            if (-not [long]::TryParse($parts[2], [ref]$size)) {
                throw "Git returned an invalid cat-file size for object $objectId."
            }

            $buffer = New-Object byte[] $size
            $offset = 0
            while ($offset -lt $size) {
                $read = $output.Read($buffer, $offset, [int]($size - $offset))
                if ($read -le 0) {
                    throw "Git ended the cat-file stream unexpectedly."
                }
                $offset += $read
            }
            [void]$output.ReadByte()

            if ($size -gt $MaxFileBytes) {
                continue
            }

            $content = [Text.Encoding]::UTF8.GetString($buffer)
            if ($content.IndexOf([char]0) -ge 0) {
                continue
            }

            Inspect-Content -Scope "history" -Object $objectId.Substring(0, 12) -Path $objectPaths[$objectId] -Content $content
        }
    }
    finally {
        if ($output) {
            $output.Dispose()
        }
        foreach ($temporaryPath in @($inputPath, $outputPath, $errorPath)) {
            if (Test-Path -LiteralPath $temporaryPath) {
                Remove-Item -LiteralPath $temporaryPath -Force
            }
        }
    }
}

if ($findings.Count -gt 0) {
    Write-Host "Secret scan failed. Values are redacted; fingerprints identify repeated findings." -ForegroundColor Red
    $findings |
        Sort-Object Scope, Path, Rule, Fingerprint, Object, Line |
        Format-Table Scope, Object, Path, Line, Rule, Fingerprint -AutoSize
    Write-Host "$($findings.Count) potential secret(s) found." -ForegroundColor Red
    exit 2
}

Write-Host "Secret scan passed. No non-placeholder credentials were detected."
