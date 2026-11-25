<?php
// dashboard-pic.php - Ausgabe eines Bildes als Stream
$carid = isset($_REQUEST["carid"]) ? $_REQUEST["carid"] : '1';

// Look for wallpaper files in the new directory structure
$wallpaperDir = "/tmp/teslalogger/wallpapers/$carid";
$wallpaperFile = null;
$supportedExtensions = ['jpg', 'jpeg', 'png'];

// First try the standard car ID filename with different extensions
foreach ($supportedExtensions as $ext) {
    $testFile = "$wallpaperDir/$carid.$ext";
    if (file_exists($testFile)) {
        $wallpaperFile = $testFile;
        break;
    }
}

// If still not found, look for any wallpaper file in the new directory
if (!$wallpaperFile && file_exists($wallpaperDir)) {
    $files = scandir($wallpaperDir);
    foreach ($files as $file) {
        if (stripos($file, ".") === 0) continue; // Skip hidden files
        
        $extension = strtolower(pathinfo($file, PATHINFO_EXTENSION));
        if (in_array($extension, $supportedExtensions)) {
            $wallpaperFile = "$wallpaperDir/$file";
            break;
        }
    }
}

// Prüfen ob die Datei existiert
if (!$wallpaperFile || !file_exists($wallpaperFile)) {
    http_response_code(404);
    die('Bild nicht gefunden');
}

// Prüfen ob es sich um eine gültige Bilddatei handelt
$imageInfo = getimagesize($wallpaperFile);
if ($imageInfo === false) {
    http_response_code(400);
    die('Ungültige Bilddatei');
}

// MIME-Type ermitteln
$mimeType = $imageInfo['mime'];

// HTTP-Header setzen
header('Content-Type: ' . $mimeType);
header('Content-Length: ' . filesize($wallpaperFile));
header('Cache-Control: public, max-age=5'); 
header('Last-Modified: ' . gmdate('D, d M Y H:i:s', filemtime($wallpaperFile)) . ' GMT');

// Bild ausgeben
readfile($wallpaperFile);
?>