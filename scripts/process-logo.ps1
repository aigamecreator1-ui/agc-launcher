Add-Type -AssemblyName System.Drawing

$srcPath = "C:\Users\MEIJ6\Dev\AGC-Launcher\Assets\Images\agc_logo.png"
$outDir = "C:\Users\MEIJ6\Dev\AGC-Launcher\src\AGC.Launcher\Assets\Images"
$outPng = Join-Path $outDir "agc_logo.png"
$outMarkPng = Join-Path $outDir "agc_mark.png"
$outIco = "C:\Users\MEIJ6\Dev\AGC-Launcher\src\AGC.Launcher\Assets\agc_logo.ico"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$srcImg = [System.Drawing.Image]::FromFile($srcPath)
$src = New-Object System.Drawing.Bitmap $srcImg
$srcImg.Dispose()

$w = $src.Width
$h = $src.Height

# --- Key out the flat black background with a soft ramp so anti-aliased/glow edges survive.
# NOTE: this source is a flat BLACK canvas (no alpha channel) — if a future source instead
# ships on white, flip refR/G/B back to ~252 the way earlier revisions of this script had it. ---
$refR = 0.0; $refG = 0.0; $refB = 0.0
$keepDist = 60.0
$killDist = 10.0

$srcRect = New-Object System.Drawing.Rectangle 0, 0, $w, $h
$srcData = $src.LockBits($srcRect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

$out = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$outData = $out.LockBits($srcRect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

$stride = $srcData.Stride
$bytes = $stride * $h
$srcBuf = New-Object byte[] $bytes
[System.Runtime.InteropServices.Marshal]::Copy($srcData.Scan0, $srcBuf, 0, $bytes)
$outBuf = New-Object byte[] $bytes

$minX = $w; $minY = $h; $maxX = 0; $maxY = 0

for ($y = 0; $y -lt $h; $y++) {
    $rowOffset = $y * $stride
    for ($x = 0; $x -lt $w; $x++) {
        $i = $rowOffset + $x * 4
        $b = $srcBuf[$i]
        $g = $srcBuf[$i + 1]
        $r = $srcBuf[$i + 2]
        $a = $srcBuf[$i + 3]

        $dr = $r - $refR
        $dg = $g - $refG
        $db = $b - $refB
        $dist = [Math]::Sqrt($dr * $dr + $dg * $dg + $db * $db)

        if ($dist -ge $keepDist) {
            $mul = 1.0
        } elseif ($dist -le $killDist) {
            $mul = 0.0
        } else {
            $mul = ($dist - $killDist) / ($keepDist - $killDist)
        }

        $newA = [byte]([Math]::Round($a * $mul))

        $outBuf[$i] = $b
        $outBuf[$i + 1] = $g
        $outBuf[$i + 2] = $r
        $outBuf[$i + 3] = $newA

        if ($newA -gt 10) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
}

[System.Runtime.InteropServices.Marshal]::Copy($outBuf, 0, $outData.Scan0, $bytes)
$src.UnlockBits($srcData)
$out.UnlockBits($outData)

"Detected content bounds: ($minX,$minY) - ($maxX,$maxY) of $w x $h"

# --- Crop to content bounding box with a small transparent margin ---
$pad = 6
$cropX = [Math]::Max(0, $minX - $pad)
$cropY = [Math]::Max(0, $minY - $pad)
$cropW = [Math]::Min($w, $maxX + $pad) - $cropX
$cropH = [Math]::Min($h, $maxY + $pad) - $cropY

$cropRect = New-Object System.Drawing.Rectangle $cropX, $cropY, $cropW, $cropH
$cropped = $out.Clone($cropRect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

$cropped.Save($outPng, [System.Drawing.Imaging.ImageFormat]::Png)
"Saved full-detail trimmed transparent PNG: $outPng ($($cropped.Width) x $($cropped.Height))"

# --- Simplified small-size mark ---
# This specific source artwork happens to contain a small, self-contained, low-detail
# triangular "A" glyph (no hairline circuit traces, no micro-copy, no wordmark) baked into
# the composition as a badge accent. It's the only part of the full lockup that stays legible
# at window-icon / sidebar-corner sizes, so we lift it out on its own and use it wherever the
# full-detail artwork would just turn to mush. Coordinates are hand-picked pixel bounds
# in the ORIGINAL (un-cropped, un-keyed) source canvas — if the master art changes, these
# will need to be re-located by eye again; they are not derived from content-detection.
$markRect = New-Object System.Drawing.Rectangle 607, 872, 38, 40
$markCrop = $out.Clone($markRect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$markCrop.Save($outMarkPng, [System.Drawing.Imaging.ImageFormat]::Png)
"Saved simplified small-size mark PNG: $outMarkPng ($($markCrop.Width) x $($markCrop.Height))"

# --- Build a square, centered, transparent canvas from a given trimmed bitmap ---
function Make-SquareCanvas($bitmap) {
    $squareSize = [Math]::Max($bitmap.Width, $bitmap.Height)
    $square = New-Object System.Drawing.Bitmap $squareSize, $squareSize, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $gfx = [System.Drawing.Graphics]::FromImage($square)
    $gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $gfx.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $offX = [int](($squareSize - $bitmap.Width) / 2)
    $offY = [int](($squareSize - $bitmap.Height) / 2)
    $gfx.DrawImage($bitmap, $offX, $offY, $bitmap.Width, $bitmap.Height)
    $gfx.Dispose()
    return $square
}

function Resize-Square($bitmap, $size) {
    $resized = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($resized)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($bitmap, 0, 0, $size, $size)
    $g.Dispose()
    return $resized
}

# --- Encode a multi-size ICO (16/32/48/256) from the SIMPLIFIED MARK, not the full-detail
# lockup — the window/taskbar icon is shown small often enough that consistency and
# legibility across all four sizes matters more than showing the full artwork at 256. ---
$markSquare = Make-SquareCanvas $markCrop

$sizes = @(16, 32, 48, 256)
$pngBlobs = @()
foreach ($s in $sizes) {
    $resized = Resize-Square $markSquare $s
    $ms = New-Object System.IO.MemoryStream
    $resized.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBlobs += ,($ms.ToArray())
    $resized.Dispose()
    $ms.Dispose()
}

$fs = New-Object System.IO.FileStream $outIco, ([System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter $fs

$bw.Write([UInt16]0)      # reserved
$bw.Write([UInt16]1)      # type: icon
$bw.Write([UInt16]$sizes.Count)

$headerSize = 6 + (16 * $sizes.Count)
$offset = $headerSize
for ($idx = 0; $idx -lt $sizes.Count; $idx++) {
    $s = $sizes[$idx]
    $blob = $pngBlobs[$idx]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim)       # width (0 = 256)
    $bw.Write([byte]$dim)       # height
    $bw.Write([byte]0)          # color palette
    $bw.Write([byte]0)          # reserved
    $bw.Write([UInt16]1)        # color planes
    $bw.Write([UInt16]32)       # bits per pixel
    $bw.Write([UInt32]$blob.Length)
    $bw.Write([UInt32]$offset)
    $offset += $blob.Length
}
foreach ($blob in $pngBlobs) {
    $bw.Write($blob)
}
$bw.Flush()
$bw.Close()
$fs.Close()

"Saved multi-size ICO (from simplified mark): $outIco"

$markSquare.Dispose()
$markCrop.Dispose()
$cropped.Dispose()
$out.Dispose()
$src.Dispose()

"Done."
