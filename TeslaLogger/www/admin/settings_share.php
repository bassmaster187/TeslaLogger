<?php 
require("tools.php");

header("Location: index.php");

if ($_REQUEST[a]== "yes")
{
    setShareData(true);
}
else if ($_REQUEST[a]== "no")
{
    setShareData(true);
}
?>