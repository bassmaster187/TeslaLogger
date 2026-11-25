<?php
require_once("tools.php");
session_start();

// Check if this is a POST request with weather data
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $apiKey = isset($_POST['api_key']) ? trim($_POST['api_key']) : '';
    $city = isset($_POST['city']) ? trim($_POST['city']) : '';
    
    // Validate inputs
    if (empty($apiKey)) {
        http_response_code(400);
        echo json_encode(['error' => 'API key is required']);
        exit;
    }
    
    if (empty($city)) {
        http_response_code(400);
        echo json_encode(['error' => 'City is required']);
        exit;
    }
    
    // Validate API key format (OpenWeatherMap keys are 32 characters)
    if (strlen($apiKey) !== 32 || !ctype_alnum($apiKey)) {
        http_response_code(400);
        echo json_encode(['error' => 'Invalid API key format. OpenWeatherMap API keys should be 32 alphanumeric characters.']);
        exit;
    }
    
    // Determine weather.ini file location based on environment
    if (isDocker()) {
        $weatherIniFile = "/tmp/teslalogger/weather.ini";
        $weatherDir = "/tmp/teslalogger";
    } else {
        $weatherIniFile = "/etc/teslalogger/weather.ini";
        $weatherDir = "/etc/teslalogger";
    }
    
    // Create directory if it doesn't exist
    if (!file_exists($weatherDir)) {
        if (!mkdir($weatherDir, 0755, true)) {
            error_log("Failed to create weather config directory: $weatherDir");
            http_response_code(500);
            echo json_encode(['error' => 'Failed to create configuration directory']);
            exit;
        }
    }
    
    // Create weather.ini content
    $weatherConfig = "[weather]\n";
    $weatherConfig .= "appid = $apiKey\n";
    $weatherConfig .= "city = $city\n";
    
    // Write to weather.ini file
    if (file_put_contents($weatherIniFile, $weatherConfig) === false) {
        error_log("Failed to write weather.ini file: $weatherIniFile");
        http_response_code(500);
        echo json_encode(['error' => 'Failed to save weather configuration']);
        exit;
    }
    
    // Set proper permissions
    chmod($weatherIniFile, 0644);
    
    error_log("Weather configuration saved successfully to: $weatherIniFile");
    echo json_encode([
        'success' => true,
        'message' => 'Weather API key saved successfully'
    ]);
    exit;
}

// If GET request, return current settings (if any)
if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    if (isDocker()) {
        $weatherIniFile = "/tmp/teslalogger/weather.ini";
    } else {
        $weatherIniFile = "/etc/teslalogger/weather.ini";
    }
    
    $currentSettings = [
        'api_key' => '',
        'city' => '',
        'configured' => false
    ];
    
    if (file_exists($weatherIniFile)) {
        $weatherParams = parse_ini_file($weatherIniFile);
        if ($weatherParams && isset($weatherParams['appid']) && isset($weatherParams['city'])) {
            $currentSettings['api_key'] = $weatherParams['appid'];
            $currentSettings['city'] = $weatherParams['city'];
            $currentSettings['configured'] = true;
            
            // Don't show the full API key for security
            if (strlen($currentSettings['api_key']) > 8) {
                $currentSettings['api_key_display'] = substr($currentSettings['api_key'], 0, 8) . '...';
            } else {
                $currentSettings['api_key_display'] = $currentSettings['api_key'];
            }
        }
    }
    
    header('Content-Type: application/json');
    echo json_encode($currentSettings);
    exit;
}

http_response_code(405);
echo json_encode(['error' => 'Method not allowed']);
?>
