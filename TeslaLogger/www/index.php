<?php
// If the requested URL is '/+', show PHP info for debugging
if ($_SERVER['REQUEST_URI'] === '/+') {
    phpinfo();
    exit;
}

// Otherwise, redirect to /admin
header("Location: /admin");
exit;
?>
