<?php
require_once("tools.php");
session_start();

// Get the car ID from session or request
$carid = isset($_SESSION["carid"]) ? $_SESSION["carid"] : GetDefaultCarId();
if (isset($_REQUEST["carid"])) {
    $carid = $_REQUEST["carid"];
}

// Check if file was uploaded
if (!isset($_FILES['wallpaper']) || $_FILES['wallpaper']['error'] !== UPLOAD_ERR_OK) {
    http_response_code(400);
    echo json_encode(['error' => 'No file uploaded or upload error occurred']);
    exit;
}

$uploadedFile = $_FILES['wallpaper'];
$fileName = $uploadedFile['name'];
$fileTmpName = $uploadedFile['tmp_name'];
$fileSize = $uploadedFile['size'];
$fileError = $uploadedFile['error'];

// Validate file type
$allowedTypes = ['image/jpeg', 'image/jpg', 'image/png'];
$fileType = mime_content_type($fileTmpName);

if (!in_array($fileType, $allowedTypes)) {
    http_response_code(400);
    echo json_encode(['error' => 'Invalid file type. Only JPG and PNG files are allowed.']);
    exit;
}

// Validate file size (max 10MB)
$maxSize = 10 * 1024 * 1024; // 10MB
if ($fileSize > $maxSize) {
    http_response_code(400);
    echo json_encode(['error' => 'File size too large. Maximum size is 10MB.']);
    exit;
}

// Get file extension
$fileExtension = strtolower(pathinfo($fileName, PATHINFO_EXTENSION));
if (!in_array($fileExtension, ['jpg', 'jpeg', 'png'])) {
    http_response_code(400);
    echo json_encode(['error' => 'Invalid file extension. Only .jpg, .jpeg, and .png files are allowed.']);
    exit;
}

// Create wallpaper directory structure
$wallpaperDir = "/tmp/teslalogger/wallpapers/$carid";

if (!file_exists($wallpaperDir)) {
    if (!mkdir($wallpaperDir, 0755, true)) {
        error_log("Failed to create wallpaper directory: $wallpaperDir");
        http_response_code(500);
        echo json_encode(['error' => 'Failed to create wallpaper directory']);
        exit;
    }
    // Ensure proper permissions
    chmod($wallpaperDir, 0755);
}

// Generate unique filename to avoid conflicts
$newFileName = "$carid.$fileExtension";
$targetPath = "$wallpaperDir/$newFileName";

// Log to Apache error log
error_log("Uploading wallpaper for car $carid: $fileTmpName to $targetPath");

// Check if target directory is writable
if (!is_writable($wallpaperDir)) {
    error_log("Wallpaper directory is not writable: $wallpaperDir");
    http_response_code(500);
    echo json_encode(['error' => 'Wallpaper directory is not writable']);
    exit;
}

// Move uploaded file to target directory
if (!move_uploaded_file($fileTmpName, $targetPath)) {
    $error = error_get_last();
    error_log("Failed to move uploaded file: " . $error['message']);
    http_response_code(500);
    echo json_encode(['error' => 'Failed to save uploaded file: ' . $error['message']]);
    exit;
}

// Remove old wallpapers (keep only the latest one)
$files = glob("$wallpaperDir/wallpaper_*");
if ($files) {
    foreach ($files as $file) {
        if ($file !== $targetPath) {
            unlink($file);
        }
    }
}

// For Docker environments, also copy to the tmp directory for web access
if (!isDocker()) {
    $tmpDir = "tmp/wallpapers/$carid";
    if (!file_exists($tmpDir)) {
        mkdir($tmpDir, 0755, true);
    }
    copy($targetPath, "$tmpDir/$newFileName");
}

// Log successful upload
error_log("Wallpaper uploaded successfully for car $carid: $newFileName");

// Return success response
echo json_encode([
    'success' => true,
    'message' => 'Wallpaper uploaded successfully',
    'filename' => $newFileName
]);
?>
