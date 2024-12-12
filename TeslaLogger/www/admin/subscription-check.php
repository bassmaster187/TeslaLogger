<?php 
$vin = $_GET['vin'];

if (empty($vin)) {
    echo "VIN is empty";
    error_log("VIN is empty", 0);
    exit;
}

if (strlen($vin) < 17) {
    echo "VIN is not at least 17 characters long";
    error_log("VIN is not at least 17 characters long", 0);
    exit;
}

$cache_file = "/tmp/subscription_cache_$vin.json";
$cache_time = 60; // 1 minutes in seconds

if (file_exists($cache_file) && (time() - filemtime($cache_file)) < $cache_time) {
    // Use cached response
    $subscription = file_get_contents($cache_file);
    error_log("Using cached response for VIN: $vin", 0);

} else {
    // Fetch new response and cache it
    $subscription = file_get_contents("https://teslalogger.de/stripe/subscription-check.php?vin=$vin");
    file_put_contents($cache_file, $subscription);
    error_log("Fetched new response and cached it for VIN: $vin", 0);
}

echo $subscription;
?>
