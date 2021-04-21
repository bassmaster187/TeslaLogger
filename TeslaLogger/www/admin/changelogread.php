<?php
$installed = "?";

copy("/etc/teslalogger/changelog.md", "/tmp/changelog.md");

header("Location: index.php");
?>