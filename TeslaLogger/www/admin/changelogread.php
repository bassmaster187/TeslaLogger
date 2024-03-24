<?php
$installed = "?";

copy("/tmp/changelog.md", "/tmp/changelog_last.md");

header("Location: index.php");
?>