# Builds "Course of Fire.exe" with the C# compiler that ships with Windows (no Visual Studio needed).
# Run:  powershell -ExecutionPolicy Bypass -File build.ps1 [-Out <path to exe>]
# Default output: bin\Course of Fire.exe next to this script.
param([string]$Out = (Join-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'bin') 'Course of Fire.exe'))
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$ico  = Join-Path $here 'CourseOfFire.ico'
$out  = $Out
New-Item -ItemType Directory -Force (Split-Path -Parent $out) | Out-Null
$csc  = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

Add-Type -AssemblyName System.Drawing

# Dark rounded square with a light-blue scope reticle, as PNG bytes.
function New-IconPng([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $w = $size - 1
    $d = $size * 0.44
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($w - $d, 0, $d, $d, 270, 90)
    $path.AddArc($w - $d, $w - $d, $d, $d, 0, 90)
    $path.AddArc(0, $w - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(15, 23, 42))), $path)

    $blue = [System.Drawing.Color]::FromArgb(121, 184, 255)
    $pen = New-Object System.Drawing.Pen $blue, ([Math]::Max(1.4, $size * 0.075))
    $c = $size / 2
    $r = $size * 0.30
    $g.DrawEllipse($pen, $c - $r, $c - $r, $r * 2, $r * 2)
    $gap = $size * 0.08
    $reach = $size * 0.40
    $g.DrawLine($pen, $c, $c - $reach, $c, $c - $gap)
    $g.DrawLine($pen, $c, $c + $gap, $c, $c + $reach)
    $g.DrawLine($pen, $c - $reach, $c, $c - $gap, $c)
    $g.DrawLine($pen, $c + $gap, $c, $c + $reach, $c)
    $dot = [Math]::Max(1.5, $size * 0.05)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush $blue), $c - $dot, $c - $dot, $dot * 2, $dot * 2)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return ,$ms.ToArray()
}

# Write a multi-size .ico (PNG-compressed entries).
$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$images = New-Object System.Collections.Generic.List[byte[]]
foreach ($s in $sizes) { $images.Add((New-IconPng $s)) }

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $ms
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$images[$i].Length); $bw.Write([UInt32]$offset)
    $offset += $images[$i].Length
}
foreach ($img in $images) { $bw.Write($img) }
$bw.Flush()
[System.IO.File]::WriteAllBytes($ico, $ms.ToArray())

$sources = Get-ChildItem (Join-Path $here 'src') -Filter *.cs | ForEach-Object { $_.FullName }
& $csc /nologo /target:winexe /optimize+ /codepage:65001 "/win32icon:$ico" "/out:$out" `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Runtime.Serialization.dll `
    $sources
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Output "Built $out"
